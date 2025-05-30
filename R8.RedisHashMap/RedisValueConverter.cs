using System;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public abstract class RedisValueConverter
    {
    }

    public class RedisValueConverter<T> : RedisValueConverter, IRedisValueConverter<T>
    {
        public virtual RedisValue ConvertToRedisValue(T value)
        {
            throw new InvalidOperationException("This class must be inherited.");
        }

        public virtual T ConvertFromRedisValue(RedisValue value)
        {
            throw new InvalidOperationException("This class must be inherited.");
        }
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