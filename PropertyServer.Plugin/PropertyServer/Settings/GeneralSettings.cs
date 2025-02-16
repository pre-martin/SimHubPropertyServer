// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace SimHub.Plugins.PropertyServer.Settings
{
    public class GeneralSettings
    {
        public int Port { get; set; } = 18082;

        [JsonConverter(typeof(StringEnumConverter))]
        public LogLevelSetting LogLevel { get; set; } = LogLevelSetting.Info;
    }
}