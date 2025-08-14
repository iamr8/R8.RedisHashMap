using R8.RedisHashMap.Test.Converters;
using R8.RedisHashMap.Test.Models;

namespace R8.RedisHashMap.Test.Objects;

[CacheObject(
    NamingStrategy = CacheableFieldNamingStrategy.SnakeCase,
    GenerationMode = CacheableGenerationMode.Serialization
)]
public partial class AdminDto
{
    [CacheableConverter(typeof(IdStringRedisConverter))]
    public int Id { get; set; }

    [CacheableConverter(typeof(StringRedisConverter))]
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public UserRoleType CurrentRole { get; set; }

    [CacheableConverter(typeof(RolesArrayRedisConverter))]
    public UserRoleType[] Roles { get; set; }
}