// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
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
        /// Triggers the start of an input in SimHub.
        /// </summary>
        void TriggerInputPressed(string inputName);

        /// <summary>
        /// Triggers the end of an input in SimHub.
        /// </summary>
        void TriggerInputReleased(string inputName);

        /// <summary>
        /// Returns the structure of the ShakeIt Bass configuration (profiles with effect groups and effects).
        /// </summary>
        ICollection<Profile> ShakeItBassStructure();

        /// <summary>
        /// Tries to find a ShakeIt Bass effect or effect group with the given id.
        /// </summary>
        /// <returns><c>null</c> if nothing is found.</returns>
        EffectsContainerBase FindShakeItBassEffect(Guid id);

        /// <summary>
        /// Returns the structure of the ShakeIt Motors configuration (profiles with effect groups and effects).
        /// </summary>
        ICollection<Profile> ShakeItMotorsStructure();

        /// <summary>
        /// Tries to find a ShakeIt Motors effect or effect group with the given id.
        /// </summary>
        /// <returns><c>null</c> if nothing is found.</returns>
        EffectsContainerBase FindShakeItMotorsEffect(Guid id);
    }
}