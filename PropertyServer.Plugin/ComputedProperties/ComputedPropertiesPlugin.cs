// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameReaderCommon;
using Jint;
using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using SimHub.Plugins.ComputedProperties.Performance;
using SimHub.Plugins.ComputedProperties.Ui;

namespace SimHub.Plugins.ComputedProperties
{
    [PluginName("Computed Properties")]
    [PluginAuthor("Martin Renner")]
    [PluginDescription("Create new properties with JavaScript - v" + ThisAssembly.AssemblyFileVersion)]
    public class ComputedPropertiesPlugin : IDataPlugin, IWPFSettingsV2, IScriptValidator, IComputedPropertiesManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComputedPropertiesPlugin));

        private ObservableCollection<ScriptData> _scripts = new ObservableCollection<ScriptData>();
        private readonly PluginManagerAccessor _pluginManagerAccessor = new PluginManagerAccessor();

        public PluginManager PluginManager { get; set; }

        public void Init(PluginManager pluginManager)
        {
            _scripts = this.ReadCommonSettings("Scripts", () => new ObservableCollection<ScriptData>());
            _scripts.CollectionChanged += ScriptsOnCollectionChanged;

            // Add our own log file for our plugin.
            var appender = new RollingFileAppender
            {
                File = "logs/ComputedProperties.log",
                Name = "FileAppender",
                MaxFileSize = 1 * 1024 * 1024,
                MaxSizeRollBackups = 5,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                Layout = new PatternLayout("%date{yyyy-MM-dd HH:mm:ss,fff} %-5level [%-15.15thread] %-40.40logger - %message%newline")
            };
            appender.ActivateOptions();

            var namespaceLogger = GetNamespaceLogger();
            namespaceLogger.Additivity = false;
            namespaceLogger.AddAppender(appender);
            namespaceLogger.Level = Level.Info;

            Log.Info($"===== Starting ComputedPropertiesManager, version {ThisAssembly.AssemblyFileVersion} =====");

            _pluginManagerAccessor.Init(pluginManager);

            // Initialize all scripts
            foreach (var scriptData in _scripts)
            {
                InitScript(scriptData);
            }

            ReplaceAllScriptActions();
        }

        public void End(PluginManager pluginManager)
        {
            Log.Info("Shutting down plugin");
            this.SaveCommonSettings("Scripts", _scripts);

            foreach (var scriptData in _scripts)
            {
                Log.Info($"Performance data for script \"{scriptData.Name}\":");
                foreach (var fp in scriptData.FunctionPerformance)
                {
                    var avg = fp.Value.Time / fp.Value.Calls;
                    Log.Info($"  {fp.Key}(): {fp.Value.Calls} calls, {avg:F3} ms/call, {fp.Value.Skipped} times skipped");
                }
                scriptData.Reset();
            }
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            var initCalled = false;

            // Loop over each script
            foreach (var scriptData in _scripts)
            {
                // Maybe the script has just been added or edited, and we have to initialize it.
                if (scriptData.InitRequired)
                {
                    InitScript(scriptData);
                    initCalled = true;
                }

                // Incorrect scripts are skipped.
                if (scriptData.HasErrors) continue;

                // Loop over each subscribed property. If its value has changed, collect the associated functions.
                var functionsToInvoke = new HashSet<string>();
                var propertyNames = new List<string>(scriptData.SubscribedProperties.Keys);
                foreach (var propertyName in propertyNames)
                {
                    var currentValue = pluginManager.GetPropertyValue(propertyName);
                    var oldValue = scriptData.SubscribedProperties[propertyName].OldValue;
                    var diff = currentValue is IComparable currentComparable
                        ? currentComparable.CompareTo(oldValue) != 0
                        : currentValue != oldValue;
                    if (diff)
                    {
                        scriptData.SubscribedProperties[propertyName].OldValue = currentValue;
                        functionsToInvoke.UnionWith(scriptData.SubscribedProperties[propertyName].Functions);
                    }
                }

                // Invoke the collected functions. We iterate over the "performance" dictionary, as it knows
                // all functions, and we can update the performance data in the same time.
                foreach (var fp in scriptData.FunctionPerformance)
                {
                    if (!functionsToInvoke.Contains(fp.Key))
                    {
                        fp.Value.Skipped++;
                    }
                    else
                    {
                        InvokeFunction(scriptData, fp.Key, fp.Value);
                    }
                }
            }

            // At least one script has changed, so we have to replace all actions (update is not supported by SimHub).
            if (initCalled)
            {
                ReplaceAllScriptActions();
            }
        }

        public Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            var computedPropertiesViewModel = new ComputedPropertiesViewModel(_scripts, this);
            return new ComputedPropertiesControl { DataContext = computedPropertiesViewModel };
        }

        public ImageSource PictureIcon
        {
            get
            {
                var dictionary = new ResourceDictionary { Source = new Uri("pack://application:,,,/PropertyServer;component/PreCommon/Ui/IconResources.xaml") };
                return dictionary["DiCalculateOutlined"] as ImageSource;
            }
        }

        public string LeftMenuTitle => "Computed Properties";

        private void InitScript(ScriptData scriptData)
        {
            try
            {
                scriptData.Init(this);
            }
            catch (Exception ex)
            {
                Log.Error($"Error in script \"{scriptData.Name}\". It is now disabled.", ex);
            }
        }

        /// <summary>
        /// Invokes a function of the script and records its performance data.
        /// </summary>
        private void InvokeFunction(ScriptData scriptData, string functionName, PerfData perfData = null)
        {
            if (perfData == null)
            {
                scriptData.FunctionPerformance.TryGetValue(functionName, out perfData);
            }

            if (perfData != null)
            {
                try
                {
                    using (new PerfToken(perfData))
                    {
                        scriptData.InvokeFunction(functionName);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in function \"{functionName}\" in script \"{scriptData.Name}\".", ex);
                }
            }
        }

        /// <summary>
        /// Removes all "Script" actions and adds them again.
        /// </summary>
        /// <remarks>SimHub does not allow to update or remove actions, so we have to go this way.</remarks>
        private void ReplaceAllScriptActions()
        {
            const string prefix = "Script";
            PluginManager.ClearActions(typeof(ComputedPropertiesPlugin), prefix);

            foreach (var scriptData in _scripts)
            {
                var functionNames = scriptData.GetAllFunctionNames();
                foreach (var functionName in functionNames)
                {
                    this.AddAction($"{prefix}.{scriptData.Name}#{functionName}()", (manager, actionName) => HandleAction(scriptData.Name, functionName));
                }
            }
        }

        private void HandleAction(string scriptName, string functionName)
        {
            var scriptData = _scripts.FirstOrDefault(s => s.Name == scriptName);
            if (scriptData == null) return;

            InvokeFunction(scriptData, functionName);
        }

        private void ScriptsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // When a script was removed, we have to update the available actions.
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                ReplaceAllScriptActions();
            }
        }

        private Logger GetNamespaceLogger()
        {
            var namespaceLogger = LogManager.GetLogger(typeof(ComputedPropertiesPlugin).Namespace);
            return (Logger)namespaceLogger.Logger;
        }

        /// <summary>
        /// Validates the given JavaScript code. Throws exceptions if the script is invalid.
        /// </summary>
        public void ValidateScript(string script)
        {
            const string initFunction = ScriptData.InitFunction;

            using (var validationEngine = new Engine(options => options.Strict = true))
            {
                var createdProperties = new HashSet<string>();
                var functionsToInvoke = new HashSet<string>();

                PrepareEngine(
                    engine: validationEngine,
                    getPropertyValue: s => "5",
                    getRawData: () => "6",
                    log: data => { },
                    createProperty: propName =>
                    {
                        if (!string.IsNullOrWhiteSpace(propName)) createdProperties.Add(propName);
                    },
                    subscribe: (propName, function) =>
                    {
                        if (string.IsNullOrWhiteSpace(propName)) throw new ArgumentException("Invalid parameter 'name' in 'subscribe()' command");
                        if (string.IsNullOrWhiteSpace(function)) throw new ArgumentException("Invalid parameter 'function' in 'subscribe()' command");
                        functionsToInvoke.Add(function);
                    },
                    setPropertyValue: (propName, value) =>
                    {
                        if (!createdProperties.Contains(propName)) throw new ArgumentException($"Property '{propName}' was not created in '{initFunction}()', cannot set value");
                    }
                );

                // could throw a ScriptPreparationException
                var preparedScript = Engine.PrepareScript(script);
                // is there an init method? and what problems does it throw?
                try
                {
                    validationEngine.Execute(preparedScript);
                    validationEngine.Invoke(initFunction);
                }
                catch (Exception e)
                {
                    throw new MissingMethodException($"Function '{initFunction}()' cannot be called: " + e.Message);
                }

                if (createdProperties.Count == 0)
                    throw new Exception($"Script does not create any properties - this is pointless. Use 'createProperty()' in '{initFunction}()'");
                if (functionsToInvoke.Count == 0)
                    throw new Exception($"Script does not subscribe to any changes - nothing will happen. Use 'subscribe()' in '{initFunction}()'");

                foreach (var function in functionsToInvoke)
                {
                    try
                    {
                        validationEngine.Invoke(function);
                    }
                    catch (Exception e)
                    {
                        throw new MissingMethodException($"Function '{function}' used in 'subscribe()', but not found in code: " + e.Message);
                    }
                }
            }
        }


        #region IComputedPropertiesManager

        public object GetPropertyValue(string propertyName)
        {
            return PluginManager.GetPropertyValue(propertyName);
        }

        public object GetRawData()
        {
            return PluginManager.LastData?.NewData?.GetRawDataObject();
        }

        public void CreateProperty(string propertyName)
        {
            PluginManager.AddProperty(propertyName, typeof(ComputedPropertiesPlugin), typeof(object));
            _pluginManagerAccessor.SetPropertySupportStatus(propertyName, typeof(ComputedPropertiesPlugin), SupportStatus.Computed);
        }

        public void SetPropertyValue(string propertyName, object value)
        {
            PluginManager.SetPropertyValue<ComputedPropertiesPlugin>(propertyName, value);
        }

        public void PrepareEngine(
            Engine engine,
            Func<string, object> getPropertyValue,
            Func<object> getRawData,
            Action<object> log,
            Action<string> createProperty,
            Action<string, string> subscribe,
            Action<string, object> setPropertyValue)
        {
            engine.SetValue("$prop", getPropertyValue);
            engine.SetValue("NewRawData", getRawData);
            engine.SetValue("log", log);
            engine.SetValue("createProperty", createProperty);
            engine.SetValue("subscribe", subscribe);
            engine.SetValue("setPropertyValue", setPropertyValue);
        }

        #endregion
    }
}