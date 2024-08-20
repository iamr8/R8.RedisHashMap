using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public interface IRedisHashMapConverter
    {
    }

    public interface ICacheableConverter<T> : IRedisHashMapConverter
    {
        T InitObject(RedisValue value);

        RedisValue Read(T value);
    }
}