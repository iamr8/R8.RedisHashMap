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

#if NET7_0_OR_GREATER
        if (int.TryParse(((ReadOnlyMemory<byte>)value).Span, out var result)) return result;
#else
        if (int.TryParse(value, out var result)) return result;
#endif

        throw new InvalidOperationException($"Cannot convert RedisValue '{value}' to int.");
    }
}