// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using Acornima.Ast;
using Jint;
using log4net;
using Newtonsoft.Json;
using SimHub.Plugins.ComputedProperties.Performance;
using SimHub.Plugins.PreCommon.Ui.Util;

namespace SimHub.Plugins.ComputedProperties
{
    /// <summary>
    /// JavaScript code with its related data (like guid, name, parsed code, ...)
    /// </summary>
    public class ScriptData : ObservableObject
    {
        public const string InitFunction = "init";
        private static readonly ILog Log = LogManager.GetLogger(typeof(ScriptData));

        private ScriptData(Guid guid)
        {
            this.Guid = guid;
        }

        public ScriptData() : this(Guid.NewGuid())
        {
        }

        public Guid Guid { get; }

        private string _name = string.Empty;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _script = string.Empty;

        public string Script
        {
            get => _script;
            set => SetProperty(ref _script, value);
        }

        [JsonIgnore]
        private bool _hasErrors;

        [JsonIgnore]
        public bool HasErrors
        {
            get => _hasErrors;
            private set => SetProperty(ref _hasErrors, value);
        }

        [JsonIgnore]
        private Engine _engine;

        [JsonIgnore]
        private Prepared<Script>? ParsedScript { get; set; }

        /// <summary>
        /// Properties created by the scripts init() function.
        /// </summary>
        [JsonIgnore]
        private readonly HashSet<string> _createdProperties = new HashSet<string>();

        [JsonIgnore]
        private readonly Dictionary<string, PropertyData> _subscribedProperties = new Dictionary<string, PropertyData>();

        /// <summary>
        /// Properties that this script is watching, associated with some data about this property.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, PropertyData> SubscribedProperties => _subscribedProperties;

        [JsonIgnore]
        private readonly Dictionary<string, PerfData> _functionPerformance = new Dictionary<string, PerfData>();

        /// <summary>
        /// Each function of this script associated with its performance data.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, PerfData> FunctionPerformance => _functionPerformance;

        [JsonIgnore]
        public bool InitRequired { get; private set; } = true;

        /// <summary>
        /// Resets the state of the script as if it had just been loaded. The JavaScript engine is disposed.
        /// </summary>
        public void Reset()
        {
            HasErrors = false;
            ParsedScript = null;
            _engine.Dispose();
            _engine = null;
            _createdProperties.Clear();
            _subscribedProperties.Clear();
            _functionPerformance.Clear();
            InitRequired = true;
        }

        /// <summary>
        /// Initializes the state of the script. The JavaScript engine is created and the "init()" function is invoked.
        /// </summary>
        public void Init(IComputedPropertiesManager computedPropertiesManager)
        {
            if (!InitRequired) return;

            InitRequired = false;
            var logPrefix = $"Script \"{Name}\": ";

            try
            {
                _engine = new Engine(options => options.Strict = true);
                computedPropertiesManager.PrepareEngine(
                    engine: _engine,
                    getRawData: computedPropertiesManager.GetRawData,
                    log: data => Log.Info(logPrefix + data),
                    createProperty: propertyName =>
                    {
                        if (!string.IsNullOrWhiteSpace(propertyName))
                        {
                            computedPropertiesManager.CreateProperty(propertyName);
                            _createdProperties.Add(propertyName);
                        }
                    },
                    subscribe: (propertyName, function) =>
                    {
                        if (string.IsNullOrWhiteSpace(propertyName)) return;
                        if (string.IsNullOrWhiteSpace(function)) return;

                        if (SubscribedProperties.TryGetValue(propertyName, out var propertyData))
                        {
                            propertyData.Functions.Add(function);
                        }
                        else
                        {
                            var newPropertyData = new PropertyData(function);
                            _subscribedProperties.Add(propertyName, newPropertyData);
                        }
                    },
                    getPropertyValue: computedPropertiesManager.GetPropertyValue,
                    setPropertyValue: (propertyName, value) =>
                    {
                        if (_createdProperties.Contains(propertyName))
                        {
                            computedPropertiesManager.SetPropertyValue(propertyName, value);
                        }
                    },
                    startRole: computedPropertiesManager.StartRole,
                    stopRole: computedPropertiesManager.StopRole,
                    triggerInputPress: computedPropertiesManager.TriggerInputPress,
                    triggerInputRelease: computedPropertiesManager.TriggerInputRelease
                );

                if (string.IsNullOrWhiteSpace(Script))
                {
                    Log.Info(logPrefix + "Script has no code");
                }
                else
                {
                    Log.Info(logPrefix + "Preparing script");
                    ParsedScript = Engine.PrepareScript(Script);
                    _engine.Execute(Script);
                    Log.Info(logPrefix + $"Calling \"{InitFunction}()\"");
                    _engine.Invoke(InitFunction);
                    Log.Info(logPrefix + "Ready to use");

                    foreach (var functionName in GetAllFunctionNames())
                    {
                        _functionPerformance.Add(functionName, new PerfData());
                    }
                }
            }
            catch (Exception)
            {
                HasErrors = true;
                throw;
            }
        }

        /// <summary>
        /// Invokes the given function of the script.
        /// </summary>
        public void InvokeFunction(string function)
        {
            try
            {
                _engine.Invoke(function);
            }
            catch (Exception)
            {
                HasErrors = true;
                throw;
            }
        }

        /// <summary>
        /// Creates a clone. Only the non-transient properties are cloned!
        /// </summary>
        /// <returns></returns>
        public ScriptData Clone()
        {
            var result = new ScriptData(this.Guid) { Name = this.Name, Script = this.Script };
            return result;
        }

        /// <summary>
        /// Returns all functions of the script, except the "init" function.
        /// </summary>
        public List<string> GetAllFunctionNames()
        {
            var result = new List<string>();

            var scriptBody = ParsedScript?.Program?.Body ?? new NodeList<Statement>();
            foreach (var statement in scriptBody)
            {
                if (statement is FunctionDeclaration functionDeclaration)
                {
                    var functionName = functionDeclaration.Id?.Name;
                    if (functionName != null && functionName != "init")
                    {
                        result.Add(functionName);
                    }
                }
            }

            return result;
        }
    }

    public class PropertyData
    {
        public PropertyData(string function)
        {
            Functions.Add(function);
        }

        /// <summary>
        /// Old value (or previous value) of a property.
        /// </summary>
        public object OldValue { get; set; }

        /// <summary>
        /// JavaScript functions that have to be invoked when the value of the property changes.
        /// </summary>
        public HashSet<string> Functions { get; } = new HashSet<string>();
    }
}