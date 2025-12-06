using System.Text.Json;
using R8.RedisHashMap;
using R8.RedisHashMap.Tests.Converters;
using R8.RedisHashMap.Tests.Models;

public class TestUser
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    
    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime CreatedAt { get; set; }
    
    public bool IsActive { get; set; }

    [CacheConverter(typeof(DecimalConverter))]
    public decimal Balance { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();

    public Dictionary<string, string> Metadata { get; set; } = new();

    // Advanced types
    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime? LastLoginAt { get; set; }

    public List<string>? Roles { get; set; }
    public byte[]? ProfilePicture { get; set; }
    public JsonDocument? Settings { get; set; }
    public Result<string>? LastAction { get; set; }
}