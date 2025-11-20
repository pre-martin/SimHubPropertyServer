// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Input;
using log4net;
using Newtonsoft.Json.Linq;
using SimHub.Plugins.PreCommon.Ui.Util;
using SimHub.Plugins.PropertyServer.Settings;

namespace SimHub.Plugins.PropertyServer.Ui
{
    /// <summary>
    /// ViewModel for the "Settings" control.
    /// </summary>
    public class SettingsViewModel : ObservableObject
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SettingsViewModel));
        private const string GitHubApiUrl = "https://api.github.com/repos/pre-martin/SimHubPropertyServer/releases/latest";
        private readonly GeneralSettings _settings;
        private readonly HttpClient _httpClient = new HttpClient();

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

        public ICommand CheckNewVersionCheckCommand { get; }

        #endregion

        #region Download Properties

        public ICommand DownloadCommand { get; }

        #endregion


        public SettingsViewModel(GeneralSettings settings)
        {
            _settings = settings;
            _ = Task.Run(CheckForNewVersion);
            PopulateFromSettings(settings);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "SimHubPropertyServer-Updater");
            CheckNewVersionCheckCommand = new RelayCommand<object>(o => Task.Run(CheckForNewVersion));
            DownloadCommand = new RelayCommand<object>(o => { });
        }

        /// <summary>
        /// Only used for UI design.
        /// </summary>
        public SettingsViewModel() : this(new GeneralSettings())
        {
        }

        private void PopulateFromSettings(GeneralSettings settings)
        {
            this.Port = settings.Port;
            this.LogLevels = new List<LogLevelSetting>(Enum.GetValues(typeof(LogLevelSetting)).Cast<LogLevelSetting>());
            this.SelectedLogLevel = settings.LogLevel;
        }

        private async Task CheckForNewVersion()
        {
            IsVersionCheckError = false;
            IsNewVersionAvailable = false;
            VersionCheckMessage = string.Empty;
            try
            {
                var jsonString = await _httpClient.GetStringAsync(GitHubApiUrl);
                var jsonObject = JObject.Parse(jsonString);
                var tagName = (string)jsonObject["tag_name"];
                var isDraft = (bool?)jsonObject["draft"] ?? false;
                var isPrerelease = (bool?)jsonObject["prerelease"] ?? false;
                if (string.IsNullOrEmpty(tagName))
                {
                    throw new Exception("GitHub release tag_name not found.");
                }
                var currentVersion = new Version(ThisAssembly.AssemblyFileVersion);
                var latestVersion = new Version(tagName.TrimStart('v'));
                if (latestVersion > currentVersion && !isDraft && !isPrerelease)
                {
                    IsNewVersionAvailable = true;
                    VersionCheckMessage = $"New version available: {tagName}";
                }
                else
                {
                    IsNewVersionAvailable = false;
                    VersionCheckMessage = "You are using the latest version.";
                }
            }
            catch (Exception ex)
            {
                IsVersionCheckError = true;
                VersionCheckMessage = "Could not check for new version.";
                Log.Error($"Version check failed: {ex.Message}");
            }
        }
    }
}
