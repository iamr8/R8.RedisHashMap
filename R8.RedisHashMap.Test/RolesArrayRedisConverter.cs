using System.Text.Json;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

public partial class RolesArrayRedisConverter : RedisValueConverter<UserRoleType[]>
{
    public override RedisValue ConvertToRedisValue(UserRoleType[] value)
    {
        return JsonSerializer.Serialize(value);
    }

    public override UserRoleType[] ConvertFromRedisValue(RedisValue value)
    {
        return JsonSerializer.Deserialize<UserRoleType[]>(value);
    }
}