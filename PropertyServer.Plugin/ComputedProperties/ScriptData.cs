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

        [JsonIgnore] private bool _hasErrors;

        public bool HasErrors
        {
            get => _hasErrors;
            set => SetProperty(ref _hasErrors, value);
        }

        [JsonIgnore]
        private Engine _engine;

        [JsonIgnore]
        public Prepared<Script>? ParsedScript { get; set; }

        /// <summary>
        /// Properties created by the scripts init() function.
        /// </summary>
        [JsonIgnore]
        private readonly HashSet<string> _createdProperties = new HashSet<string>();

        /// <summary>
        /// Properties that this script is watching, associated with some data about this property.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, PropertyData> SubscribedProperties { get; } = new Dictionary<string, PropertyData>();

        /// <summary>
        /// Each function of this script associated with its performance data.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, PerfData> FunctionPerformance { get; } = new Dictionary<string, PerfData>();

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
            SubscribedProperties.Clear();
            FunctionPerformance.Clear();
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
            _engine = new Engine(options => options.Strict = true);
            computedPropertiesManager.PrepareEngine(
                engine: _engine,
                getPropertyValue: computedPropertiesManager.GetPropertyValue,
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
                        SubscribedProperties.Add(propertyName, newPropertyData);
                    }

                    if (!FunctionPerformance.ContainsKey(function))
                    {
                        FunctionPerformance.Add(function, new PerfData());
                    }
                },
                setPropertyValue: (propertyName, value) =>
                {
                    if (_createdProperties.Contains(propertyName))
                    {
                        computedPropertiesManager.SetPropertyValue(propertyName, value);
                    }
                }
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
                Log.Info(logPrefix + "Calling \"init()\"");
                _engine.Invoke("init");
                Log.Info(logPrefix + "Ready to use");
            }
        }

        /// <summary>
        /// Invokes the given function of the script.
        /// </summary>
        public void InvokeFunction(string function)
        {
            _engine.Invoke(function);
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
        /// JavaScript functions that have to be invoked, when the value of the property changes.
        /// </summary>
        public HashSet<string> Functions { get; } = new HashSet<string>();
    }
}