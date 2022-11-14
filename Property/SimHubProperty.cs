// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SimHub.Plugins.PropertyServer.Property
{
    public class ValueChangedEventArgs : EventArgs
    {
        public ValueChangedEventArgs(SimHubProperty property)
        {
            Property = property;
        }

        public SimHubProperty Property { get; }
    }

    public abstract class SimHubProperty
    {
        private readonly List<Func<ValueChangedEventArgs, Task>> _subscribers =
            new List<Func<ValueChangedEventArgs, Task>>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private object _oldValue;
        private object _value;

        protected SimHubProperty(PropertySource propertySource, string propertyName)
        {
            PropertySource = propertySource;
            Name = propertyName;
        }

        public PropertySource PropertySource { get; }
        public string Name { get; }
        public string Type => TypeToString(GetPropertyType());
        public string ValueAsString => _value != null ? _value.ToString() : "(null)";
        public bool HasSubscribers => SubscriberCount > 0;
        public int SubscriberCount => _subscribers.Count;

        public async Task AddSubscriber(Func<ValueChangedEventArgs, Task> callback)
        {
            await _semaphore.WaitAsync();
            try
            {
                _subscribers.Add(callback);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveSubscriber(Func<ValueChangedEventArgs, Task> callback)
        {
            await _semaphore.WaitAsync();
            try
            {
                _subscribers.Remove(callback);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        protected abstract object GetValue(object obj);
        protected abstract Type GetPropertyType();

        private async Task FireValueChangedEvent()
        {
            var valueChangedEvent = new ValueChangedEventArgs(this);
            // Clone the list. This is still not really thread safe.
            var localSubscribers = new List<Func<ValueChangedEventArgs, Task>>(_subscribers);
            foreach (var subscriber in localSubscribers)
            {
                await subscriber.Invoke(valueChangedEvent);
            }
        }

        public async Task UpdateFromObject(object obj)
        {
            var newValue = obj != null ? GetValue(obj) : null;
            if (_oldValue == null && newValue != null || _oldValue != null && newValue == null ||
                (_oldValue != null && !_oldValue.Equals(newValue)))
            {
                _value = newValue;
                _oldValue = newValue;
                await FireValueChangedEvent();
            }
        }

        private string TypeToString(Type type)
        {
            switch (type.FullName)
            {
                case "System.Boolean":
                    return "boolean";
                case "System.Int32":
                    return "integer";
                case "System.Int64":
                    return "long";
                default:
                    return "(unknown)";
            }
        }
    }

    /// <summary>
    /// Manages the access to a SimHub property which is a "getter".
    /// </summary>
    public class SimHubPropertyGetter : SimHubProperty
    {
        private readonly PropertyInfo _propertyInfo;

        public SimHubPropertyGetter(PropertySource propertySource, string propertyName, PropertyInfo propertyInfo) :
            base(propertySource, propertyName)
        {
            _propertyInfo = propertyInfo;
        }

        protected override object GetValue(object obj)
        {
            return _propertyInfo.GetValue(obj);
        }

        protected override Type GetPropertyType()
        {
            return _propertyInfo.PropertyType;
        }
    }

    /// <summary>
    /// Manages the access to a SimHub property, which is a parameterless method with return value.
    /// </summary>
    public class SimHubPropertyMethod : SimHubProperty
    {
        private readonly MethodInfo _methodInfo;

        public SimHubPropertyMethod(PropertySource propertySource, string propertyName, MethodInfo methodInfo) : base(
            propertySource, propertyName)
        {
            _methodInfo = methodInfo;
        }

        protected override object GetValue(object obj)
        {
            return _methodInfo.Invoke(obj, null);
        }

        protected override Type GetPropertyType()
        {
            return _methodInfo.ReturnType;
        }
    }
}