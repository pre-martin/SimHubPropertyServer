// Copyright (C) 2026 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SimHub.Plugins.PropertyServer.Settings
{
    public class GeneralSettings
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ListenAddress ListenAddress { get; set; }

        public int Port { get; set; } = 18082;

        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevelSetting LogLevel { get; set; } = LogLevelSetting.Info;
    }
}