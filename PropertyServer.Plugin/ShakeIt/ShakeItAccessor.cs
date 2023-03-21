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
    /// <c>ShakeITBSV3Plugin.settings</c> and <c>ShakeITMotorsV3Plugin.settings</c> are not accessible for us, because both are "private".
    /// So we use this class to manage access to these property. It allows us to read the whole configuration of the ShakeIt Bass
    /// and the ShakeIt Motors plugin.
    /// </summary>
    public class ShakeItAccessor
    {
        private PluginManager _pluginManager;
        private FieldInfo _shakeItBassSettingsField;
        private ShakeITBSV3Plugin _shakeItBassPlugin;
        private ShakeITMotorsV3Plugin _shakeItMotorsPlugin;
        private FieldInfo _shakeItMotorsSettingsField;

        /// <summary>
        /// Initialises the accessor. Calls to subsequent methods of this class will only be successful, if this method was called once.
        /// </summary>
        /// <remarks>
        /// De facto, this method initializes the reflective access to the private fields <c>ShakeITBSV3Plugin.settings</c> and
        /// <c>ShakeItMotorsV3Plugin.settings</c>.
        /// </remarks>
        public void Init(PluginManager pluginManager)
        {
            _pluginManager = pluginManager;

            if (_shakeItBassSettingsField == null)
            {
                var shakeItPluginBaseType = typeof(ShakeITBSV3Plugin).BaseType;
                if (shakeItPluginBaseType != null)
                {
                    _shakeItBassSettingsField = shakeItPluginBaseType.GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }
            _shakeItBassPlugin = _pluginManager.GetPlugin<ShakeITBSV3Plugin>();

            if (_shakeItMotorsSettingsField == null)
            {
                var shakeItPluginBaseType = typeof(ShakeITMotorsV3Plugin).BaseType;
                if (shakeItPluginBaseType != null)
                {
                    _shakeItMotorsSettingsField = shakeItPluginBaseType.GetField("settings", BindingFlags.NonPublic | BindingFlags.Instance);
                }
            }
            _shakeItMotorsPlugin = _pluginManager.GetPlugin<ShakeITMotorsV3Plugin>();
        }

        /// <summary>
        /// Returns all known profiles of the given plugin, which must be a "ShakeItV3" plugin.
        /// </summary>
        /// <remarks>
        /// This returns the original data structure of the ShakeIt internals! Be careful when modifying the data.
        /// </remarks>
        private IEnumerable<ShakeItProfile> SimHubProfiles<T>(ShakeITV3PluginBase<T> shakeItPlugin, FieldInfo settingsField) where T : IOutputManager
        {
            if (settingsField == null || shakeItPlugin == null) return Enumerable.Empty<ShakeItProfile>();

            if (settingsField.GetValue(shakeItPlugin) is ShakeItSettings settings)
            {
                return settings.Profiles;
            }

            return Enumerable.Empty<ShakeItProfile>();
        }

        /// <summary>
        /// Returns a view on all known profiles of the plugin "ShakeIt Bass Shakers".
        /// </summary>
        /// <remarks>
        /// We wrap the internal classes into our own model, in order to be independent of ShakeIt implementation details.
        /// </remarks>
        public ICollection<Profile> BassProfiles()
        {
            var simHubProfiles = SimHubProfiles(_shakeItBassPlugin, _shakeItBassSettingsField);
            return simHubProfiles.Select(simHubProfile => new Profile(simHubProfile)).ToList();
        }

        /// <summary>
        /// Tries to find an effect group or effect with the given Guid by searching in all available profiles.
        /// </summary>
        /// <remarks>
        /// Caution: SimHub does not enforce that each element has an unique Guid! This method returns only the first matching element.
        /// </remarks>
        /// <returns>An instance of <c>EffectsContainerBase</c> or one if its subclasses, or <c>null</c> if the element was not found.</returns>
        public EffectsContainerBase FindBassEffect(Guid guid)
        {
            var simHubProfiles = SimHubProfiles(_shakeItBassPlugin, _shakeItBassSettingsField);
            return simHubProfiles.Select(simHubProfile => FindEffect(simHubProfile.EffectsContainers, guid)).FirstOrDefault(result => result != null);
        }

        /// <summary>
        /// Returns a view on all known profiles of the plugin "ShakeIt Motors".
        /// </summary>
        /// <remarks>
        /// We wrap the internal classes into our own model, in order to be independent of ShakeIt implementation details.
        /// </remarks>
        public ICollection<Profile> MotorsProfiles()
        {
            var simHubProfiles = SimHubProfiles(_shakeItMotorsPlugin, _shakeItMotorsSettingsField);
            return simHubProfiles.Select(simHubProfile => new Profile(simHubProfile)).ToList();
        }

        /// <summary>
        /// Tries to find an effect group or effect with the given Guid by searching in all available profiles.
        /// </summary>
        /// <remarks>
        /// Caution: SimHub does not enforce that each element has an unique Guid! This method returns only the first matching element.
        /// </remarks>
        /// <returns>An instance of <c>EffectsContainerBase</c> or one if its subclasses, or <c>null</c> if the element was not found.</returns>
        public EffectsContainerBase FindMotorsEffect(Guid guid)
        {
            var simHubProfiles = SimHubProfiles(_shakeItMotorsPlugin, _shakeItMotorsSettingsField);
            return simHubProfiles.Select(simHubProfile => FindEffect(simHubProfile.EffectsContainers, guid)).FirstOrDefault(result => result != null);
        }

        /// <summary>
        /// Groups all effect groups and effects of a given profile by their Guid.
        /// </summary>
        public Dictionary<Guid, List<EffectsContainerBase>> GroupEffectsByGuid(Profile profile)
        {
            var effectsContainerCollector = new EffectsContainerCollector();
            return effectsContainerCollector.ByGuid(profile);
        }

        private EffectsContainerBase FindEffect(IEnumerable<DataPlugins.ShakeItV3.EffectsContainers.EffectsContainerBase> simHubEffectsContainerBases, Guid guid)
        {
            foreach (var simHubEffectsContainerBase in simHubEffectsContainerBases)
            {
                if (simHubEffectsContainerBase.ContainerId == guid)
                {
                    return Converter.Convert(null, simHubEffectsContainerBase);
                }

                if (simHubEffectsContainerBase is DataPlugins.ShakeItV3.EffectsContainers.GroupContainer simHubGroupContainer)
                {
                    var result = FindEffect(simHubGroupContainer.EffectsContainers, guid);
                    if (result != null)
                    {
                        return result;
                    }
                }

            }

            return null;
        }
    }
}
