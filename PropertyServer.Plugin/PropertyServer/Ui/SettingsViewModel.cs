// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using log4net;
using SimHub.Plugins.PreCommon.Ui.Util;
using SimHub.Plugins.PropertyServer.AutoUpdate;
using SimHub.Plugins.PropertyServer.Settings;
using SimHub.Plugins.Styles;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// ViewModel for the "Settings" control.
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SettingsViewModel));
        private readonly ISimHub _simHub;
        private readonly GeneralSettings _settings;
        private readonly AutoUpdater _autoUpdater = new AutoUpdater();

        public event EventHandler LogLevelChangedEvent;
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

        #region Version Check Properties

        private string _versionCheckMessage = string.Empty;
        public string VersionCheckMessage
        {
            get => _versionCheckMessage;
            set => SetProperty(ref _versionCheckMessage, value);
        }

        private bool _isNewVersionAvailable;
        public bool IsNewVersionAvailable
        {
            get => _isNewVersionAvailable;
            set => SetProperty(ref _isNewVersionAvailable, value);
        }

        private bool _isVersionCheckError;
        public bool IsVersionCheckError
        {
            get => _isVersionCheckError;
            set => SetProperty(ref _isVersionCheckError, value);
        }

        public ICommand CheckNewVersionCommand { get; }

        #endregion

        public SettingsViewModel(ISimHub simHub, GeneralSettings settings)
        {
            _simHub = simHub;
            _settings = settings;
            _ = Task.Run(CheckForNewVersion);
            PopulateFromSettings(settings);
            CheckNewVersionCommand = new RelayCommand<object>(o => Task.Run(CheckForNewVersion));
        }

        private void PopulateFromSettings(GeneralSettings settings)
        {
            this.Port = settings.Port;
            this.LogLevels = new List<LogLevelSetting>(Enum.GetValues(typeof(LogLevelSetting)).Cast<LogLevelSetting>());
            this.SelectedLogLevel = settings.LogLevel;
        }

        private async Task CheckForNewVersion()
        {
            await CheckForNewVersion(false);
        }

        public async Task CheckForNewVersion(bool testMode)
        {
            IsVersionCheckError = false;
            IsNewVersionAvailable = false;
            VersionCheckMessage = string.Empty;

            try
            {
                var versionInfo = testMode
                    ? new GitHubVersionInfo { RawTagName = "v99.99.1", Draft = false, Prerelease = false }
                    : await _autoUpdater.GetLatestVersion();

                var currentVersion = new Version(ThisAssembly.AssemblyFileVersion);
                var latestVersion = new Version(versionInfo.TagName);
                if (latestVersion > currentVersion && !versionInfo.Draft && !versionInfo.Prerelease)
                {
                    IsNewVersionAvailable = true;
                    VersionCheckMessage = $"New version available: {versionInfo.TagName}";
                }
                else
                {
                    IsNewVersionAvailable = false;
                    VersionCheckMessage = "You are using the latest version.";
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error checking for new version: {ex.Message}");
                IsVersionCheckError = true;
                VersionCheckMessage = $"Error checking for new version: {ex.Message}";
            }
        }

        public async Task Update()
        {
            try
            {
                await _autoUpdater.Update();
                _simHub.RestartSimHub();
            }
            catch (Exception ex)
            {
                Log.Error("Updated failed", ex);
                await SHMessageBox.Show($"Update failed: {ex.Message}.\n" +
                                        "See \"Logs\\PropertyServer.log\" for details.",
                    "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
