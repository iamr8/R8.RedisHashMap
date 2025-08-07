using System.Text.Json;
using R8.RedisHashMap.Test.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Converters;

public class RolesArrayRedisConverter : RedisValueConverter<UserRoleType[]>
{
    public override RedisValue ToRedisValue(UserRoleType[] value)
    {
        return JsonSerializer.Serialize(value);
    }

    public override UserRoleType[] FromRedisValue(RedisValue value)
    {
        return JsonSerializer.Deserialize<UserRoleType[]>(value);
    }
}