using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class StringRedisConverter : RedisValueConverter<string>
{
    public override RedisValue ToRedisValue(string value)
    {
        return value;
    }

    public override string FromRedisValue(RedisValue value)
    {
        return value.IsNullOrEmpty ? null : (string)value;
    }
}