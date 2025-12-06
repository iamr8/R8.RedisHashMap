using StackExchange.Redis;

namespace R8.RedisHashMap.Tests.Converters;

/// <summary>
/// Converter for DateTime that stores values as Unix timestamp (milliseconds) to preserve precision.
/// </summary>
public class DateTimeConverter : CacheValueConverter<DateTime>
{
    public override RedisValue GetBytes(DateTime value)
    {
        return value.Ticks;
    }

    public override DateTime Parse(RedisValue value)
    {
        return new DateTime((long)value!);
    }
}

