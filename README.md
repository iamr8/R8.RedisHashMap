# R8.RedisHashMap

R8.RedisHashMap is a high-performance Redis hash mapping library for .NET, designed to provide a simple and efficient
way to convert objects to and from Redis hash entries (`HashEntry[]`).

[![CI Build](https://github.com/iamr8/R8.RedisHashMap/actions/workflows/ci.yml/badge.svg)](https://github.com/iamr8/R8.RedisHashMap/actions/workflows/ci.yml)

## Features

- 🚀 **High Performance**: All implementations are generated at compile time using Source Generators, ensuring zero
  runtime overhead
- 🎯 **Type Safe**: Fully typed API with compile-time code generation
- 🔧 **Customizable**: Support for custom converters, naming strategies, and generation modes
- 📦 **Zero Allocation**: Optimized for minimal memory allocation during serialization/deserialization
- 🔄 **Bidirectional**: Convert objects to Redis hashes and vice versa
- 🎨 **Flexible Naming**: Support for PascalCase, camelCase, and snake_case field naming strategies

## How It Works

When working with Redis hashes, you often need to convert .NET objects into `HashEntry[]` arrays to store them in Redis.
This library uses C# Source Generators to automatically generate the serialization and deserialization code at compile
time, providing:

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

### Benchmark Results

Serializing and deserializing 10 000 `UserDto` objects (9 properties: 6 direct, 3 JSON — `enum[]`, `string[]`,
`Dictionary<string, string>`). The baseline is hand-written `new HashEntry[] { ... }` code with
`JsonSerializer.SerializeToUtf8Bytes`. Lower ratio is better.

```
BenchmarkDotNet v0.15.8, macOS Tahoe 26.5.2 (25F84) [Darwin 25.5.0]
Apple M2 Pro, 1 CPU, 12 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]    : .NET 10.0.10 (10.0.10, 10.0.1026.32716), Arm64 RyuJIT armv8.0-a

Server=True
```

#### Write — `GetHashEntries`

| Method                                          | Runtime   |     Mean | Ratio | Allocated | Alloc Ratio | Winner |
|-------------------------------------------------|-----------|---------:|------:|----------:|------------:|:------:|
| Array + JsonSerializerOptions *(baseline)*      | .NET 10.0 | 3.138 ms |  1.00 |   4.96 MB |        1.00 |        |
| Array + JsonSerializerContext                   | .NET 10.0 | 2.803 ms |  0.89 |   4.96 MB |        1.00 |        |
| **Source Generator**                            | .NET 10.0 | 1.191 ms |  0.38 |   4.43 MB |        0.89 | 🏆     |
| **Source Generator + JsonSerializerOptions**    | .NET 10.0 | 1.187 ms |  0.38 |   4.43 MB |        0.89 | 🏆     |
| **Source Generator + JsonSerializerContext**    | .NET 10.0 | 1.182 ms |  0.38 |   4.43 MB |        0.89 | 🏆     |
| Array + JsonSerializerOptions *(baseline)*      | .NET 8.0  | 3.733 ms |  1.00 |   4.96 MB |        1.00 |        |
| Array + JsonSerializerContext                   | .NET 8.0  | 3.386 ms |  0.91 |   4.96 MB |        1.00 |        |
| **Source Generator**                            | .NET 8.0  | 1.444 ms |  0.39 |   4.43 MB |        0.89 | 🏆     |
| **Source Generator + JsonSerializerOptions**    | .NET 8.0  | 1.466 ms |  0.39 |   4.43 MB |        0.89 | 🏆     |
| **Source Generator + JsonSerializerContext**    | .NET 8.0  | 1.462 ms |  0.39 |   4.43 MB |        0.89 | 🏆     |
| Array + JsonSerializerOptions *(baseline)*      | .NET 6.0  | 6.391 ms |  1.00 |   4.96 MB |        1.00 |        |
| Array + JsonSerializerContext                   | .NET 6.0  | 5.549 ms |  0.87 |   4.96 MB |        1.00 |        |
| **Source Generator**                            | .NET 6.0  | 2.605 ms |  0.41 |   4.43 MB |        0.89 | 🏆     |
| **Source Generator + JsonSerializerOptions**    | .NET 6.0  | 2.615 ms |  0.41 |   4.43 MB |        0.89 | 🏆     |
| **Source Generator + JsonSerializerContext**    | .NET 6.0  | 2.672 ms |  0.42 |   4.43 MB |        0.89 |        |

🏆 marks every row that is fastest within measurement error; the three Source Generator variants are
statistically tied on each runtime.

**2.6× faster than hand-written array code on .NET 10**, with 11 % fewer allocations — and byte-for-byte identical
output. Well-known value shapes (`string[]`, `List<string>`, enum and integral sequences,
`Dictionary<string, string>`) are emitted by hand-written, SIMD-accelerated UTF-8 writers that bypass
`Utf8JsonWriter` entirely. Anything they cannot reproduce exactly — a custom encoder, a naming policy, non-ASCII
text — transparently falls back to `System.Text.Json`.

#### Read — `FromHashEntries`

| Method                                          | Runtime   |      Mean | Ratio | Allocated | Winner |
|-------------------------------------------------|-----------|----------:|------:|----------:|:------:|
| Array + JsonSerializerOptions *(baseline)*      | .NET 10.0 |  5.716 ms |  1.00 |  10.07 MB |        |
| **Array + JsonSerializerContext**               | .NET 10.0 |  5.298 ms |  0.93 |  10.07 MB | 🏆     |
| Source Generator                                | .NET 10.0 |  5.641 ms |  0.99 |  10.07 MB |        |
| **Source Generator + JsonSerializerOptions**    | .NET 10.0 |  5.336 ms |  0.93 |  10.07 MB | 🏆     |
| Source Generator + JsonSerializerContext        | .NET 10.0 |  6.739 ms |  1.18 |  10.07 MB |        |
| Array + JsonSerializerOptions *(baseline)*      | .NET 6.0  | 11.313 ms |  1.00 |  10.07 MB |        |
| Array + JsonSerializerContext                   | .NET 6.0  |  9.903 ms |  0.88 |  10.07 MB | 🏆     |
| Source Generator                                | .NET 6.0  | 10.614 ms |  0.94 |  10.07 MB |        |
| Source Generator + JsonSerializerOptions        | .NET 6.0  | 10.489 ms |  0.93 |  10.07 MB |        |
| Source Generator + JsonSerializerContext        | .NET 6.0  | 10.332 ms |  0.91 |  10.07 MB |        |

The read path is at parity with hand-written code: `JsonSerializer.Deserialize` dominates it, and both approaches
call the same deserializer. The generator's benefit here is that you do not write or maintain the mapping code.

Full results, including .NET 8 read numbers, are in [`benchmark/Benchmark_3.txt`](benchmark/Benchmark_3.txt).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
