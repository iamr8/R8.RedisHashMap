using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class CacheGeneric<T> : CacheValueConverter<T>
{
    public override RedisValue GetBytes(T value)
    {
        throw new NotImplementedException();
    }

    public override T Parse(RedisValue value)
    {
        throw new NotImplementedException();
    }
}