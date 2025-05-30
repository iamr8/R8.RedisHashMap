using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace R8.RedisHashMap.Test;

// [CacheableOptions(IncludeFields = false, IncludePrivate = false, GenerationOption = CacheableGenerationOption.Hierarchy | CacheableGenerationOption.Json)]
public partial class UserDto : ICacheable
{
    [JsonPropertyName("id")] public int Id { get; set; }

    // [CacheableConverter(typeof(StringRedisConverter))]
    public string FirstName { get; set; }

    [JsonPropertyName("lastName")] public string LastName { get; set; }

    [CacheablePropertyName("email_address")]
    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public int Age { get; set; }

    // public UserRoleType CurrentRole { get; set; }
    
    // [CacheableConverter(typeof(RolesArrayRedisConverter))]
    public UserRoleType[] Roles { get; set; }

    public string[] Tags { get; set; }
    // public List<string> Names { get; set; }
    public Dictionary<string, string> Data { get; set; }
    //
    // public JsonDocument? Document { get; set; }
    //
    // public JsonElement? ElementNullable { get; set; }
    //
    // public JsonElement Element { get; set; }
    //
    // public UserDto? Parent { get; set; }
    //
    // public Result<string> Result { get; set; }
    //
    // public Nested Nested { get; set; }
    // public DateTime RegistrationDate { get; set; }
    //
    // public ReadOnlyMemory<byte> RawData { get; set; }
    //
    // public byte[] Keys { get; set; }
    //
    // public Dictionary<int, UserDto> RelatedUsers { get; set; }
    //
    // [CacheableConverter(typeof(RedisTimeSpanConverter))]
    // public TimeSpan? DurationNullable { get; set; }
    //
    // [CacheableConverter(typeof(RedisTimeSpanConverter))]
    // public TimeSpan DurationWithConverter { get; set; }
    //
    // public TimeSpan Duration { get; set; }
}