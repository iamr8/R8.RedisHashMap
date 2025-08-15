using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using R8.RedisHashMap.Test.Map;
using R8.RedisHashMap.Test.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test;

public class Class1
{
    public static void Main(string[] args)
    {
        // BenchmarkRunner.Run<WriteBenchmark>();
        BenchmarkRunner.Run<ReadBenchmark>();
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net80)]
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
                new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Roles, Objects.UserDtoJsonSerializer.Default.Options)),
                new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Tags, Objects.UserDtoJsonSerializer.Default.Options)),
                new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Data, Objects.UserDtoJsonSerializer.Default.Options))
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
                new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Roles, Objects.UserDtoJsonSerializer.Default.UserRoleTypeArray)),
                new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Tags, Objects.UserDtoJsonSerializer.Default.StringArray)),
                new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Data, Objects.UserDtoJsonSerializer.Default.DictionaryStringString))
            };
    }

    [Benchmark(Description = "Write: Source Generator")]
    public void Write_SourceGen0()
    {
        foreach (var model in models) _ = model.GetHashEntries();
    }

    [Benchmark(Description = "Write: Source Generator + JsonSerializerOptions")]
    public void Write_SourceGen1()
    {
        foreach (var model in models) _ = model.GetHashEntries(Objects.UserDtoJsonSerializer.Default.Options);
    }

    [Benchmark(Description = "Write: Source Generator + JsonSerializerContext")]
    public void Write_SourceGen2()
    {
        foreach (var model in models) _ = model.GetHashEntries(Objects.UserDtoJsonSerializer.Default);
    }
    //
    // [Benchmark(Description = "Write: Reflection + JsonSerializerOptions")]
    // public void Write_Reflection()
    // {
    //     foreach (var model in models) _ = MapCache.GetHashEntries(model, Objects.UserDtoJsonSerializer.Default.Options);
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
            new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(new[] { UserRoleType.Admin, UserRoleType.User }, Objects.UserDtoJsonSerializer.Default.UserRoleTypeArray)),
            new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(new[] { "super-admin", "moderator", "super-user", "developer" }, Objects.UserDtoJsonSerializer.Default.StringArray)),
            new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, string>
            {
                ["nationality"] = "Iranian",
                ["countryOfResidence"] = "Turkey",
                ["age"] = "34"
            }, Objects.UserDtoJsonSerializer.Default.DictionaryStringString))
        };
    }
    
    [Benchmark(Baseline = true, Description = "Read: Source Generator")]
    public void Read_SourceGen0()
    {
        for (var i = 0; i < N; i++) _ = Objects.UserDto.FromHashEntries(hashEntries);
    }
    
    [Benchmark(Description = "Read: Source Generator + JsonSerializerOptions")]
    public void Read_SourceGen1()
    {
        for (var i = 0; i < N; i++) _ = Objects.UserDto.FromHashEntries(hashEntries, Objects.UserDtoJsonSerializer.Default.Options);
    }
    
    [Benchmark(Description = "Read: Source Generator + JsonSerializerContext")]
    public void Read_SourceGen2()
    {
        for (var i = 0; i < N; i++) _ = Objects.UserDto.FromHashEntries(hashEntries, Objects.UserDtoJsonSerializer.Default);
    }
    
    [Benchmark(Description = "Read: Reflection + JsonSerializerOptions")]
    public void Read_Reflection()
    {
        for (var i = 0; i < N; i++) _ = hashEntries.TryDeserialize<Objects.UserDto>(Objects.UserDtoJsonSerializer.Default.Options, out _);
    }
}