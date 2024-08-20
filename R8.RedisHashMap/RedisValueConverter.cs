using System;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public abstract class RedisValueConverter
    {
        public abstract RedisValue ConvertToRedisValue(object value);
        public abstract object ConvertFromRedisValue(RedisValue value);
    }

    public class RedisValueConverter<T> : RedisValueConverter
    {
        public override RedisValue ConvertToRedisValue(object value)
        {
            throw new NotImplementedException();
        }

        public override object ConvertFromRedisValue(RedisValue value)
        {
            throw new NotImplementedException();
        }
    }
}