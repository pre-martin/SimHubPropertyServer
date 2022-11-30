// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;

namespace SimHub.Plugins.PropertyServer.Property
{
    /// <summary>
    /// Manages access to the SimHub properties.
    /// </summary>
    public static class PropertyAccessor
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PropertyAccessor));
        private static readonly List<PropertySource> PropertySources;

        static PropertyAccessor()
        {
            // Sort PropertySources descending by prefix length. This order is important, so that the longest matching
            // prefix is being used first.
            PropertySources = new List<PropertySource>(Enum.GetValues(typeof(PropertySource)).Cast<PropertySource>());
            PropertySources.Sort((a, b) => b.GetPropertyPrefix().Length.CompareTo(a.GetPropertyPrefix().Length));
        }

        /// <summary>
        /// Tries to create an instance of <c>SimHubProperty</c> from a given property name.
        /// </summary>
        /// <param name="propertyName">The name of the SimHub property in the format <c>prefix.propertyName</c></param>
        /// <param name="errorCallback">Will be called with an error message if the property cannot be used.</param>
        /// <returns><c>null</c> if the property could not be found</returns>
        public static async Task<SimHubProperty> CreateProperty(string propertyName, Func<string, Task> errorCallback)
        {
            PropertySource? source = null;
            foreach (var ps in PropertySources.Where(ps => propertyName.StartsWith($"{ps.GetPropertyPrefix()}.")))
            {
                source = ps;
                break;
            }

            if (source == null)
            {
                Log.Info($"Property {propertyName} does not start with a known source prefix");
                await errorCallback.Invoke($"Property {propertyName} does not start with a known source prefix");
                return null;
            }

            var simHubProperty = await CreateProperty(source.Value, propertyName, errorCallback);
            if (simHubProperty == null)
            {
                // Property does not exist.
                Log.Info($"Property {propertyName} is unknown");
                await errorCallback.Invoke($"Property {propertyName} is unknown");
                return null;
            }

            return simHubProperty;
        }

        private static bool IsPropertySupported(PropertyInfo pi)
        {
            return pi.PropertyType == typeof(int) || pi.PropertyType == typeof(bool);
        }

        private static bool IsFieldSupported(FieldInfo fi)
        {
            return fi.FieldType == typeof(int) || fi.FieldType == typeof(bool);
        }

        private static bool IsMethodSupported(MethodInfo mi)
        {
            return !mi.Name.StartsWith("get_") &&
                   mi.DeclaringType != typeof(object) &&
                   mi.GetParameters().Length == 0 &&
                   (mi.ReturnType == typeof(int) || mi.ReturnType == typeof(bool));
        }

        /// <summary>
        /// Tries to create an instance of <c>SimHubProperty</c> from a given property name.
        /// </summary>
        private static async Task<SimHubProperty> CreateProperty(PropertySource source, string name, Func<string, Task> errorCallback)
        {
            // Is it a property of type "getter"?
            Log.Debug($"Trying to find property {name} in {source}");
            var plainName = name.Contains('.') ? name.Substring(source.GetPropertyPrefix().Length + 1) : name;
            var propertyInfo = source.GetPropertySourceType().GetProperty(plainName);
            if (propertyInfo != null)
            {
                if (!IsPropertySupported(propertyInfo))
                {
                    // Unsupported property type.
                    Log.Info($"Property {name} is not supported");
                    await errorCallback.Invoke($"Property {name} is not supported");
                    return null;
                }

                return new SimHubPropertyGetter(source, name, propertyInfo);
            }

            // Or is it a field?
            var fieldInfo = source.GetPropertySourceType().GetField(plainName);
            if (fieldInfo != null)
            {
                if (!IsFieldSupported(fieldInfo))
                {
                    Log.Info($"Property {name} (which is a field) is not supported");
                    await errorCallback.Invoke($"Property {name} (which is a field) is not supported");
                    return null;
                }

                return new SimHubPropertyField(source, name, fieldInfo);
            }

            // Maybe it is a method?
            var methodInfo = source.GetPropertySourceType().GetMethod(plainName);
            if (methodInfo != null)
            {
                if (!IsMethodSupported(methodInfo))
                {
                    Log.Info($"Property {name} (which is a method) is not supported");
                    await errorCallback.Invoke($"Property {name} (which is a method) is not supported");
                    return null;
                }

                return new SimHubPropertyMethod(source, name, methodInfo);
            }

            return null;
        }

        /// <summary>
        /// Collects all properties, which can be subscribed.
        /// </summary>
        public static IEnumerable<SimHubProperty> GetAvailableProperties()
        {
            var result = new List<SimHubProperty>();

            var sources = Enum.GetValues(typeof(PropertySource)).Cast<PropertySource>();
            foreach (var source in sources)
            {
                var availableProperties = source.GetPropertySourceType().GetProperties();
                foreach (var p in availableProperties.Where(IsPropertySupported))
                {
                    result.Add(new SimHubPropertyGetter(source, $"{source.GetPropertyPrefix()}.{p.Name}", p));
                }

                var availableFields = source.GetPropertySourceType().GetFields();
                foreach (var f in availableFields.Where(IsFieldSupported))
                {
                    result.Add(new SimHubPropertyField(source, $"{source.GetPropertyPrefix()}.{f.Name}", f));
                }

                var availableMethods = source.GetPropertySourceType().GetMethods();
                foreach (var m in availableMethods.Where(IsMethodSupported))
                {
                    result.Add(new SimHubPropertyMethod(source, $"{source.GetPropertyPrefix()}.{m.Name}", m));
                }
            }

            return result;
        }
    }
}