# RH2001: Property Requires JSON Serialization

## Diagnostic Information

- **ID**: RH2001
- **Severity**: Warning
- **Category**: SourceGenerator

## Description

This warning is reported when a property in a type registered with `[CacheObject]` will be serialized/deserialized using JSON because it's not natively supported by Redis and doesn't have a custom converter.

## Cause

The source generator will use JSON serialization/deserialization for properties with types that:

1. **Don't have a custom converter** (no `[CacheConverter]` attribute)
2. **Aren't natively supported by Redis**, which includes:
   - Primitive types (int, bool, double, etc.)
   - Enums
   - string
   - byte[]
   - ReadOnlyMemory<byte>
   - JsonElement (special handling)
   - JsonDocument (special handling)

Types that **require JSON serialization** include:

- **User-defined structs** (e.g., `Nested`, `MyCustomStruct`)
- **User-defined classes** (e.g., `Result<T>`, `MyCustomClass`)
- **Collections** (e.g., `List<T>`, `Dictionary<TKey, TValue>`)
- **DateTime** and **DateTimeOffset**
- **TimeSpan** (unless using a custom converter)
- **Guid** (when not using string representation)
- **Decimal** (unless using a custom converter)
- Any other complex type

## Why This Matters

JSON serialization/deserialization:

1. **May cause precision loss** - For types like `DateTime`, the serialized value may lose precision during round-trip conversion
2. **May not preserve exact semantics** - Some types may not serialize/deserialize exactly as expected
3. **Performance overhead** - JSON serialization is slower than direct conversion
4. **Requires JSON-serializable types** - The type must be properly annotated or have appropriate JSON converters

## Example

```csharp
public class TestProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    
    // ⚠️ RH2001: Property 'LastRestocked' of type 'DateTime?' will be serialized/deserialized using JSON
    public DateTime? LastRestocked { get; set; }
    
    // ⚠️ RH2001: Property 'StockResult' of type 'Result<int>?' will be serialized/deserialized using JSON
    public Result<int>? StockResult { get; set; }
    
    // ⚠️ RH2001: Property 'RelatedProducts' of type 'List<string>?' will be serialized/deserialized using JSON
    public List<string>? RelatedProducts { get; set; }
    
    // ⚠️ RH2001: Property 'ProductCode' of type 'Nested?' will be serialized/deserialized using JSON
    public Nested? ProductCode { get; set; }
}
```

## How to Fix

### Option 1: Use a Custom Converter (Recommended)

Create a custom converter for precise control over serialization:

```csharp
public class DateTimeConverter : CacheValueConverter<DateTime>
{
    public override RedisValue GetBytes(DateTime value)
    {
        // Store as Unix timestamp (milliseconds) for precision
        return new DateTimeOffset(value).ToUnixTimeMilliseconds();
    }

    public override DateTime Parse(RedisValue value)
    {
        var milliseconds = (long)value;
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).DateTime;
    }
}

// Apply to property
public class TestProduct
{
    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime? LastRestocked { get; set; }
}
```

### Option 2: Accept JSON Serialization

If JSON serialization is acceptable for your use case:

1. **Ensure the type is JSON-serializable**
   - Add `[JsonSerializable]` attributes if using System.Text.Json source generators
   - Ensure the type has appropriate constructors and properties

2. **Test round-trip conversion**
   - Verify that values serialize and deserialize correctly
   - Check for precision loss or data corruption

3. **Suppress the warning** (if you're confident it works correctly)
   ```csharp
   #pragma warning disable RH2001
   public DateTime? LastRestocked { get; set; }
   #pragma warning restore RH2001
   ```

### Option 3: Use Redis-Native Types

Convert your data to types that Redis supports natively:

```csharp
// Instead of DateTime
public long LastRestockedTimestamp { get; set; } // Unix timestamp

// Instead of List<string>
public string RelatedProductsCsv { get; set; } // Comma-separated values

// Instead of Dictionary<int, string>
public string VariantMapJson { get; set; } // Pre-serialized JSON string
```

## Common Scenarios

### DateTime/DateTimeOffset
**Problem**: May lose precision or timezone information  
**Solution**: Use a custom converter that stores Unix timestamps

```csharp
public class DateTimeConverter : CacheValueConverter<DateTime>
{
    public override RedisValue GetBytes(DateTime value)
        => new DateTimeOffset(value).ToUnixTimeMilliseconds();

    public override DateTime Parse(RedisValue value)
        => DateTimeOffset.FromUnixTimeMilliseconds((long)value).DateTime;
}
```

### TimeSpan
**Problem**: JSON serialization may use string format  
**Solution**: Use a custom converter that stores ticks

```csharp
public class TimeSpanConverter : CacheValueConverter<TimeSpan>
{
    public override RedisValue GetBytes(TimeSpan value)
        => value.Ticks;

    public override TimeSpan Parse(RedisValue value)
        => TimeSpan.FromTicks((long)value);
}
```

### Decimal
**Problem**: RedisValue doesn't support decimal directly  
**Solution**: Use a custom converter that preserves precision

```csharp
public class DecimalConverter : CacheValueConverter<decimal>
{
    public override RedisValue GetBytes(decimal value)
        => value.ToString("G", CultureInfo.InvariantCulture);

    public override decimal Parse(RedisValue value)
        => decimal.Parse((string)value!, CultureInfo.InvariantCulture);
}
```

### Collections (List<T>, Dictionary<K,V>)
**Problem**: JSON serialization overhead  
**Solution**: 
- Accept JSON serialization if performance is acceptable
- Or convert to/from delimited strings for simple cases

### Custom Classes/Structs
**Problem**: Need full object serialization  
**Solution**: 
- Ensure type is properly JSON-serializable
- Or create a custom converter if special handling is needed

## When to Ignore This Warning

You can safely ignore this warning if:

1. You've tested round-trip conversion and it works correctly
2. The type is properly JSON-serializable
3. Performance is acceptable for your use case
4. You don't need precise control over the serialization format

## Related Diagnostics

- **RH1001**: Mismatched converter generic type
- **RH1002**: Context cannot be abstract
- **RH1003**: Context class must be top-level
- **RH1004**: Setter method required

## See Also

- [CacheConverterAttribute Documentation](../README.md#custom-converters)
- [Custom Converters Examples](../sample/Converters/)
- [System.Text.Json Documentation](https://docs.microsoft.com/en-us/dotnet/standard/serialization/system-text-json-overview)

