using StackExchange.Redis;

namespace R8.RedisHashMap.Tests.Converters;

public class TimeSpanConverter : CacheValueConverter<TimeSpan>
{
    public override RedisValue GetBytes(TimeSpan value)
    {
        return (long)value.TotalMilliseconds;
    }

    public override TimeSpan Parse(RedisValue value)
    {
        return TimeSpan.FromMilliseconds((long)value);
    }
}