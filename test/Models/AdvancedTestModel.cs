using System.Text.Json;
using R8.RedisHashMap.Tests.Converters;
using StackExchange.Redis;

namespace R8.RedisHashMap.Tests.Models;

/// <summary>
///     Advanced test model with complex types supported by the source generator
/// </summary>
public class AdvancedTestModel
{
    // Basic types
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // DateTime support (with converter to avoid RH2001 warning)
    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime RegistrationDate { get; set; }

    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime? LastLoginDate { get; set; }

    // TimeSpan support (with converter)
    [CacheConverter(typeof(TimeSpanConverter))]
    public TimeSpan Duration { get; set; }

    [CacheConverter(typeof(TimeSpanConverter))]
    public TimeSpan? DurationNullable { get; set; }

    // Collections
    public List<string> Names { get; set; } = new();
    public byte[] Keys { get; set; } = Array.Empty<byte>();
    public IEnumerable<byte> Enumerable { get; set; } = Array.Empty<byte>();

    // Memory types
    public ReadOnlyMemory<byte> RawData { get; set; }

    // JSON types
    public JsonDocument? Document { get; set; }
    public JsonElement? ElementNullable { get; set; }
    public JsonElement Element { get; set; }

    // Generic types
    public Result<string> StringResult { get; set; } = new();
    public Result<int> IntResult { get; set; } = new();

    // Struct types
    public Nested NestedStruct { get; set; }
    public Nested? NullableNestedStruct { get; set; }

    // Decimal with converter
    [CacheConverter(typeof(DecimalConverter))]
    public decimal Price { get; set; }

    [CacheConverter(typeof(DecimalConverter))]
    public decimal? OptionalPrice { get; set; }

    // Complex dictionaries
    public Dictionary<string, int> Scores { get; set; } = new();
    public Dictionary<int, string> IdToName { get; set; } = new();
}