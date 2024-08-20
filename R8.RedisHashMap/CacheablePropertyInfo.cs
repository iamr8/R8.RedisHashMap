using System;
using System.Text.Json.Serialization.Metadata;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public abstract class CacheablePropertyInfo
    {
        private protected Func<object, object?>? _untypedGet;
        private protected Action<object, object?>? _untypedSet;
        private protected Func<object?>? _UntypedSetDefault;
        private protected Func<object, RedisValue>? _untypedGenerate;
        private protected Func<RedisValue, object?>? _untypedParse;

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

        public string PropertyName { get; set; } = null!;

        public RedisValueConverter? Converter { get; set; }

        public Func<object, object?>? Get
        {
            get => _untypedGet;
            set => SetGetter(value);
        }

        public Action<object, object?>? Set
        {
            get => _untypedSet;
            set => SetSetter(value);
        }

        public Func<object?>? SetDefault
        {
            get => _UntypedSetDefault;
            set => SetDefaultSetter(value);
        }

        public Func<object, RedisValue>? Generate
        {
            get => _untypedGenerate!;
            set => SetGenerator(value);
        }

        public Func<RedisValue, object?>? Parse
        {
            get => _untypedParse!;
            set => SetParser(value);
        }

        private protected abstract void SetGetter(Delegate? getter);
        private protected abstract void SetSetter(Delegate? setter);
        private protected abstract void SetDefaultSetter(Delegate? setter);
        private protected abstract void SetGenerator(Delegate? setter);
        private protected abstract void SetParser(Delegate? setter);
    }

    public sealed class CacheablePropertyInfo<T> : CacheablePropertyInfo
    {
        private Func<object, T>? _typedGet;
        private Action<object, T>? _typedSet;
        private Func<T>? _typedSetDefault;
        private Func<T, RedisValue>? _typedGenerate;
        private Func<RedisValue, T>? _typedParse;

        internal CacheablePropertyInfo(Type declaringType, Type valueType) : base(declaringType, valueType)
        {
        }

        internal new Func<T>? SetDefault
        {
            get => _typedSetDefault;
            set => SetDefaultSetter(value);
        }

        internal new Func<object, T>? Get
        {
            get => _typedGet;
            set => SetGetter(value);
        }

        internal new Action<object, T>? Set
        {
            get => _typedSet;
            set => SetSetter(value);
        }

        internal new Func<T, RedisValue>? Generate
        {
            get => _typedGenerate;
            set => SetGenerator(value);
        }

        internal new Func<RedisValue, T>? Parse
        {
            get => _typedParse;
            set => SetParser(value);
        }

        private protected override void SetGetter(Delegate? getter)
        {
            if (getter is null)
            {
                _typedGet = null;
                _untypedGet = null;
            }
            else if (getter is Func<object, T> typedGetter)
            {
                _typedGet = typedGetter;
                _untypedGet = getter as Func<object, object?> ?? (obj => typedGetter(obj));
            }
            else
            {
                var untypedGet = (Func<object, object?>)getter;
                _typedGet = obj => (T)untypedGet(obj)!;
                _untypedGet = untypedGet;
            }
        }

        private protected override void SetSetter(Delegate? setter)
        {
            if (setter is null)
            {
                _typedSet = null;
                _untypedSet = null;
            }
            else if (setter is Action<object, T> typedSetter)
            {
                _typedSet = typedSetter;
                _untypedSet = setter as Action<object, object?> ?? ((obj, value) => typedSetter(obj, (T)value!));
            }
            else
            {
                var untypedSet = (Action<object, object?>)setter;
                _typedSet = ((obj, value) => untypedSet(obj, value));
                _untypedSet = untypedSet;
            }
        }

        private protected override void SetDefaultSetter(Delegate? setter)
        {
            if (setter is null)
            {
                _typedSetDefault = null;
                _UntypedSetDefault = null;
            }
            else if (setter is Func<T> typedDefaultSetter)
            {
                _typedSetDefault = typedDefaultSetter;
                _UntypedSetDefault = setter as Func<object?> ?? (() => typedDefaultSetter());
            }
            else
            {
                var untypedSetDefault = (Func<object?>)setter;
                _typedSetDefault = () => (T)untypedSetDefault();
                _UntypedSetDefault = untypedSetDefault;
            }
        }

        private protected override void SetGenerator(Delegate? setter)
        {
            if (setter is null)
            {
                _typedGenerate = null;
                _untypedGenerate = null;
            }
            else if (setter is Func<T, RedisValue> typedGenerator)
            {
                _typedGenerate = typedGenerator;
                _untypedGenerate = setter as Func<object, RedisValue> ?? (obj => typedGenerator((T)obj));
            }
            else
            {
                var untypedGenerator = (Func<object, RedisValue>)setter;
                _typedGenerate = obj => untypedGenerator(obj);
                _untypedGenerate = untypedGenerator;
            }
        }

        private protected override void SetParser(Delegate? setter)
        {
            if (setter is null)
            {
                _typedParse = null;
                _untypedParse = null;
            }
            else if (setter is Func<RedisValue, T> typedParser)
            {
                _typedParse = typedParser;
                _untypedParse = setter as Func<RedisValue, object> ?? (obj => typedParser(obj));
            }
            else
            {
                var untypedParser = (Func<RedisValue, object>)setter;
                _typedParse = obj => (T)untypedParser(obj);
                _untypedParse = untypedParser;
            }
        }
    }
}