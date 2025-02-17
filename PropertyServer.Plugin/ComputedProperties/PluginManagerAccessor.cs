// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Reflection;

namespace SimHub.Plugins.ComputedProperties
{
    /// <summary>
    /// Access to private fields of the PluginManager and its related classes.
    /// </summary>
    public class PluginManagerAccessor
    {
        private IDictionary<string, PropertyEntry> _generatedProperties;
        private PropertyInfo _propertySupportStatus;

        public void Init(PluginManager pluginManager)
        {
            var generatedPropertiesField = typeof(PluginManager).GetField("GeneratedProperties", BindingFlags.NonPublic | BindingFlags.Instance);
            if (generatedPropertiesField != null)
            {
                var dictObj = generatedPropertiesField.GetValue(pluginManager);
                if (dictObj is IDictionary<string, PropertyEntry> dict)
                {
                    _generatedProperties = dict;
                }
            }

            _propertySupportStatus = typeof(PropertyEntry).GetProperty("SupportStatus");
        }

        public void SetPropertySupportStatus(string name, Type pluginType, SupportStatus status)
        {
            if (_generatedProperties == null || _propertySupportStatus == null) return;

            var propName = (pluginType.Name + '.' + name).ToLowerInvariant();
            if (_generatedProperties.TryGetValue(propName, out var propertyEntry))
            {
                _propertySupportStatus.SetValue(propertyEntry, status);
            }
        }
    }
}