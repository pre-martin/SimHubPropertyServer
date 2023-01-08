// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Reflection;
using SimHub.Plugins.DataPlugins.ShakeItV3;
using SimHub.Plugins.DataPlugins.ShakeItV3.Settings;

namespace SimHub.Plugins.PropertyServer
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
        /// Initialises the accessor. Calls to <see cref="CurrentProfile"/> will only be successful, if this method was called once.
        /// </summary>
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
        /// Returns the current profile of the plugin "ShakeIt Bass Shakers".
        /// </summary>
        /// <returns>The current profile or <c>null</c> if the plugin is not initialized or the reflective access fails.</returns>
        public ShakeItProfile CurrentProfile()
        {
            if (_shakeItSettingsField == null) return null;

            var shakeItPlugin = _pluginManager.GetPlugin<ShakeITBSV3Plugin>();
            if (_shakeItSettingsField.GetValue(shakeItPlugin) is ShakeItSettings settings)
            {
                return settings.CurrentProfile;
            }

            return null;
        }
    }
}