// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using GameReaderCommon;

namespace SimHub.Plugins.PropertyServer.Property
{
    /// <summary>
    /// Possible sources for SimHub properties.
    /// </summary>
    public enum PropertySource
    {
        /// <summary>
        /// For properties which can be found in the class <c>GameReaderCommon.GameData</c>
        /// </summary>
        GameData,
        /// <summary>
        /// For properties which can be found in the class <c>GameReaderCommon.GameData.StatusDataBase</c>
        /// </summary>
        StatusDataBase
    }

    public static class PropertySourceEx
    {
        public static Type GetPropertySourceType(this PropertySource propertySource)
        {
            switch (propertySource)
            {
                case PropertySource.GameData:
                    return typeof(GameData);
                case PropertySource.StatusDataBase:
                    return typeof(StatusDataBase);
                default:
                    throw new ArgumentException($"Unknown PropertySource {propertySource}");
            }
        }

        public static string GetPropertyPrefix(this PropertySource propertySource)
        {
            switch (propertySource)
            {
                case PropertySource.GameData:
                    return "gd";
                case PropertySource.StatusDataBase:
                    return "gd.sdb";
                default:
                    throw new ArgumentException($"Unknown PropertySource {propertySource}");
            }
        }
    }
}