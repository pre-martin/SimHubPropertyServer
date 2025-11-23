// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using SimHub.Plugins.DataPlugins.ShakeItV3;
using SimHub.Plugins.DataPlugins.ShakeItV3.Settings;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// We use this class to manage access to the "settings" properties of the ShakeIt plugins. It allows us to read the
    /// whole configuration of the ShakeIt Bass and the ShakeIt Motors plugin.
    /// </summary>
    public class ShakeItAccessor
    {
        private readonly ShakeITBSV3Plugin _shakeItBassPlugin = PluginManager.GetInstance().GetPlugin<ShakeITBSV3Plugin>();
        private readonly ShakeITMotorsV3Plugin _shakeItMotorsPlugin = PluginManager.GetInstance().GetPlugin<ShakeITMotorsV3Plugin>();

        /// <summary>
        /// Returns all known profiles of the given plugin, which must be a "ShakeItV3" plugin.
        /// </summary>
        /// <remarks>
        /// This returns the original data structure of the ShakeIt internals! Be careful when modifying the data.
        /// </remarks>
        private IEnumerable<ShakeItProfile> SimHubProfiles<T, TSettingsType>(ShakeITV3PluginBase<T, TSettingsType> shakeItPlugin)
            where T : IOutputManager where TSettingsType : ShakeItSettings<T>, new()
        {
            return shakeItPlugin?.Settings == null ? Enumerable.Empty<ShakeItProfile>() : shakeItPlugin.Settings.Profiles;
        }

        /// <summary>
        /// Returns a view on all known profiles of the plugin "ShakeIt Bass Shakers".
        /// </summary>
        /// <remarks>
        /// We wrap the internal classes into our own model, in order to be independent of ShakeIt implementation details.
        /// </remarks>
        public ICollection<Profile> BassProfiles()
        {
            var simHubProfiles = SimHubProfiles(_shakeItBassPlugin);
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
            var simHubProfiles = SimHubProfiles(_shakeItBassPlugin);
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
            var simHubProfiles = SimHubProfiles(_shakeItMotorsPlugin);
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
            var simHubProfiles = SimHubProfiles(_shakeItMotorsPlugin);
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
