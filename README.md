# R8.RedisHashMap

R8.RedisHashMap is a Redis-backed HashMap implementation for .NET, designed to provide a simple and efficient way to
generate a `HashEntry[]` from an object.

Sometimes, you may need to use all hash fields of a Redis hash for a specific object type. This library allows you to
easily convert an object into a `HashEntry[]`, which can then be used with Redis commands.

This package also allows you to implement Converters for custom types, enabling you to control how objects are converted
to `RedisValue` and back.

All implementations are done using Source Generators, which means that the code is generated at compile time, ensuring
high performance and low overhead.

## Installation

You can install the R8.RedisHashMap package via NuGet:

```bash
dotnet add package R8.RedisHashMap
```

## Usage

Here's a simple example of how to use R8.RedisHashMap:

```csharp
using R8.RedisHashMap;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
}

[CacheContext]
[CacheObject(typeof(Person))]
public partial class MapperContext
{
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var redis = ConnectionMultiplexer.Connect("localhost");
        var db = redis.GetDatabase();

        var person = new Person { Name = "Alice", Age = 30 };
        var hashEntries = MapperContext.Default.Person.GetHashEntries(person);

        // Set the object in Redis
        var redisKey = new RedisKey("person:1");
        db.HashSet(redisKey, hashEntries);

        // Get the object back from Redis
        var retrievedHashEntries = db.HashGetAll(redisKey);
        var retrievedPerson = MapperContext.Default.Person.FromHashEntries(retrievedHashEntries);

        Console.WriteLine($"Name: {retrievedPerson.Name}, Age: {retrievedPerson.Age}");
    }
}
```