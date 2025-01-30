// Copyright (C) 2025 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using GameReaderCommon;
using log4net;
using log4net.Appender;
using log4net.Layout;
using log4net.Repository.Hierarchy;
using SimHub.Plugins.PropertyServer.Comm;
using SimHub.Plugins.PropertyServer.Property;
using SimHub.Plugins.PropertyServer.Settings;
using SimHub.Plugins.PropertyServer.ShakeIt;
using SimHub.Plugins.PropertyServer.Ui;

namespace SimHub.Plugins.PropertyServer
{
    [PluginName("Property Server")]
    [PluginAuthor("Martin Renner")]
    [PluginDescription("Provides a network server for read access to game properties - v" + ThisAssembly.AssemblyFileVersion)]
    public class PropertyServerPlugin : IDataPlugin, IWPFSettingsV2, ISimHub, IInputPlugin
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PropertyServerPlugin));
        private GeneralSettings _settings = new GeneralSettings();
        private Server _server;
        private long _lastDataUpdate;
        private readonly SubscriptionManager _subscriptionManager = new SubscriptionManager();
        private FieldInfo _rawField;
        private readonly RawDataManager _rawDataManager = new RawDataManager();
        private readonly ShakeItAccessor _shakeItAccessor = new ShakeItAccessor();
        private int _unhandledExceptionCount;

        public PluginManager PluginManager { get; set; }

        public void Init(PluginManager pluginManager)
        {
            _settings = this.ReadCommonSettings("GeneralSettings", () => new GeneralSettings());

            // Add our own log file for our plugin.
            var appender = new RollingFileAppender
            {
                File = "logs/PropertyServer.log",
                Name = "FileAppender",
                MaxFileSize = 1 * 1024 * 1024,
                MaxSizeRollBackups = 5,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                Layout = new PatternLayout(
                    "%date{yyyy-MM-dd HH:mm:ss,fff} %-5level [%-15.15thread] %-30.30logger [cid:%-6.6property{client}] %message%newline")
            };
            appender.ActivateOptions();

            var namespaceLogger = GetNamespaceLogger();
            namespaceLogger.Additivity = false;
            namespaceLogger.AddAppender(appender);
            namespaceLogger.Level = _settings.LogLevel.ToLog4Net();

            Log.Info($"Starting plugin, version {ThisAssembly.AssemblyFileVersion}");

            _shakeItAccessor.Init(pluginManager);

            // Move execution of server into a new task/thread (away from SimHub thread). The server is async, but we
            // do not want to put any unnecessary load onto the SimHub thread.
            _server = new Server(this, _subscriptionManager, _settings.Port);
            Task.Run(_server.Start);
        }

        public void End(PluginManager pluginManager)
        {
            Log.Info("Shutting down plugin");
            this.SaveCommonSettings("GeneralSettings", _settings);
            _server?.Stop();
        }

        public void DataUpdate(PluginManager pluginManager, ref GameData data)
        {
            // We are not real time. As we do reflection, we are nice to the SimHub thread.
            const long updateMillis = 100;

            var now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            if (now - _lastDataUpdate > updateMillis)
            {
                try
                {
                    DataUpdateInternal(data);
                }
                catch (Exception e)
                {
                    // We are on the critical path of the SimHub thread. Writing excessive logs does not help and is I/O intensive.
                    // So we stop reporting unhandled exceptions after a specific limit.
                    if (_unhandledExceptionCount < 50)
                    {
                        _unhandledExceptionCount++;
                        Log.Error($"Oh no, we have an unhandled exception: {e}");
                        if (_unhandledExceptionCount >= 50)
                        {
                            Log.Error($"Reached limit of more than {_unhandledExceptionCount} unhandled exceptions.");
                            Log.Error("Not reporting any more exceptions, but most probably the problem still exists.");
                        }
                    }
                }

                _lastDataUpdate = now;
            }
        }

        private async void DataUpdateInternal(GameData data)
        {
            // Game raw data is not accessible for us:
            // - "GameData<T> : GameData" has a property "StatusData<T> GameNewData" (where "StatusData<T> : StatusDataBase")
            // - "StatusData<T>" has a Field "T Raw".
            // We would like to get this "Raw" field, which would be of type "ACSharedMemory.ACC.Reader.ACCRawData". But we only
            // get "GameData" and not "GameData<T>".
            // So we use reflection, but we query the reflection data only once and save it in an instance variable.
            if (_rawField == null && data?.NewData != null)
            {
                _rawField = data.NewData.GetType().GetField("Raw");
            }

            // Now get the "Raw" data with reflection.
            object rawData = null;
            if (_rawField != null && data?.NewData != null)
            {
                rawData = _rawField.GetValue(data.NewData);
            }
            _rawDataManager.UpdateObjects(rawData);

            var properties = await _subscriptionManager.GetProperties();
            foreach (var simHubProperty in properties.Values)
            {
                // TODO Better performance: Collect all changed Properties and dispatch them in a Task
                switch (simHubProperty.PropertySource)
                {
                    case PropertySource.GameData:
                        await simHubProperty.UpdateFromObject(data);
                        break;
                    case PropertySource.StatusDataBase:
                        await simHubProperty.UpdateFromObject(data?.NewData);
                        break;
                    case PropertySource.AccGraphics:
                        await simHubProperty.UpdateFromObject(_rawDataManager.AccGraphics);
                        break;
                    case PropertySource.AccPhysics:
                        await simHubProperty.UpdateFromObject(_rawDataManager.AccPhysics);
                        break;
                    case PropertySource.Generic:
                        await simHubProperty.UpdateFromObject(PluginManager);
                        break;
                    case PropertySource.ShakeItBass:
                        await simHubProperty.UpdateFromObject(_shakeItAccessor);
                        break;
                    case PropertySource.ShakeItMotors:
                        await simHubProperty.UpdateFromObject(_shakeItAccessor);
                        break;
                    default:
                        throw new ArgumentException($"Unknown PropertySource {simHubProperty.PropertySource}");
                }
            }
        }

        private Logger GetNamespaceLogger()
        {
            var namespaceLogger = LogManager.GetLogger(typeof(PropertyServerPlugin).Namespace);
            return (Logger)namespaceLogger.Logger;
        }

        public Control GetWPFSettingsControl(PluginManager pluginManager)
        {
            var settingsViewModel = new SettingsViewModel(_settings);
            settingsViewModel.LogLevelChangedEvent += (sender, args) => GetNamespaceLogger().Level = _settings.LogLevel.ToLog4Net();
            return new SettingsControl { DataContext = settingsViewModel, PluginManager = pluginManager };
        }

        public ImageSource PictureIcon => this.ToIcon(Properties.Resources.properties);

        public string LeftMenuTitle => "Property Server";

        public void TriggerInput(string inputName)
        {
            PluginManager.TriggerInput(inputName, typeof(PropertyServerPlugin), PressType.Default);
        }

        public void TriggerInputPressed(string inputName)
        {
            PluginManager.TriggerInputPress(inputName, typeof(PropertyServerPlugin));
        }

        public void TriggerInputReleased(string inputName)
        {
            PluginManager.TriggerInputRelease(inputName, typeof(PropertyServerPlugin));
        }

        public ICollection<Profile> ShakeItBassStructure()
        {
            return _shakeItAccessor.BassProfiles();
        }

        public EffectsContainerBase FindShakeItBassEffect(Guid id)
        {
            return _shakeItAccessor.FindBassEffect(id);
        }

        public ICollection<Profile> ShakeItMotorsStructure()
        {
            return _shakeItAccessor.MotorsProfiles();
        }

        public EffectsContainerBase FindShakeItMotorsEffect(Guid id)
        {
            return _shakeItAccessor.FindMotorsEffect(id);
        }
    }
}