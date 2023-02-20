// Copyright (C) 2023 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SimHub.Plugins.PropertyServer.Settings;
using SimHub.Plugins.PropertyServer.Ui.Util;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// ViewModel for the "Settings" control.
    /// </summary>
    public class SettingsViewModel
    {
        //  As the data never changes in the underlying model, we do not need a NotifyPropertyChanged mechanism.

        private readonly GeneralSettings _settings;

        public ICommand SaveSettings { get; }
        public event EventHandler SaveSettingsEvent;

        public SettingsViewModel(GeneralSettings settings)
        {
            _settings = settings;
            PopulateFromSettings(settings);
            SaveSettings = new RelayCommand<object>(e => HasChanges, o =>
                {
                    WriteIntoSettings(settings);
                    SaveSettingsEvent?.Invoke(this, EventArgs.Empty);
                }
            );
        }

        /// <summary>
        /// Only used for UI design.
        /// </summary>
        public SettingsViewModel()
        {
        }

        public string Version => "Version " + ThisAssembly.AssemblyFileVersion;
        public int Port { get; set; }
        public List<LogLevelSetting> LogLevels { get; set; }
        public LogLevelSetting SelectedLogLevel { get; set; }

        private void PopulateFromSettings(GeneralSettings settings)
        {
            this.Port = settings.Port;
            this.LogLevels = new List<LogLevelSetting>(Enum.GetValues(typeof(LogLevelSetting)).Cast<LogLevelSetting>());
            this.SelectedLogLevel = settings.LogLevel;
        }

        private void WriteIntoSettings(GeneralSettings settings)
        {
            settings.Port = Port;
            settings.LogLevel = SelectedLogLevel;
        }

        private bool HasChanges => Port != _settings.Port || SelectedLogLevel != _settings.LogLevel;
    }
}