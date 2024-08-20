using System;
using System.ComponentModel;
using System.Text.Json.Serialization.Metadata;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class CacheablePropertyInfoValues<T>
    {
        /// <summary>
        /// The declaring type of the property or field.
        /// </summary>
        public Type DeclaringType { get; set; } = null!;

        public Func<T>? DefaultCreator { get; set; }

        /// <summary>
        /// Provides a mechanism to get the property or field's value.
        /// </summary>
        public Func<object, T>? Getter { get; set; }

        /// <summary>
        /// Provides a mechanism to set the property or field's value.
        /// </summary>
        public Action<object, T>? Setter { get; set; }

        /// <summary>
        /// The name of the property or field.
        /// </summary>
        public string PropertyName { get; set; } = null!;
        
        public Type ValueType { get; set; } = null!;

        public RedisValueConverter? Converter { get; set; }
        public Func<T, RedisValue>? Generator { get; set; }
        public Func<RedisValue, T>? Parser { get; set; }
    }
}