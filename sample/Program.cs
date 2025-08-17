using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using R8.RedisHashMap.Test.Map;
using R8.RedisHashMap.Test.Models;
using R8.RedisHashMap.Test.Objects;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

public class Class1
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    private static readonly ConcurrentDictionary<Type, SerializerContext> _serializerContexts = new();

    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<WriteBenchmark>();
        // BenchmarkRunner.Run<ReadBenchmark>();

        // var redis = ConnectionMultiplexer.Connect("localhost");
        // var db = redis.GetDatabase();
        //
        // var person = new Person { Name = "Alice", Age = 30 };
        // var hashEntries = person.GetHashEntries();
        //
        // // // Set the object in Redis
        // db.HashSet("person:1", hashEntries);

        // Get the object back from Redis
        // var retrievedPerson = await hashMap.GetAsync();
        // Console.WriteLine($"Name: {retrievedPerson.Name}, Age: {retrievedPerson.Age}");
    }
}

internal class SerializerContext
{
}

// [SimpleJob(RuntimeMoniker.Net60)]
// [SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[GcServer(true)]
public class WriteBenchmark
{
    private ConnectionMultiplexer connectionMultiplexer;
    private Objects.UserDto[] models;

    [Params(10_000)] public int N;

    [GlobalSetup]
    public void Setup()
    {
        models = Enumerable.Range(0, N)
            .Select(x => new Objects.UserDto
            {
                Id = x,
                FirstName = $"Arash {x}",
                LastName = $"Shabbeh {x}",
                Email = "arash.shabbeh@gmail.com",
                Mobile = $"09123{x:00000}",
                Age = 34,
                Roles = new[] { UserRoleType.Admin, UserRoleType.User },
                Tags = new[] { "super-admin", "moderator", "super-user", "developer" },
                Data = new Dictionary<string, string>
                {
                    ["nationality"] = "Iranian",
                    ["countryOfResidence"] = "Turkey",
                    ["age"] = "34"
                }
            }).ToArray();
    }

    [Benchmark(Baseline = true, Description = "Write: Array + JsonSerializerOptions")]
    public void Write_Array1()
    {
        foreach (var model in models)
            _ = new[]
            {
                new HashEntry("Id", model.Id),
                new HashEntry("FirstName", (RedisValue)model.FirstName),
                new HashEntry("LastName", (RedisValue)model.LastName),
                new HashEntry("Email", (RedisValue)model.Email),
                new HashEntry("Mobile", (RedisValue)model.Mobile),
                new HashEntry("Age", model.Age),
                new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Roles, UserDtoSerializerContext.Default.Options)),
                new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Tags, UserDtoSerializerContext.Default.Options)),
                new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Data, UserDtoSerializerContext.Default.Options))
            };
    }

    [Benchmark(Description = "Write: Array + JsonSerializerContext")]
    public void Write_Array2()
    {
        foreach (var model in models)
            _ = new[]
            {
                new HashEntry("Id", model.Id),
                new HashEntry("FirstName", (RedisValue)model.FirstName),
                new HashEntry("LastName", (RedisValue)model.LastName),
                new HashEntry("Email", (RedisValue)model.Email),
                new HashEntry("Mobile", (RedisValue)model.Mobile),
                new HashEntry("Age", model.Age),
                new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Roles, UserDtoSerializerContext.Default.UserRoleTypeArray)),
                new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Tags, UserDtoSerializerContext.Default.StringArray)),
                new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Data, UserDtoSerializerContext.Default.DictionaryStringString))
            };
    }

    [Benchmark(Description = "Write: Source Generator")]
    public void Write_SourceGen0()
    {
        foreach (var model in models) _ = UserDtoMapperContext.Default.UserDto.GetHashEntries(model);
    }

    [Benchmark(Description = "Write: Source Generator + JsonSerializerOptions")]
    public void Write_SourceGen1()
    {
        foreach (var model in models) _ = UserDtoMapperContext.Default.UserDto.GetHashEntries(model, UserDtoSerializerContext.Default.Options);
    }

    [Benchmark(Description = "Write: Source Generator + JsonSerializerContext")]
    public void Write_SourceGen2()
    {
        foreach (var model in models) _ = UserDtoMapperContext.Default.UserDto.GetHashEntries(model, UserDtoSerializerContext.Default);
    }

    // [Benchmark(Description = "Write: Reflection + JsonSerializerOptions")]
    // public void Write_Reflection()
    // {
    //     foreach (var model in models) _ = MapCache.GetHashEntries(model, UserDtoSerializerContext.Default.Options);
    // }
}

[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[GcServer(true)]
public class ReadBenchmark
{
    private HashEntry[] hashEntries;
    [Params(10_000)] public int N;

    [GlobalSetup]
    public void Setup()
    {
        hashEntries = new[]
        {
            new HashEntry("Id", 1),
            new HashEntry("FirstName", (RedisValue)"Arash"),
            new HashEntry("LastName", (RedisValue)"Shabbeh"),
            new HashEntry("Email", (RedisValue)"arash.shabbeh@gmail.com"),
            new HashEntry("Mobile", (RedisValue)"09123456789"),
            new HashEntry("Age", 34),
            new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(new[] { UserRoleType.Admin, UserRoleType.User }, UserDtoSerializerContext.Default.UserRoleTypeArray)),
            new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(new[] { "super-admin", "moderator", "super-user", "developer" }, UserDtoSerializerContext.Default.StringArray)),
            new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>
            {
                ["nationality"] = "Iranian",
                ["countryOfResidence"] = "Turkey",
                ["age"] = "34"
            }, UserDtoSerializerContext.Default.DictionaryStringString))
        };
    }

    [Benchmark(Baseline = true, Description = "Read: Source Generator")]
    public void Read_SourceGen0()
    {
        for (var i = 0; i < N; i++) _ = UserDtoMapperContext.Default.UserDto.FromHashEntries(hashEntries);
    }

    [Benchmark(Description = "Read: Source Generator + JsonSerializerOptions")]
    public void Read_SourceGen1()
    {
        // var ff = UserDtoRedisMapper.UserDto.GetHashEntries(hashEntries, UserDtoSerializerContext.Default.Options);
        for (var i = 0; i < N; i++) _ = UserDtoMapperContext.Default.UserDto.FromHashEntries(hashEntries, UserDtoSerializerContext.Default.Options);
    }

    [Benchmark(Description = "Read: Source Generator + JsonSerializerContext")]
    public void Read_SourceGen2()
    {
        for (var i = 0; i < N; i++) _ = UserDtoMapperContext.Default.UserDto.FromHashEntries(hashEntries, UserDtoSerializerContext.Default);
    }

    // [Benchmark(Description = "Read: Reflection + JsonSerializerOptions")]
    // public void Read_Reflection()
    // {
    //     for (var i = 0; i < N; i++) _ = hashEntries.TryDeserialize<Objects.UserDto>(UserDtoSerializerContext.Default.Options, out _);
    // }
}