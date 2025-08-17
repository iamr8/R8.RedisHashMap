using R8.RedisHashMap.Test.Converters;
using R8.RedisHashMap.Test.Models;

namespace R8.RedisHashMap.Test.Objects;

public class AdminDto
{
    [CacheConverter(typeof(IdStringCacheConverter))]
    public int Id { get; set; }

    [CacheConverter(typeof(StringCacheConverter))]
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public UserRoleType CurrentRole { get; set; }

    [CacheConverter(typeof(RolesArrayCacheConverter))]
    public UserRoleType[] Roles { get; set; }
}