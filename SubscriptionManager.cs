// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using SimHub.Plugins.PropertyServer.Property;

namespace SimHub.Plugins.PropertyServer
{
    /// <summary>
    /// Handles subscriptions from clients and maps them onto properties from SimHub. The whole class is thread-safe
    /// so that it can be called from clients and from SimHub.
    /// </summary>
    public class SubscriptionManager
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(SubscriptionManager));
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Dictionary<string, SimHubProperty> _properties = new Dictionary<string, SimHubProperty>();

        /// <summary>
        /// Subscribes to a given SimHub property.
        /// </summary>
        /// <param name="propertyName">The name of the SimHub property. See class <c>GameReaderCommon.StatusDataBase</c>.</param>
        /// <param name="eventHandler">This handler will be called when the value of the property has changed.</param>
        /// <param name="errorCallback">Will be called if the subscription was not possible.</param>
        /// <returns><c>true</c> if the subscription was successful.</returns>
        public async Task<bool> Subscribe(
            string propertyName,
            Func<ValueChangedEventArgs, Task> eventHandler,
            Func<string, Task> errorCallback)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_properties.TryGetValue(propertyName, out var property))
                {
                    // We already know the property. So just add the callback and send the current value.
                    await property.AddSubscriber(eventHandler);
                    Log.Info(
                        $"Added subscription to existing property {propertyName} (has now {property.SubscriberCount} subscriptions)");
                    await eventHandler.Invoke(new ValueChangedEventArgs(property));
                    return true;
                }

                // Create a new property instance.
                property = await PropertyAccessor.CreateProperty(propertyName, errorCallback);
                if (property == null) return false;

                Log.Info($"Created new property {propertyName}");
                await property.AddSubscriber(eventHandler);
                _properties[propertyName] = property;
                // Send "null" to client. If no sim is running, the first regular update could take some time.
                await eventHandler.Invoke(new ValueChangedEventArgs(property));
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Ubsubscribes from a SimHub property.
        /// </summary>
        /// <param name="propertyName">The name of the SimHub property.</param>
        /// <param name="eventHandler">The handler that shall be removed from the property.</param>
        /// <returns><c>true</c> if the handler was unsubscribed.</returns>
        public async Task<bool> Unsubscribe(string propertyName, Func<ValueChangedEventArgs, Task> eventHandler)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_properties.TryGetValue(propertyName, out var property))
                {
                    await property.RemoveSubscriber(eventHandler);
                    Log.Info($"Removed subscription from {propertyName} ({property.SubscriberCount} remaining)");
                    if (!property.HasSubcribers)
                    {
                        // No more active subscribers on this property: Remove it.
                        Log.Info($"Removing property {propertyName}, it has no more subscriptions");
                        _properties.Remove(propertyName);
                    }

                    return true;
                }

                // We have a confused client. Print some info into the log.
                Log.Info($"Client wanted to unsubscribe from {propertyName}, but it is not known");
                return false;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// Returns a list of all SimHub properties for which subscriptions exist.
        /// </summary>
        /// <returns>A shallow clone of the internal list, which is safe to iterate, but whose elements could be stale.</returns>
        public async Task<Dictionary<string, SimHubProperty>> GetProperties()
        {
            await _semaphore.WaitAsync();
            try
            {
                return new Dictionary<string, SimHubProperty>(_properties);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}