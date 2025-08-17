using System.Text.Json;
using R8.RedisHashMap.Test.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class RolesArrayCacheConverter : CacheValueConverter<UserRoleType[]>
{
    public override RedisValue GetBytes(UserRoleType[] value)
    {
        return JsonSerializer.Serialize(value);
    }

    public override UserRoleType[] Parse(RedisValue value)
    {
        return JsonSerializer.Deserialize<UserRoleType[]>(value);
    }
}