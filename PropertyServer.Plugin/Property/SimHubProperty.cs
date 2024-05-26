// Copyright (C) 2022 Martin Renner
// LGPL-3.0-or-later (see file COPYING and COPYING.LESSER)

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SimHub.Plugins.PropertyServer.ShakeIt;

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
                case "System.Single":
                    return "double";
                case "System.Double":
                    return "double";
                case "System.TimeSpan":
                    return "timespan";
                case "System.String":
                    return "string";
                case "System.Object":
                    return "object";
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
    /// Manages the access to a SimHub property which is a field.
    /// </summary>
    public class SimHubPropertyField : SimHubProperty
    {
        private readonly FieldInfo _fieldInfo;

        public SimHubPropertyField(PropertySource propertySource, string propertyName, FieldInfo fieldInfo) :
            base(propertySource, propertyName)
        {
            _fieldInfo = fieldInfo;
        }

        protected override object GetValue(object obj)
        {
            return _fieldInfo.GetValue(obj);
        }

        protected override Type GetPropertyType()
        {
            return _fieldInfo.FieldType;
        }
    }

    /// <summary>
    /// Manages the access to a SimHub property, which is a parameterless method with return value.
    /// </summary>
    public class SimHubPropertyMethod : SimHubProperty
    {
        private readonly MethodInfo _methodInfo;

        public SimHubPropertyMethod(PropertySource propertySource, string propertyName, MethodInfo methodInfo) :
            base(propertySource, propertyName)
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

    /// <summary>
    /// This is a simple delegate that tries to retrieve the property value directly from the PluginManager.
    /// </summary>
    /// <remarks>
    /// This method is much slower than the other property implementations. Use it only if the property is not available through
    /// the other implementations.
    /// </remarks>
    public class SimHubPropertyGeneric : SimHubProperty
    {
        private Type _lastSeenType;

        public SimHubPropertyGeneric(string propertyName) : base(PropertySource.Generic, propertyName)
        {
        }

        protected override object GetValue(object obj)
        {
            var value = obj is PluginManager pm ? pm.GetPropertyValue(Name) : null;
            _lastSeenType = value == null ? typeof(object) : value.GetType();
            return value;
        }

        protected override Type GetPropertyType()
        {
            return _lastSeenType ?? typeof(object);
        }
    }

    /// <summary>
    /// A property which is mapped on a special view of all available ShakeIt Bass and ShakeIt Motors effect groups and effects.
    /// </summary>
    /// <remarks>
    /// The special view is defined by the classes in the namespace <see cref="ShakeIt"/>.
    /// </remarks>
    public abstract class SimHubPropertyShakeItBase : SimHubProperty
    {
        public Guid Guid { get; }
        private readonly Property _property;

        public enum Property
        {
            Gain,
            IsMuted
        }

        protected SimHubPropertyShakeItBase(PropertySource source, string propertyName, Guid guid, Property property) : base(source, propertyName)
        {
            Guid = guid;
            _property = property;
        }

        protected abstract EffectsContainerBase GetEffect(ShakeItAccessor shakeItAccessor, Guid guid);

        protected override object GetValue(object obj)
        {
            if (obj is ShakeItAccessor accessor)
            {
                var effect = GetEffect(accessor, Guid);
                if (effect == null) return null;

                switch (_property)
                {
                    case Property.Gain:
                        return effect.Gain;
                    case Property.IsMuted:
                        return effect.IsMuted;
                    default:
                        return null;
                }
            }

            return null;
        }

        protected override Type GetPropertyType()
        {
            switch (_property)
            {
                case Property.Gain:
                    return typeof(double);
                case Property.IsMuted:
                    return typeof(bool);
                default:
                    return typeof(object);
            }
        }
    }

    /// <summary>
    /// A property which is mapped on a special view of all available ShakeIt Bass effect groups and effects.
    /// </summary>
    public class SimHubPropertyShakeItBass : SimHubPropertyShakeItBase
    {
        public SimHubPropertyShakeItBass(string propertyName, Guid guid, Property property) : base(PropertySource.ShakeItBass, propertyName, guid, property)
        {
        }

        protected override EffectsContainerBase GetEffect(ShakeItAccessor shakeItAccessor, Guid guid)
        {
            return shakeItAccessor.FindBassEffect(guid);
        }
    }

    /// <summary>
    /// A property which is mapped on a special view of all available ShakeIt Motors effect groups and effects.
    /// </summary>
    public class SimHubPropertyShakeItMotors : SimHubPropertyShakeItBase
    {
        public SimHubPropertyShakeItMotors(string propertyName, Guid guid, Property property) : base(PropertySource.ShakeItMotors, propertyName, guid, property)
        {
        }

        protected override EffectsContainerBase GetEffect(ShakeItAccessor shakeItAccessor, Guid guid)
        {
            return shakeItAccessor.FindMotorsEffect(guid);
        }
    }

}