using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class StringCacheConverter : CacheValueConverter<string>
{
    public override RedisValue GetBytes(string value)
    {
        return value;
    }

    public override string Parse(RedisValue value)
    {
        return value.IsNullOrEmpty ? null : (string)value;
    }
}