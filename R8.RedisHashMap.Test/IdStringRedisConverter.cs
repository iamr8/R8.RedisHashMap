using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

public class IdStringRedisConverter : RedisValueConverter<int>
{
    public override RedisValue ConvertToRedisValue(int value)
    {
        return value.ToString();
    }

    public override int ConvertFromRedisValue(RedisValue value)
    {
        if (value.IsNullOrEmpty)
        {
            return 0;
        }

        if (int.TryParse(value, out var result))
        {
            return result;
        }

        throw new InvalidOperationException($"Cannot convert RedisValue '{value}' to int.");
    }
}