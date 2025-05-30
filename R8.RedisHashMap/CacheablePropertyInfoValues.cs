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
        public Func<object, RedisValue>? Getter { get; set; }

        /// <summary>
        /// Provides a mechanism to set the property or field's value.
        /// </summary>
        public Action<object, RedisValue>? Setter { get; set; }

        /// <summary>
        /// The name of the property or field.
        /// </summary>
        public RedisValue FieldName { get; set; }

        public Type ValueType { get; set; } = null!;
    }
}