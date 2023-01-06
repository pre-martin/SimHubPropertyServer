// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using log4net.Core;

namespace SimHub.Plugins.PropertyServer.Settings
{
    public enum LogLevelSetting
    {
        Debug,
        Info
    }

    public static class LogLevelSettingEx
    {
        public static Level ToLog4Net(this LogLevelSetting setting)
        {
            switch (setting)
            {
                case LogLevelSetting.Debug:
                    return Level.Debug;
                case LogLevelSetting.Info:
                    return Level.Info;
                default:
                    return Level.Info;
            }
        }
    }

}