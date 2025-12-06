using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class CacheTimeSpanConverter : CacheValueConverter<TimeSpan>
{
    // private readonly string _name;
    //
    // public CacheTimeSpanConverter(string name)
    // {
    //     _name = name;
    // }
    public override RedisValue GetBytes(TimeSpan value)
    {
        return (long)value.TotalMilliseconds;
    }

    public override TimeSpan Parse(RedisValue value)
    {
        return TimeSpan.FromMilliseconds((long)value);
    }
}