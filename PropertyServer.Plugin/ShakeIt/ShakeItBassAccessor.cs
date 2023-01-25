// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SimHub.Plugins.DataPlugins.ShakeItV3;
using SimHub.Plugins.DataPlugins.ShakeItV3.Settings;

namespace SimHub.Plugins.PropertyServer.ShakeIt
{
    /// <summary>
    /// <c>ShakeITBSV3Plugin.settings</c> is not accessible for us, because it is "private". So we use this class to manage
    /// access to this property.
    /// </summary>
    public class ShakeItBassAccessor
    {
        private PluginManager _pluginManager;
        private FieldInfo _shakeItSettingsField;

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
        public IEnumerable<Profile> Profiles()
        {
            var simHubProfiles = SimHubProfiles();
            return simHubProfiles.Select(ConvertProfile).ToList();
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
                    var groupContainer = new GroupContainer
                    {
                        ContainerId = simHubEffectsContainerBase.ContainerId, ContainerName = simHubEffectsContainerBase.ContainerName,
                        Description = simHubEffectsContainerBase.Description, Gain = simHubEffectsContainerBase.Gain,
                        IsMuted = simHubEffectsContainerBase.IsMuted
                    };
                    effectsContainerBases.Add(groupContainer);
                    ConvertEffectsContainers(simHubGroupContainer.EffectsContainers, groupContainer.EffectsContainers);
                }
                else
                {
                    var effectsContainerBase = new EffectsContainerBase
                    {
                        ContainerId = simHubEffectsContainerBase.ContainerId, ContainerName = simHubEffectsContainerBase.ContainerName,
                        Description = simHubEffectsContainerBase.Description, Gain = simHubEffectsContainerBase.Gain,
                        IsMuted = simHubEffectsContainerBase.IsMuted
                    };
                    effectsContainerBases.Add(effectsContainerBase);
                }
            }
        }
    }
}
