using System;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public abstract class CacheablePropertyInfo
    {
        private protected Func<object?>? UntypedSetDefault;

        protected CacheablePropertyInfo(Type declaringType, Type valueType)
        {
            DeclaringType = declaringType;
            ValueType = valueType;
        }

        /// <summary>
        /// The declaring type of the property or field.
        /// </summary>
        public Type DeclaringType { get; }

        public Type ValueType { get; }

        public RedisValue FieldName { get; set; }

        public Func<object, RedisValue>? Get { get; set; }

        public Action<object, RedisValue>? Set { get; set; }

        public Func<object?>? SetDefault
        {
            get => UntypedSetDefault;
            set => SetDefaultSetter(value);
        }

        private protected abstract void SetDefaultSetter(Delegate? setter);
    }

    public sealed class CacheablePropertyInfo<T> : CacheablePropertyInfo
    {
        private Func<T>? _typedSetDefault;

        internal CacheablePropertyInfo(Type declaringType, Type valueType) : base(declaringType, valueType)
        {
        }

        internal new Func<T>? SetDefault
        {
            get => _typedSetDefault;
            set => SetDefaultSetter(value);
        }

        private protected override void SetDefaultSetter(Delegate? setter)
        {
            if (setter is null)
            {
                _typedSetDefault = null;
                UntypedSetDefault = null;
            }
            else if (setter is Func<T> typedDefaultSetter)
            {
                _typedSetDefault = typedDefaultSetter;
                UntypedSetDefault = setter as Func<object?> ?? (() => typedDefaultSetter());
            }
            else
            {
                var untypedSetDefault = (Func<object?>)setter;
                _typedSetDefault = () => (T)untypedSetDefault();
                UntypedSetDefault = untypedSetDefault;
            }
        }
    }
}