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
        StatusDataBase,

        /// <summary>
        /// Assetto Corsa Competizione - Rawdata "Graphics"
        /// </summary>
        AccGraphics,

        /// <summary>
        /// Assetto Corsa Competizione - Rawdata "Physics"
        /// </summary>
        AccPhysics

    }

    public static class PropertySourceEx
    {
        /// <summary>
        /// Determines the underlying <c>Type</c> of a given <c>PropertySource</c>.
        /// </summary>
        /// <returns>The <c>Type</c> or <c>null</c> if the underlying type cannot be found.</returns>
        public static Type GetPropertySourceType(this PropertySource propertySource)
        {
            switch (propertySource)
            {
                case PropertySource.GameData:
                    return typeof(GameData);
                case PropertySource.StatusDataBase:
                    return typeof(StatusDataBase);
                case PropertySource.AccGraphics:
                    return Type.GetType("ACSharedMemory.ACC.MMFModels.Graphics, ACSharedMemory");
                case PropertySource.AccPhysics:
                    return Type.GetType("ACSharedMemory.ACC.MMFModels.Physics, ACSharedMemory");
                default:
                    throw new ArgumentException($"Unknown PropertySource {propertySource}");
            }
        }

        /// <summary>
        /// Determines the name prefix of a given <c>PropertySource</c>.
        /// </summary>
        public static string GetPropertyPrefix(this PropertySource propertySource)
        {
            switch (propertySource)
            {
                case PropertySource.GameData:
                    return "dcp"; // [DataCorePlugin.xyz]
                case PropertySource.StatusDataBase:
                    return "dcp.gd"; // [DataCorePlugin.GameData.xyz]
                case PropertySource.AccGraphics:
                    return "acc.graphics";
                case PropertySource.AccPhysics:
                    return "acc.physics";
                default:
                    throw new ArgumentException($"Unknown PropertySource {propertySource}");
            }
        }
    }
}