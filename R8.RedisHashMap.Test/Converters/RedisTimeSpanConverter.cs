using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class RedisTimeSpanConverter : RedisValueConverter<TimeSpan>
{
    public override RedisValue ToRedisValue(TimeSpan value)
    {
        return (long)value.TotalMilliseconds;
    }

    public override TimeSpan FromRedisValue(RedisValue value)
    {
        return TimeSpan.FromMilliseconds((long)value);
    }
}