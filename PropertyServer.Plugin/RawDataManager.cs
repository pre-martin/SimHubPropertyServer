﻿// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Reflection;
using log4net;

namespace SimHub.Plugins.PropertyServer
{
    /// <summary>
    /// Handles the access to game specific raw data.
    /// </summary>
    public class RawDataManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(RawDataManager));

        private string _currentRawDataType;
        private FieldInfo _accGraphicsField;
        private FieldInfo _accPhysicsField;

        public object AccGraphics;
        public object AccPhysics;

        /// <summary>
        /// Has to be called in the game loop. Only after a call to this method, the properties in this class will return
        /// valid data.
        /// </summary>
        public void UpdateObjects(object rawData)
        {
            if (rawData == null)
            {
                Reset();
                return;
            }

            var rawDataType = rawData.GetType().FullName;
            if (rawDataType != null && rawDataType != _currentRawDataType)
            {
                // Type of raw data has changed. We have a new game, so configure the access via reflection now.
                Reset();
                _currentRawDataType = rawDataType;
                Log.Info("Detected a new game");

                if (rawDataType.EndsWith("ACCRawData"))
                {
                    // Assetto Corsa Competizione
                    Log.Info("New game is ACC");
                    _accGraphicsField = rawData.GetType().GetField("Graphics");
                    _accPhysicsField = rawData.GetType().GetField("Physics");
                }
                else
                {
                    Log.Info($"Don't know how to handle {rawDataType}. Access to raw data is not possible.");
                }
            }

            AccGraphics = _accGraphicsField?.GetValue(rawData);
            AccPhysics = _accPhysicsField?.GetValue(rawData);
        }

        private void Reset()
        {
            _currentRawDataType = null;
            _accGraphicsField = null;
            _accPhysicsField = null;
            AccGraphics = null;
            AccPhysics = null;
        }
    }
}