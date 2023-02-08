// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimHub.Plugins.DataPlugins.ShakeItV3;
using SimHub.Plugins.DataPlugins.ShakeItV3.Settings;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// <c>ShakeITBSV3Plugin.settings</c> is not accessible for us, because it is "private". So we use this class to manage
    /// access to this property. It allows us to read the whole configuration of the ShakeIt Bass plugin.
    /// </summary>
    public class ShakeItBassAccessor
    {
        private PluginManager _pluginManager;
        private FieldInfo _shakeItSettingsField;
        private readonly Converter _converter = new Converter();

        /// <summary>
        /// Initialises the accessor. Calls to subsequent methods of this class will only be successful, if this method was called once.
        /// </summary>
        /// <remarks>
        /// De facto, this method initializes the reflective access to the private field <c>ShakeITBSV3Plugin.settings</c>.
        /// </remarks>
        public void Init(PluginManager pluginManager)
        {
            _pluginManager = pluginManager;

            if (_shakeItSettingsField == null)
            {
                var shakeItPluginBaseType = typeof(ShakeITBSV3Plugin).BaseType;
                if (shakeItPluginBaseType != null)
                {
                    _shakeItSettingsField = shakeItPluginBaseType.GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }
        }

        /// <summary>
        /// Returns all known profiles of the plugin "ShakeIt Bass Shakers".
        /// </summary>
        private IEnumerable<ShakeItProfile> SimHubProfiles()
        {
            if (_shakeItSettingsField == null) return Enumerable.Empty<ShakeItProfile>();

            var shakeItPlugin = _pluginManager.GetPlugin<ShakeITBSV3Plugin>();
            if (_shakeItSettingsField.GetValue(shakeItPlugin) is ShakeItSettings settings)
            {
                return settings.Profiles;
            }

            return Enumerable.Empty<ShakeItProfile>();
        }

        /// <summary>
        /// Returns a view on all known profiles of the plugin "ShakeIt Bass Shakers".
        /// </summary>
        /// <remarks>
        /// We convert the ShakeIt Bass internal classes into our own view, in order to be independent of ShakeIt Bass implementation
        /// details. The returned view is not bound to the underlying objects, so calling "setters" will not reflect the changes.
        /// </remarks>
        public ICollection<Profile> Profiles()
        {
            var simHubProfiles = SimHubProfiles();
            return simHubProfiles.Select(ConvertProfile).ToList();
        }

        /// <summary>
        /// Tries to find an effect group or effect with the given Guid by searching in all available profiles.
        /// </summary>
        /// <remarks>
        /// Caution: SimHub does not enforce that each element has an unique Guid! This method returns only the first matching element.
        /// </remarks>
        /// <returns>An instance of <c>EffectsContainerBase</c> or one if its subclasses, or <c>null</c> if the element was not found.</returns>
        public EffectsContainerBase FindEffect(Guid guid)
        {
            var simHubProfiles = SimHubProfiles();
            return simHubProfiles.Select(simHubProfile => FindEffect(simHubProfile.EffectsContainers, guid)).FirstOrDefault(result => result != null);
        }

        private Profile ConvertProfile(ShakeItProfile simHubProfile)
        {
            var profile = new Profile { Id = simHubProfile.ProfileId, Name = simHubProfile.Name };
            ConvertEffectsContainers(simHubProfile.EffectsContainers, profile.EffectsContainers);
            return profile;
        }

        private void ConvertEffectsContainers(
            IEnumerable<DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase> simHubEffectsContainerBases,
            ICollection<EffectsContainerBase> effectsContainerBases)
        {
            foreach (var simHubEffectsContainerBase in simHubEffectsContainerBases)
            {
                if (simHubEffectsContainerBase is DataPlugins.ShakeItV3.EffectsContainers.GroupContainer simHubGroupContainer)
                {
                    var groupContainer = _converter.Convert(simHubGroupContainer);
                    effectsContainerBases.Add(groupContainer);
                    ConvertEffectsContainers(simHubGroupContainer.EffectsContainers, groupContainer.EffectsContainers);
                }
                else
                {
                    var effectsContainerBase = _converter.Convert(simHubEffectsContainerBase);
                    effectsContainerBases.Add(effectsContainerBase);
                }
            }
        }

        private EffectsContainerBase FindEffect(IEnumerable<DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase> simHubEffectsContainerBases, Guid guid)
        {
            foreach (var simHubEffectsContainerBase in simHubEffectsContainerBases)
            {
                if (simHubEffectsContainerBase.ContainerId == guid)
                {
                    return _converter.Convert(simHubEffectsContainerBase);
                }

                if (simHubEffectsContainerBase is DataPlugins.ShakeItV3.EffectsContainers.GroupContainer simHubGroupContainer)
                {
                    return FindEffect(simHubGroupContainer.EffectsContainers, guid);
                }

            }

            return null;
        }
    }
}
