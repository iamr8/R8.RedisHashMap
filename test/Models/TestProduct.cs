using System.Text.Json;
using R8.RedisHashMap.Tests.Converters;

namespace R8.RedisHashMap.Tests.Models;

public class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    [CacheConverter(typeof(DecimalConverter))]
    public decimal Price { get; set; }

    public int StockQuantity { get; set; }

    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime? LastRestocked { get; set; }

    public double Rating { get; set; }
    public bool IsAvailable { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public JsonDocument? Specifications { get; set; }

    // Advanced types
    public List<string>? RelatedProducts { get; set; }
    public Dictionary<int, string>? VariantMap { get; set; }
    public ReadOnlyMemory<byte> Thumbnail { get; set; }
    public Result<int>? StockResult { get; set; }
    public Nested? ProductCode { get; set; }
}