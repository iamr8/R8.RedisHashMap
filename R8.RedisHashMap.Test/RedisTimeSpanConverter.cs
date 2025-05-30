using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

public partial class RedisTimeSpanConverter : RedisValueConverter<TimeSpan>
{
    public override RedisValue ConvertToRedisValue(TimeSpan value)
    {
        return (long)value.TotalMilliseconds;
    }

    public override TimeSpan ConvertFromRedisValue(RedisValue value)
    {
        return TimeSpan.FromMilliseconds((long)value);
    }
}