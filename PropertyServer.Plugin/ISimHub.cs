// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System.Collections.Generic;
using SimHub.Plugins.PropertyServer.ShakeIt;

namespace SimHub.Plugins.PropertyServer
{
    /// <summary>
    /// Interface used to communicate with SimHub.
    /// </summary>
    public interface ISimHub
    {
        /// <summary>
        /// Triggers an input in SimHub.
        /// </summary>
        void TriggerInput(string inputName);

        /// <summary>
        /// Returns the structure of the ShakeIt Bass configuration (profiles with effect groups and effects).
        /// </summary>
        ICollection<Profile> ShakeItBassStructure();
    }
}