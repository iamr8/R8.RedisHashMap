using System;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public abstract class RedisValueConverter
    {
    }

    public abstract class RedisValueConverter<T> : RedisValueConverter, IRedisValueConverter<T>
    {
        public abstract RedisValue ConvertToRedisValue(T value);

        public abstract T ConvertFromRedisValue(RedisValue value);
    }

    public interface IRedisValueConverter<T>
    {
        /// <summary>
        /// Returns a <see cref="RedisValue"/> from <typeparamref name="T"/> value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A <see cref="RedisValue"/> from <typeparamref name="T"/> value.</returns>
        RedisValue ConvertToRedisValue(T value);

        /// <summary>
        /// Returns a <typeparamref name="T"/> from <see cref="RedisValue"/> value.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>A <typeparamref name="T"/> from <see cref="RedisValue"/> value.</returns>
        T ConvertFromRedisValue(RedisValue value);
    }
}