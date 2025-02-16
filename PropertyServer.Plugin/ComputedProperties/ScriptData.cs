// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using Acornima.Ast;
using Jint;
using Newtonsoft.Json;
using SimHub.Plugins.PreCommon.Ui.Util;

namespace SimHub.Plugins.ComputedProperties
{
    public class ScriptData : ObservableObject
    {
        public ScriptData(Guid guid)
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
        public Prepared<Script>? ParsedScript { get; set; }

        /// <summary>
        /// Properties that this script is watching, associated with some data about this property.
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, PropertyData> SubscribedProperties { get; } = new Dictionary<string, PropertyData>();

        [JsonIgnore]
        public bool InitRequired { get; set; } = true;

        /// <summary>
        /// Resets the state of the script as if it had just been loaded.
        /// </summary>
        public void Reset()
        {
            HasErrors = false;
            ParsedScript = null;
            SubscribedProperties.Clear();
            InitRequired = true;
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