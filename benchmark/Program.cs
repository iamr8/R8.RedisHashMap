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

[CacheContext]
[CacheObject(typeof(Class1.Person))]
public partial class MapperContext
{
}

public class Class1
{
    public class Person
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == "--verify")
        {
            Verify();
            return;
        }

        BenchmarkSwitcher.FromTypes(new[] { typeof(WriteBenchmark), typeof(ReadBenchmark) }).Run(args);
    }

    /// <summary>
    /// Asserts that every GetHashEntries overload produces byte-identical output to hand-written
    /// JsonSerializer.SerializeToUtf8Bytes calls, including payloads that force the fast writers to bail.
    /// </summary>
#pragma warning disable RS1035 // this is a console app, not an analyzer
    private static void Verify()
    {
        var models = new[]
        {
            new Objects.UserDto
            {
                Id = 7, FirstName = "Arash", LastName = "Shabbeh", Email = "arash.shabbeh@gmail.com", Mobile = "09123456789", Age = 34,
                Roles = new[] { UserRoleType.Admin, UserRoleType.User },
                Tags = new[] { "super-admin", "moderator", "super-user", "developer" },
                Data = new Dictionary<string, string> { ["nationality"] = "Iranian", ["countryOfResidence"] = "Turkey", ["age"] = "34" }
            },
            // escaping + non-ASCII: forces the hand-written writers to bail into System.Text.Json
            new Objects.UserDto
            {
                Id = -1, FirstName = "Ærîal", LastName = "O'Brien <b>", Email = null, Mobile = "",
                Age = int.MinValue,
                Roles = Array.Empty<UserRoleType>(),
                Tags = new[] { "a\"b", "c\\d", "é", "tab\there", null! },
                Data = new Dictionary<string, string> { ["<key>"] = "va&lue", ["ünïcode"] = "\n\r\t", ["ok"] = null! }
            }
        };

        var failures = 0;
        foreach (var model in models)
        foreach (var (label, actual) in new (string, HashEntry[])[]
                 {
                     ("no args", UserDtoMapperContext.Default.UserDto.GetHashEntries(model)),
                     ("options", UserDtoMapperContext.Default.UserDto.GetHashEntries(model, UserDtoSerializerContext.Default.Options)),
                     ("context", UserDtoMapperContext.Default.UserDto.GetHashEntries(model, UserDtoSerializerContext.Default))
                 })
        {
            var expected = new List<HashEntry> { new("id", model.Id) };
            if (model.FirstName is { Length: > 0 }) expected.Add(new HashEntry("first_name", model.FirstName));
            if (model.LastName is { Length: > 0 }) expected.Add(new HashEntry("last_name", model.LastName));
            if (model.Email is { Length: > 0 }) expected.Add(new HashEntry("email", model.Email));
            if (model.Mobile is { Length: > 0 }) expected.Add(new HashEntry("mobile", model.Mobile));
            expected.Add(new HashEntry("age", model.Age));
            if (model.Roles is { Length: > 0 }) expected.Add(new HashEntry("roles", JsonSerializer.SerializeToUtf8Bytes(model.Roles)));
            if (model.Tags is { Length: > 0 }) expected.Add(new HashEntry("tags", JsonSerializer.SerializeToUtf8Bytes(model.Tags)));
            if (model.Data is { Count: > 0 }) expected.Add(new HashEntry("data", JsonSerializer.SerializeToUtf8Bytes(model.Data)));

            if (actual.Length != expected.Count)
            {
                Console.WriteLine($"FAIL [{label}] entry count {actual.Length} != {expected.Count}");
                failures++;
                continue;
            }

            for (var i = 0; i < actual.Length; i++)
            {
                if (actual[i].Name == expected[i].Name && actual[i].Value == expected[i].Value)
                    continue;

                Console.WriteLine($"FAIL [{label}] entry {i}: got {actual[i].Name}={actual[i].Value} expected {expected[i].Name}={expected[i].Value}");
                failures++;
            }
        }

        // The read benchmark is only meaningful if FromHashEntries actually rebuilds the object.
        foreach (var model in models)
        {
            var entries = UserDtoMapperContext.Default.UserDto.GetHashEntries(model, UserDtoSerializerContext.Default);
            var restored = UserDtoMapperContext.Default.UserDto.FromHashEntries(entries, UserDtoSerializerContext.Default);

            if (restored.Id != model.Id || restored.Age != model.Age ||
                !string.Equals(restored.FirstName, model.FirstName, StringComparison.Ordinal) ||
                (restored.Tags?.Length ?? 0) != model.Tags.Length ||
                (restored.Data?.Count ?? 0) != model.Data.Count ||
                (restored.Roles?.Length ?? 0) != model.Roles.Length)
            {
                Console.WriteLine($"FAIL [read] round trip mismatch for Id={model.Id}");
                failures++;
            }
        }

        Console.WriteLine(failures == 0 ? "VERIFY OK" : $"VERIFY FAILED ({failures})");
        Environment.ExitCode = failures;
#pragma warning restore RS1035

        // var redis = ConnectionMultiplexer.Connect("localhost");
        // var db = redis.GetDatabase();
        //
        // var person = new Person { Name = "Alice", Age = 30 };
        // var hashEntries = MapperContext.Default.Person.GetHashEntries(person);
        //
        // // Set the object in Redis
        // var redisKey = new RedisKey("person:1");
        // db.HashSet(redisKey, hashEntries);
        //
        // // Get the object back from Redis
        // var retrievedHashEntries = db.HashGetAll(redisKey);
        // var retrievedPerson = MapperContext.Default.Person.FromHashEntries(retrievedHashEntries);
        //
        // // Console.WriteLine($"Name: {retrievedPerson.Name}, Age: {retrievedPerson.Age}");
    }
}

[SimpleJob(RuntimeMoniker.Net60)]
[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[GcServer(true)]
public class WriteBenchmark
{
    private Objects.UserDto[] models = null!;

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
                },
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
[SimpleJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
[GcServer(true)]
public class ReadBenchmark
{
    private HashEntry[] hashEntries = null!;
    [Params(10_000)] public int N;

    [GlobalSetup]
    public void Setup()
    {
        // Produced by the writer so the field names match the context's snake_case naming strategy:
        // hand-written PascalCase names would silently match no property at all.
        hashEntries = UserDtoMapperContext.Default.UserDto.GetHashEntries(new Objects.UserDto
        {
            Id = 1,
            FirstName = "Arash",
            LastName = "Shabbeh",
            Email = "arash.shabbeh@gmail.com",
            Mobile = "09123456789",
            Age = 34,
            Roles = new[] { UserRoleType.Admin, UserRoleType.User },
            Tags = new[] { "super-admin", "moderator", "super-user", "developer" },
            Data = new Dictionary<string, string>
            {
                ["nationality"] = "Iranian",
                ["countryOfResidence"] = "Turkey",
                ["age"] = "34"
            }
        }, UserDtoSerializerContext.Default);
    }

    [Benchmark(Baseline = true, Description = "Read: Array + JsonSerializerOptions")]
    public void Read_Array1()
    {
        for (var i = 0; i < N; i++)
        {
            int id = default, age = default;
            string? firstName = null, lastName = null, email = null, mobile = null;
            UserRoleType[]? roles = null;
            string[]? tags = null;
            Dictionary<string, string>? data = null;

            foreach (var entry in hashEntries)
                switch (entry.Name)
                {
                    case "id": id = (int)entry.Value; break;
                    case "first_name": firstName = entry.Value; break;
                    case "last_name": lastName = entry.Value; break;
                    case "email": email = entry.Value; break;
                    case "mobile": mobile = entry.Value; break;
                    case "age": age = (int)entry.Value; break;
                    case "roles": roles = JsonSerializer.Deserialize<UserRoleType[]>(((ReadOnlyMemory<byte>)entry.Value).Span, UserDtoSerializerContext.Default.Options); break;
                    case "tags": tags = JsonSerializer.Deserialize<string[]>(((ReadOnlyMemory<byte>)entry.Value).Span, UserDtoSerializerContext.Default.Options); break;
                    case "data": data = JsonSerializer.Deserialize<Dictionary<string, string>>(((ReadOnlyMemory<byte>)entry.Value).Span, UserDtoSerializerContext.Default.Options); break;
                }

            _ = new Objects.UserDto
            {
                Id = id, FirstName = firstName!, LastName = lastName!, Email = email, Mobile = mobile,
                Age = age, Roles = roles!, Tags = tags!, Data = data!
            };
        }
    }

    [Benchmark(Description = "Read: Array + JsonSerializerContext")]
    public void Read_Array2()
    {
        for (var i = 0; i < N; i++)
        {
            int id = default, age = default;
            string? firstName = null, lastName = null, email = null, mobile = null;
            UserRoleType[]? roles = null;
            string[]? tags = null;
            Dictionary<string, string>? data = null;

            foreach (var entry in hashEntries)
                switch (entry.Name)
                {
                    case "id": id = (int)entry.Value; break;
                    case "first_name": firstName = entry.Value; break;
                    case "last_name": lastName = entry.Value; break;
                    case "email": email = entry.Value; break;
                    case "mobile": mobile = entry.Value; break;
                    case "age": age = (int)entry.Value; break;
                    case "roles": roles = JsonSerializer.Deserialize(((ReadOnlyMemory<byte>)entry.Value).Span, UserDtoSerializerContext.Default.UserRoleTypeArray); break;
                    case "tags": tags = JsonSerializer.Deserialize(((ReadOnlyMemory<byte>)entry.Value).Span, UserDtoSerializerContext.Default.StringArray); break;
                    case "data": data = JsonSerializer.Deserialize(((ReadOnlyMemory<byte>)entry.Value).Span, UserDtoSerializerContext.Default.DictionaryStringString); break;
                }

            _ = new Objects.UserDto
            {
                Id = id, FirstName = firstName!, LastName = lastName!, Email = email, Mobile = mobile,
                Age = age, Roles = roles!, Tags = tags!, Data = data!
            };
        }
    }

    [Benchmark(Description = "Read: Source Generator")]
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