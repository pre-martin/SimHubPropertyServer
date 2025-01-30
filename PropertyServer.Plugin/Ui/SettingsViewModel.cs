// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using SimHub.Plugins.PropertyServer.Settings;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// ViewModel for the "Settings" control.
    /// </summary>
    public class SettingsViewModel
    {
        //  As the data never changes in the underlying model, we do not need a NotifyPropertyChanged mechanism.

        private readonly GeneralSettings _settings;

        public event EventHandler LogLevelChangedEvent;

        public SettingsViewModel(GeneralSettings settings)
        {
            _settings = settings;
            PopulateFromSettings(settings);
        }

        /// <summary>
        /// Only used for UI design.
        /// </summary>
        public SettingsViewModel()
        {
        }

        public string Version => "Version " + ThisAssembly.AssemblyFileVersion;
        public int Port { get => _settings.Port; set => _settings.Port = value; }
        public List<LogLevelSetting> LogLevels { get; private set; }
        public LogLevelSetting SelectedLogLevel
        {
            get => _settings.LogLevel;
            set
            {
                _settings.LogLevel = value;
                LogLevelChangedEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        private void PopulateFromSettings(GeneralSettings settings)
        {
            this.Port = settings.Port;
            this.LogLevels = new List<LogLevelSetting>(Enum.GetValues(typeof(LogLevelSetting)).Cast<LogLevelSetting>());
            this.SelectedLogLevel = settings.LogLevel;
        }
    }
}