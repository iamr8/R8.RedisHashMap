using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace R8.RedisHashMap.Test;

// [CacheableOptions(IncludeFields = false, IncludePrivate = false, GenerationOption = CacheableGenerationOption.Hierarchy | CacheableGenerationOption.Json)]
public class UserDto : ICacheable
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string? Email { get; set; }
    public string? Mobile { get; set; }
    public UserRoleType[] Roles { get; set; }
    public string[] Tags { get; set; }
    public Dictionary<string, string> Data { get; set; }
}

public class Nested
{
}

public enum UserRoleType
{
    User = 0,
    Admin = 1,
}

[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(Dictionary<int, UserDto>))]
public partial class CustomCacheableContext : JsonSerializerContext
{
}