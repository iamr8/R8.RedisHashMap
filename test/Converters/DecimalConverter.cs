using System.Globalization;
using StackExchange.Redis;

namespace R8.RedisHashMap.Tests.Converters;

public class DecimalConverter : CacheValueConverter<decimal>
{
    public override RedisValue GetBytes(decimal value)
    {
        // Convert decimal to string to preserve precision
        return value.ToString("G", CultureInfo.InvariantCulture);
    }

    public override decimal Parse(RedisValue value)
    {
        // Parse string back to decimal
        return decimal.Parse((string)value!, CultureInfo.InvariantCulture);
    }
}