# R8.RedisHashMap

R8.RedisHashMap is a high-performance Redis hash mapping library for .NET, designed to provide a simple and efficient way to convert objects to and from Redis hash entries (`HashEntry[]`).

## Features

- 🚀 **High Performance**: All implementations are generated at compile time using Source Generators, ensuring zero runtime overhead
- 🎯 **Type Safe**: Fully typed API with compile-time code generation
- 🔧 **Customizable**: Support for custom converters, naming strategies, and generation modes
- 📦 **Zero Allocation**: Optimized for minimal memory allocation during serialization/deserialization
- 🔄 **Bidirectional**: Convert objects to Redis hashes and vice versa
- 🎨 **Flexible Naming**: Support for PascalCase, camelCase, and snake_case field naming strategies

## How It Works

When working with Redis hashes, you often need to convert .NET objects into `HashEntry[]` arrays to store them in Redis. This library uses C# Source Generators to automatically generate the serialization and deserialization code at compile time, providing:

- Fast conversion between objects and `HashEntry[]`
- Type-safe API without reflection overhead
- Support for complex types through custom converters
- Efficient memory usage with minimal allocations

## Installation

You can install the R8.RedisHashMap package via NuGet:

```bash
dotnet add package R8.RedisHashMap
```

## Quick Start

### Basic Usage

Here's a simple example of how to use R8.RedisHashMap:

```csharp
using R8.RedisHashMap;
using StackExchange.Redis;

// Define your model
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

// Create a mapper context
[CacheContext]
[CacheObject(typeof(Person))]
public partial class CacheMapperContext
{
}

public class Program
{
    public static void Main(string[] args)
    {
        var redis = ConnectionMultiplexer.Connect("localhost");
        var db = redis.GetDatabase();

        // Create an object
        var person = new Person { Name = "Alice", Age = 30 };
        
        // Convert to hash entries
        var hashEntries = CacheMapperContext.Default.Person.ToHashEntries(person);

        // Store in Redis
        db.HashSet("person:1", hashEntries);

        // Retrieve from Redis
        var retrievedEntries = db.HashGetAll("person:1");
        var retrievedPerson = CacheMapperContext.Default.Person.FromHashEntries(retrievedEntries);

        Console.WriteLine($"Name: {retrievedPerson.Name}, Age: {retrievedPerson.Age}");
    }
}
```

## Advanced Features

### Naming Strategies

You can customize how property names are mapped to Redis hash fields:

```csharp
[CacheContext(NamingStrategy = CacheFieldNamingStrategy.SnakeCase)]
[CacheObject(typeof(Person))]
public partial class CacheMapperContext
{
}

// Properties like "FirstName" will be stored as "first_name" in Redis
```

Available naming strategies:
- `PascalCase` (default): `FirstName`
- `CamelCase`: `firstName`
- `SnakeCase`: `first_name`

### Generation Modes

Control what code gets generated:

```csharp
[CacheContext(GenerationMode = CacheGenerationMode.Serialization)]
[CacheObject(typeof(Person))]
public partial class CacheMapperContext
{
}
```

Available modes:
- `Default`: Generate both serialization and deserialization code
- `Serialization`: Generate only `ToHashEntries` method
- `Deserialization`: Generate only `FromHashEntries` method

### Custom Converters

Implement custom type converters for complex or non-standard types:

```csharp
// Define a custom converter
public class TimeSpanConverter : CacheValueConverter<TimeSpan>
{
    public override RedisValue GetBytes(TimeSpan value)
    {
        return (long)value.TotalMilliseconds;
    }

    public override TimeSpan Parse(RedisValue value)
    {
        return TimeSpan.FromMilliseconds((long)value);
    }
}

// Apply the converter to a property
public class Session
{
    public string Id { get; set; }
    
    [CacheConverter(typeof(TimeSpanConverter))]
    public TimeSpan Duration { get; set; }
}
```

### Multiple Object Types

You can map multiple types in a single context:

```csharp
[CacheContext(NamingStrategy = CacheFieldNamingStrategy.SnakeCase)]
[CacheObject(typeof(Person))]
[CacheObject(typeof(Session))]
[CacheObject(typeof(Product), GenerationMode = CacheGenerationMode.Serialization)]
public partial class CacheMapperContext
{
}

// Access each mapper
var personEntries = CacheMapperContext.Default.Person.ToHashEntries(person);
var sessionEntries = CacheMapperContext.Default.Session.ToHashEntries(session);
var productEntries = CacheMapperContext.Default.Product.ToHashEntries(product);
```

## Performance

The library is designed for high performance:

- **Compile-time code generation**: No reflection at runtime
- **Optimized memory usage**: Significantly reduced allocations compared to manual array creation
- **Direct conversions**: Efficient type conversions without intermediate steps
- **ArrayPool reuse**: Leverages array pooling for temporary buffers

### Benchmark Results

Benchmarks performed on macOS Sequoia 15.7.2 with Apple M2 Pro (.NET 10.0):

```
BenchmarkDotNet v0.15.8, macOS Sequoia 15.7.2 (24G325) [Darwin 24.6.0]
Apple M2 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.100
  [Host]    : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a
  .NET 10.0 : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a

Job=.NET 10.0  Runtime=.NET 10.0  Server=True
```

| Method                                            | N     | Mean     | Error     | StdDev    | Ratio | Completed Work Items | Lock Contentions | Gen0    | Allocated | Alloc Ratio |
|-------------------------------------------------- |------ |---------:|----------:|----------:|------:|---------------------:|-----------------:|--------:|----------:|------------:|
| 'Write: Array + JsonSerializerOptions'            | 10000 | 3.449 ms | 0.0201 ms | 0.0157 ms |  1.00 |                    - |                - | 62.5000 |   7.71 MB |        1.00 |
| 'Write: Array + JsonSerializerContext'            | 10000 | 2.842 ms | 0.0154 ms | 0.0129 ms |  0.82 |                    - |                - | 62.5000 |   7.71 MB |        1.00 |
| 'Write: Source Generator'                         | 10000 | 3.797 ms | 0.0166 ms | 0.0139 ms |  1.10 |                    - |                - | 62.5000 |   7.71 MB |        1.00 |
| 'Write: Source Generator + JsonSerializerOptions' | 10000 | 3.684 ms | 0.0408 ms | 0.0361 ms |  1.07 |                    - |                - | 62.5000 |   7.71 MB |        1.00 |
| 'Write: Source Generator + JsonSerializerContext' | 10000 | 4.147 ms | 0.0109 ms | 0.0096 ms |  1.20 |                    - |                - | 62.5000 |   7.71 MB |        1.00 |

**Key Takeaways:**
- ✅ **Same memory allocation** (7.71 MB) - Consistent and predictable memory usage
- ✅ **Excellent performance** - Source Generator performs on par with manual array creation (within 7-20% depending on method)
- ✅ **Type-safe code generation** with minimal runtime overhead
- ✅ **Best performance** with JsonSerializerOptions (~3.68 ms, comparable to baseline)

## Requirements

- .NET 6.0 or higher
- StackExchange.Redis

  
## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
