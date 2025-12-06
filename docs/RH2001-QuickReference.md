# Quick Reference: Handling RH2001 Warnings

## What is RH2001?

**RH2001** is a source generator warning that alerts you when a property will be serialized/deserialized using JSON because it's not natively supported by Redis.

## Quick Fixes

### DateTime / DateTimeOffset

❌ **Warning:**
```csharp
public DateTime CreatedAt { get; set; }  // RH2001 Warning
```

✅ **Solution:**
```csharp
[CacheConverter(typeof(DateTimeConverter))]
public DateTime CreatedAt { get; set; }
```

**Converter:**
```csharp
public class DateTimeConverter : CacheValueConverter<DateTime>
{
    public override RedisValue GetBytes(DateTime value)
    {
        var utcValue = value.Kind == DateTimeKind.Unspecified 
            ? DateTime.SpecifyKind(value, DateTimeKind.Utc) 
            : value.ToUniversalTime();
        return new DateTimeOffset(utcValue).ToUnixTimeMilliseconds();
    }

    public override DateTime Parse(RedisValue value)
    {
        var milliseconds = (long)value;
        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
    }
}
```

### TimeSpan

❌ **Warning:**
```csharp
public TimeSpan Duration { get; set; }  // RH2001 Warning
```

✅ **Solution:**
```csharp
[CacheConverter(typeof(TimeSpanConverter))]
public TimeSpan Duration { get; set; }
```

**Converter:**
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

❌ **Warning:**
```csharp
public decimal Price { get; set; }  // RH2001 Warning
```

✅ **Solution:**
```csharp
[CacheConverter(typeof(DecimalConverter))]
public decimal Price { get; set; }
```

**Converter:**
```csharp
public class DecimalConverter : CacheValueConverter<decimal>
{
    public override RedisValue GetBytes(decimal value)
        => value.ToString("G", CultureInfo.InvariantCulture);

    public override decimal Parse(RedisValue value)
        => decimal.Parse((string)value!, CultureInfo.InvariantCulture);
}
```

### Collections (List, Dictionary)

⚠️ **Warning:**
```csharp
public List<string> Tags { get; set; }  // RH2001 Warning
public Dictionary<int, string> Variants { get; set; }  // RH2001 Warning
```

✅ **Options:**

**Option 1: Accept JSON serialization** (if performance is OK)
```csharp
#pragma warning disable RH2001
public List<string> Tags { get; set; }
#pragma warning restore RH2001
```

**Option 2: Use simple types**
```csharp
public string TagsCsv { get; set; }  // Store as comma-separated values
```

**Option 3: Create custom converter**
```csharp
[CacheConverter(typeof(StringListConverter))]
public List<string> Tags { get; set; }
```

### Custom Classes/Structs

⚠️ **Warning:**
```csharp
public Result<int> StockResult { get; set; }  // RH2001 Warning
public Nested ProductCode { get; set; }  // RH2001 Warning
```

✅ **Options:**

**Option 1: Accept JSON serialization** (ensure type is JSON-serializable)
```csharp
// Make sure Result<T> and Nested are JSON-serializable
#pragma warning disable RH2001
public Result<int> StockResult { get; set; }
#pragma warning restore RH2001
```

**Option 2: Flatten the structure**
```csharp
// Instead of Result<int>
public int StockValue { get; set; }

// Instead of Nested
public long ProductCodeId { get; set; }
public string ProductCodeName { get; set; }
```

**Option 3: Create custom converter**
```csharp
[CacheConverter(typeof(ResultIntConverter))]
public Result<int> StockResult { get; set; }
```

## When to Suppress the Warning

It's OK to suppress RH2001 if:
1. ✅ You've tested round-trip conversion and it works
2. ✅ The type is properly JSON-serializable
3. ✅ Performance is acceptable for your use case
4. ✅ You don't need precise control over serialization

## Suppression Examples

### Single Property
```csharp
#pragma warning disable RH2001
public List<string> Tags { get; set; }
#pragma warning restore RH2001
```

### Entire Class
```csharp
#pragma warning disable RH2001
public class MyModel
{
    public DateTime CreatedAt { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<int, string> Data { get; set; }
}
#pragma warning restore RH2001
```

### Entire File
```csharp
// At top of file
#pragma warning disable RH2001

namespace MyApp.Models;

public class MyModel
{
    // ... properties ...
}
```

## Custom Converter Template

```csharp
using StackExchange.Redis;

namespace MyApp.Converters;

public class MyTypeConverter : CacheValueConverter<MyType>
{
    public override RedisValue GetBytes(MyType value)
    {
        // Convert to RedisValue
        // Options:
        // - Store as string
        // - Store as bytes
        // - Store as number
        // - Serialize to JSON manually
        
        return value.ToString();
    }

    public override MyType Parse(RedisValue value)
    {
        // Parse from RedisValue
        var str = (string)value!;
        return MyType.Parse(str);
    }
}
```

## Common Patterns

### Store Enums as Strings
```csharp
public class EnumConverter<TEnum> : CacheValueConverter<TEnum>
    where TEnum : struct, Enum
{
    public override RedisValue GetBytes(TEnum value)
        => value.ToString();

    public override TEnum Parse(RedisValue value)
        => Enum.Parse<TEnum>((string)value!);
}
```

### Store Complex Objects as JSON
```csharp
public class JsonConverter<T> : CacheValueConverter<T>
{
    public override RedisValue GetBytes(T value)
        => JsonSerializer.Serialize(value);

    public override T Parse(RedisValue value)
        => JsonSerializer.Deserialize<T>((string)value!)!;
}
```

## Need Help?

- 📖 Full documentation: `/docs/RH2001-PropertyRequiresJsonSerialization.md`
- 💡 Examples: `/test/R8.RedisHashMap.Tests/Converters/`
- 🔍 Implementation details: `/docs/RH2001-Implementation-Summary.md`

