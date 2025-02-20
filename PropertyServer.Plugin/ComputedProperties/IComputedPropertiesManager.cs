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

        void PrepareEngine(
            Engine engine,
            Func<string, object> getPropertyValue,
            Func<object> getRawData,
            Action<object> log,
            Action<string> createProperty,
            Action<string, string> subscribe,
            Action<string, object> setPropertyValue);
    }
}