// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using Jint;

namespace SimHub.Plugins.ComputedProperties
{
    public interface IComputedPropertiesManager
    {
        object GetPropertyValue(string propertyName);
        object GetRawData();
        void CreateProperty(string propertyName);
        void SetPropertyValue(string propertyName, object value);
        void StartRole(string roleName);
        void StopRole(string roleName);
        void TriggerInputPress(string inputName);
        void TriggerInputRelease(string inputName);

        void PrepareEngine(
            Engine engine,
            Func<object> getRawData,
            Action<object> log,
            Action<string> createProperty,
            Action<string, string> subscribe,
            Func<string, object> getPropertyValue,
            Action<string, object> setPropertyValue,
            Action<string> startRole,
            Action<string> stopRole,
            Action<string> triggerInputPress,
            Action<string> triggerInputRelease);

        void SaveScripts();
    }
}