# RH2001 Diagnostic Implementation Summary

## What Was Done

Added a new source generator diagnostic (RH2001) that warns developers when a property requires JSON serialization/deserialization because it's not natively supported by Redis and doesn't have a custom converter.

## Changes Made

### 1. **SourceGenerator.Diagnostics.cs**
Added new diagnostic descriptor:
```csharp
public static readonly DiagnosticDescriptor PropertyRequiresJsonSerialization = new DiagnosticDescriptor(
    id: "RH2001",
    title: "Property requires JSON serialization",
    messageFormat: "Property '{0}' of type '{1}' will be serialized/deserialized using JSON...",
    ...
);
```

### 2. **TypeSymbol.cs**
Added property to identify types that require JSON serialization:
```csharp
public bool RequiresJsonSerialization =>
    !_hasConverter &&        // No custom converter
    !_isPrimitiveType &&     // Not a primitive type
    !_isEnum &&              // Not an enum
    !_isReadOnlyMemory &&    // Not ReadOnlyMemory<byte>
    !_isJsonElement &&       // Not JsonElement
    !_isJsonDocument &&      // Not JsonDocument
    !_isString &&            // Not string
    !_isBytesArray &&        // Not byte[]
    (_isValueType || _isReferenceType); // User-defined struct or class
```

### 3. **SourceGenerator.Parser.cs**
Added diagnostic reporting in `GetObjectOptions`:
```csharp
if (item.RequiresJsonSerialization)
{
    ctx.ReportDiagnostic(Diagnostic.Create(
        SourceGeneratorDiagnostics.PropertyRequiresJsonSerialization,
        symbol.Locations.FirstOrDefault(),
        symbol.Name,
        item.Type.ToDisplayString()));
}
```

### 4. **DateTimeConverter.cs** (New)
Created a converter for DateTime that stores values as Unix timestamps to avoid precision loss:
```csharp
public class DateTimeConverter : CacheValueConverter<DateTime>
{
    public override RedisValue GetBytes(DateTime value)
        => new DateTimeOffset(utcValue).ToUnixTimeMilliseconds();

    public override DateTime Parse(RedisValue value)
        => DateTimeOffset.FromUnixTimeMilliseconds((long)value).UtcDateTime;
}
```

### 5. **Test Models Updated**
Applied `[CacheConverter(typeof(DateTimeConverter))]` to all DateTime properties in:
- `TestUser.cs` - CreatedAt, LastLoginAt
- `TestProduct.cs` - LastRestocked
- `TestSession.cs` - StartTime, EndTime
- `AdvancedTestModel.cs` - RegistrationDate, LastLoginDate

## Types That Trigger RH2001

### User-Defined Value Types (Structs)
- ✅ `Nested` - Custom struct
- ✅ Any custom struct without converter

### User-Defined Reference Types (Classes)
- ✅ `Result<T>` - Generic class
- ✅ Any custom class without converter

### Collections
- ✅ `List<T>`
- ✅ `Dictionary<TKey, TValue>`
- ✅ `IEnumerable<T>` (that's not byte[])

### Date/Time Types (without converter)
- ✅ `DateTime`
- ✅ `DateTimeOffset`
- ✅ `TimeSpan`

### Other Types (without converter)
- ✅ `Guid` (when stored as object, not string)
- ✅ `Decimal` (RedisValue doesn't support it directly)

## Types That DON'T Trigger RH2001

### Primitive Types
- ❌ `int`, `long`, `short`, `byte`, etc.
- ❌ `bool`
- ❌ `double`, `float`
- ❌ `char`

### Special Types
- ❌ `string`
- ❌ `byte[]`
- ❌ `ReadOnlyMemory<byte>`
- ❌ `JsonDocument` (special handling)
- ❌ `JsonElement` (special handling)

### Enums
- ❌ Any enum type

### Types with Custom Converters
- ❌ Properties with `[CacheConverter]` attribute

## Benefits

1. **Awareness**: Developers know which properties use JSON serialization
2. **Performance**: Encourages using custom converters for better performance
3. **Reliability**: Highlights potential issues with DateTime precision loss
4. **Best Practices**: Guides developers to use appropriate converters

## Example Warning Output

```
Warning RH2001: Property 'LastRestocked' of type 'DateTime?' will be serialized/deserialized using JSON. 
Consider using a custom converter with [CacheConverter] attribute for better performance and reliability, 
or ensure the type is JSON-serializable
```

## How Developers Should Respond

### For DateTime/DateTimeOffset
Use `DateTimeConverter` to avoid precision loss:
```csharp
[CacheConverter(typeof(DateTimeConverter))]
public DateTime CreatedAt { get; set; }
```

### For TimeSpan
Use `TimeSpanConverter`:
```csharp
[CacheConverter(typeof(TimeSpanConverter))]
public TimeSpan Duration { get; set; }
```

### For Decimal
Use `DecimalConverter`:
```csharp
[CacheConverter(typeof(DecimalConverter))]
public decimal Price { get; set; }
```

### For Collections/Custom Types
Either:
1. Accept JSON serialization if acceptable
2. Create a custom converter if special handling is needed
3. Suppress the warning if you've tested and verified it works

## Documentation

Created comprehensive documentation in:
- `/docs/RH2001-PropertyRequiresJsonSerialization.md`

## Testing

All existing tests should continue to pass. The warnings will appear during build, guiding developers to add appropriate converters.

## Future Enhancements

Possible improvements:
1. Add quick-fix code actions to automatically add converter attributes
2. Suggest specific converters based on the type
3. Add configuration to suppress warnings for specific types globally

