using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

public class StringRedisConverter : RedisValueConverter<string>
{
    public override RedisValue ConvertToRedisValue(string value)
    {
        return value;
    }

    public override string ConvertFromRedisValue(RedisValue value)
    {
        return value.IsNullOrEmpty ? null : (string)value;
    }
}