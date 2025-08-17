using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class IdStringCacheConverter : CacheValueConverter<int>
{
    public override RedisValue GetBytes(int value)
    {
        return value.ToString();
    }

    public override int Parse(RedisValue value)
    {
        if (value.IsNullOrEmpty) return 0;

        if (int.TryParse(value, out var result)) return result;

        throw new InvalidOperationException($"Cannot convert RedisValue '{value}' to int.");
    }
}