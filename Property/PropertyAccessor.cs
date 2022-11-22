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

        /// <summary>
        /// Tries to create an instance of <c>SimHubProperty</c> from a given property name.
        /// </summary>
        private static async Task<SimHubProperty> CreateProperty(PropertySource source, string name,
            Func<string, Task> errorCallback, bool quiet = false)
        {
            // Is it a property of type "getter"?
            if (!quiet) Log.Debug($"Trying to find property {name} in {source}");
            var plainName = name.Contains('.') ? name.Substring(source.GetPropertyPrefix().Length + 1) : name;
            var propertyInfo = source.GetPropertySourceType().GetProperty(plainName);
            if (propertyInfo != null)
            {
                if (propertyInfo.PropertyType != typeof(int) && propertyInfo.PropertyType != typeof(bool))
                {
                    // Unsupported property type.
                    if (!quiet) Log.Info($"Property {name} has an unsupported type");
                    await errorCallback.Invoke($"Property {name} has an unsupported type");
                    return null;
                }

                return new SimHubPropertyGetter(source, name, propertyInfo);
            }

            // Maybe it is a method?
            var methodInfo = source.GetPropertySourceType().GetMethod(plainName);
            if (methodInfo != null)
            {
                if (methodInfo.GetParameters().Length != 0)
                {
                    // Method requires parameters.
                    if (!quiet) Log.Info($"Property {name} (which is a method) is not parameterless");
                    await errorCallback.Invoke($"Property {name} (which is a method) is not parameterless");
                    return null;
                }

                if (methodInfo.ReturnType != typeof(int) && methodInfo.ReturnType != typeof(bool))
                {
                    // Unsupported property type.
                    if (!quiet) Log.Info($"Property {name} (which is a method) has an unsupported type");
                    await errorCallback.Invoke($"Property {name} (which is a method) has an unsupported type");
                    return null;
                }

                return new SimHubPropertyMethod(source, name, methodInfo);
            }

            return null;
        }

        /// <summary>
        /// Collects all properties, which can be subscribed.
        /// </summary>
        public static async Task<List<SimHubProperty>> GetAvailableProperties()
        {
            var result = new List<SimHubProperty>();

            var sources = Enum.GetValues(typeof(PropertySource)).Cast<PropertySource>();
            foreach (var source in sources)
            {
                var availableProperties = source.GetPropertySourceType().GetProperties();
                foreach (var p in availableProperties)
                {
                    var property = await CreateProperty(source, $"{source.GetPropertyPrefix()}.{p.Name}",
                        s => Task.CompletedTask, true);
                    if (property != null)
                    {
                        result.Add(property);
                    }
                }
            }

            return result;
        }
    }
}