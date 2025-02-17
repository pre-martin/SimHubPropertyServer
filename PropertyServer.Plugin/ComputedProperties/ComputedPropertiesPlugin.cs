// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SimHub.Plugins.ComputedProperties.Ui;

namespace SimHub.Plugins.ComputedProperties
{
    [PluginName("Computed Properties")]
    [PluginAuthor("Martin Renner")]
    [PluginDescription("Create new properties with JavaScript - v" + ThisAssembly.AssemblyFileVersion)]
    public class ComputedPropertiesPlugin : IDataPlugin, IWPFSettingsV2, IComputedPropertiesManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(ComputedPropertiesPlugin));

        private Engine _engine;
        private ObservableCollection<ScriptData> _scripts = new ObservableCollection<ScriptData>();
        private readonly PluginManagerAccessor _pluginManagerAccessor = new PluginManagerAccessor();

        public PluginManager PluginManager { get; set; }

        public void Init(PluginManager pluginManager)
        {
            _scripts = this.ReadCommonSettings("Scripts", () => new ObservableCollection<ScriptData>());

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

            var scriptLogger = LogManager.GetLogger(GetNamespaceLogger().Name + ".script");

            _engine = new Engine(options => options.Strict = true);
            PrepareEngine(
                engine: _engine,
                getPropertyValue: pluginManager.GetPropertyValue,
                getRawData: (Func<object>)pluginManager.LastData?.NewData?.GetRawDataObject(),
                log: scriptLogger.Info,
                createProperty: propertyName =>
                {
                    if (!string.IsNullOrWhiteSpace(propertyName))
                    {
                        pluginManager.AddProperty(propertyName, typeof(ComputedPropertiesPlugin), typeof(object));
                        _pluginManagerAccessor.SetPropertySupportStatus(propertyName, typeof(ComputedPropertiesPlugin),
                            SupportStatus.Computed);
                    }
                },
                subscribe: (context, propertyName, function) =>
                {
                    if (string.IsNullOrWhiteSpace(propertyName)) return;
                    if (string.IsNullOrWhiteSpace(function)) return;

                    var scriptData = _scripts.FirstOrDefault(sm => sm.Guid == context);
                    if (scriptData != null)
                    {
                        if (scriptData.SubscribedProperties.TryGetValue(propertyName, out var propertyData))
                        {
                            propertyData.Functions.Add(function);
                        }
                        else
                        {
                            var newPropertyData = new PropertyData(function);
                            scriptData.SubscribedProperties.Add(propertyName, newPropertyData);
                        }
                    }
                },
                setPropertyValue: pluginManager.SetPropertyValue<ComputedPropertiesPlugin>);

            #region Test

            if (false)
            {
                var scriptSource = $@"
function init(context) {{
    log('init(' + context + ')');
    createProperty('RpmsThousands');
    createProperty('RpmsHundreds');
    createProperty('RpmsTens');
    createProperty('RpmsOnes');
    subscribe(context, 'DataCorePlugin.GameData.Rpms', 'calculateRpms');
    subscribe(context, 'Rpms', 'calculateRpms');
}}

function calculateRpms() {{
    var rpms = $prop('DataCorePlugin.GameData.Rpms');
    var thousands = Math.floor(rpms / 1000);
    var hundreds = Math.floor((rpms - thousands * 1000) / 100);
    var tens = Math.floor((rpms - thousands * 1000 - hundreds * 100) / 10);
    var ones = Math.floor(rpms - thousands * 1000 - hundreds * 100 - tens * 10);
    setPropertyValue('RpmsThousands', thousands);
    setPropertyValue('RpmsHundreds', hundreds);
    setPropertyValue('RpmsTens', tens);
    setPropertyValue('RpmsOnes', ones);
}}
";
                var parsedScript = Engine.PrepareScript(scriptSource);

                var guid = Guid.NewGuid();
                var metadata = new ScriptData(guid) { Script = scriptSource, ParsedScript = parsedScript };
                _scripts.Add(metadata);
                _engine.Execute(metadata.Script).Invoke("init", guid);
            }

            #endregion

            foreach (var scriptMetadata in _scripts)
            {
                try
                {
                    InitScript(scriptMetadata);
                }
                catch (Exception ex)
                {
                    Log.Error($"Error in script. Disabling it.", ex);
                    scriptMetadata.HasErrors = true;
                }
            }
        }

        public void End(PluginManager pluginManager)
        {
            Log.Info("Shutting down plugin");
            this.SaveCommonSettings("Scripts", _scripts);
            _engine.Dispose();
            _engine = null;
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // Loop over each script
            foreach (var scriptData in _scripts)
            {
                // Maybe the script has just been added or edited, and we have to initialize it.
                if (scriptData.InitRequired)
                {
                    try
                    {
                        InitScript(scriptData);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error in script \"{scriptData.Name}\". Disabling it.", ex);
                        scriptData.HasErrors = true;
                    }
                }

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

                // Invoke the collected functions.
                foreach (var functionName in functionsToInvoke)
                {
                    try
                    {
                        _engine.Execute(scriptData.Script).Invoke(functionName);
                        scriptData.HasErrors = false;
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error in function \"{functionName}\" in script \"{scriptData.Name}\".", ex);
                        scriptData.HasErrors = true;
                    }
                }
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
                var dictionary = new ResourceDictionary
                    { Source = new Uri("pack://application:,,,/PropertyServer;component/PreCommon/Ui/IconResources.xaml") };
                return dictionary["DiCalculateOutlined"] as ImageSource;
            }
        }

        public string LeftMenuTitle => "Computed Properties";

        private void InitScript(ScriptData scriptData)
        {
            if (!scriptData.InitRequired) return;

            scriptData.Reset();
            scriptData.InitRequired = false;
            Log.Info($"Script \"{scriptData.Name}\": Preparing script");
            scriptData.ParsedScript = Engine.PrepareScript(scriptData.Script);
            Log.Info($"Script \"{scriptData.Name}\": Calling \"init()\"");
            _engine.Execute(scriptData.Script).Invoke("init", scriptData.Guid);
            Log.Info($"Script \"{scriptData.Name}\": Ready to use");
        }

        private Logger GetNamespaceLogger()
        {
            var namespaceLogger = LogManager.GetLogger(typeof(ComputedPropertiesPlugin).Namespace);
            return (Logger)namespaceLogger.Logger;
        }

        public void ValidateScript(string script)
        {
            using (var validationEngine = new Engine(options => options.Strict = true))
            {
                var guid = new Guid("11111111-2222-3333-4444-555555555555");
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
                    subscribe: (context, propName, function) =>
                    {
                        if (context != guid) throw new ArgumentException("Invalid parameter 'context' in 'subscribe()' command");
                        if (string.IsNullOrWhiteSpace(propName)) throw new ArgumentException("Invalid parameter 'name' in 'subscribe()' command");
                        if (string.IsNullOrWhiteSpace(function)) throw new ArgumentException("Invalid parameter 'function' in 'subscribe()' command");
                        functionsToInvoke.Add(function);
                    },
                    setPropertyValue: (propName, value) =>
                    {
                        if (!createdProperties.Contains(propName)) throw new ArgumentException($"Property '{propName}' was not created in 'init()', cannot set value");
                    }
                );

                // could throw a ScriptPreparationException
                var preparedScript = Engine.PrepareScript(script);
                // is there an init method? and what problems does it throw?
                try
                {
                    validationEngine.Execute(preparedScript).Invoke("init", guid);
                }
                catch (Exception e)
                {
                    throw new MissingMethodException("Function 'init(context)' cannot be called: " + e.Message);
                }

                if (createdProperties.Count == 0)
                    throw new Exception("Script does not create any properties - this is pointless. Use 'createProperty()' in 'init()'");
                if (functionsToInvoke.Count == 0)
                    throw new Exception("Script does not subscribe to any changes - nothing will happen. Use 'subscribe()' in 'init()'");

                foreach (var function in functionsToInvoke)
                {
                    try
                    {
                        validationEngine.Execute($"{function}()");
                    }
                    catch (Exception e)
                    {
                        throw new MissingMethodException($"Function '{function}' used in 'subscribe()', but not found in code: " + e.Message);
                    }
                }
            }
        }

        private void PrepareEngine(
            Engine engine,
            Func<string, object> getPropertyValue,
            Func<object> getRawData,
            Action<object> log,
            Action<string> createProperty,
            Action<Guid, string, string> subscribe,
            Action<string, object> setPropertyValue)
        {
            engine.SetValue("$prop", getPropertyValue);
            engine.SetValue("NewRawData", getRawData);
            engine.SetValue("log", log);
            engine.SetValue("createProperty", createProperty);
            engine.SetValue("subscribe", subscribe);
            engine.SetValue("setPropertyValue", setPropertyValue);
        }
    }
}