using System.Text.Json;
using R8.RedisHashMap.Test.Converters;
using R8.RedisHashMap.Test.Models;

namespace R8.RedisHashMap.Test;

[CacheableObject(CacheableFieldNamingStrategy.SnakeCase)]
public class UserDto
{
    [CacheableConverter(typeof(IdStringRedisConverter))]
    public int Id { get; set; }

    [CacheableConverter(typeof(StringRedisConverter))]
    public string FirstName { get; set; }

    public string LastName { get; set; }

    public string? Email { get; set; }

    public string? Mobile { get; set; }

    public int Age { get; set; }

    public UserRoleType CurrentRole { get; set; }

    [CacheableConverter(typeof(RolesArrayRedisConverter))]
    public UserRoleType[] Roles { get; set; }

    public string[] Tags { get; set; }

    public Dictionary<string, string> Data { get; set; }
    public List<string> Names { get; set; }
    
    public JsonDocument? Document { get; set; }
    
    public JsonElement? ElementNullable { get; set; }
    
    public JsonElement Element { get; set; }
    
    public UserDto? Parent { get; set; }
    
    public Result<string> Result { get; set; }
    
    public Nested Nested { get; set; }
    public DateTime RegistrationDate { get; set; }
    
    public ReadOnlyMemory<byte> RawData { get; set; }
    
    public byte[] Keys { get; set; }
    
    public Dictionary<int, UserDto> RelatedUsers { get; set; }
    
    [CacheableConverter(typeof(RedisTimeSpanConverter))]
    public TimeSpan? DurationWithConverterNullable { get; set; }
    
    [CacheableConverter(typeof(RedisTimeSpanConverter))]
    public TimeSpan DurationWithConverter { get; set; }
    
    public TimeSpan Duration { get; set; }
    
    public TimeSpan? DurationNullable { get; set; }
}