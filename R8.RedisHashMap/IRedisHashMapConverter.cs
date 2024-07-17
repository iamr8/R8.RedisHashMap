using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public interface IRedisHashMapConverter<T>
    {
        T InitObject(RedisValue value);
        
        RedisValue Read(T value);
    }
}