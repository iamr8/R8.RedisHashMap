using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using R8.RedisHashMap.Test.Map;
using R8.RedisHashMap.Test.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test
{
    public class Class1
    {
        public static void Main(string[] args)
        {
            BenchmarkRunner.Run<CacheBenchmark>();
        }
    }

    [SimpleJob(RuntimeMoniker.Net80)]
    [MemoryDiagnoser]
    [ThreadingDiagnoser]
    [GcServer(true)]
    public class CacheBenchmark
    {
        private UserDto[] models;
        private ConnectionMultiplexer connectionMultiplexer;
        private IDatabase database;

        [Params(100, 1000, 10000)] public int N;

        [GlobalSetup]
        public void Setup()
        {
            connectionMultiplexer = ConnectionMultiplexer.Connect("localhost");
            database = connectionMultiplexer.GetDatabase();
            models = Enumerable.Range(0, N)
                .Select(x => new UserDto
                {
                    Id = x,
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
                        ["age"] = "34",
                    },
                }).ToArray();
        }

        [Benchmark(Baseline = true)]
        public void Array()
        {
            foreach (var model in models)
            {
                var hashEntries = new HashEntry[]
                {
                    new HashEntry("Id", model.Id),
                    new HashEntry("FirstName", (RedisValue)model.FirstName),
                    new HashEntry("LastName", (RedisValue)model.LastName),
                    new HashEntry("Email", (RedisValue)model.Email),
                    new HashEntry("Mobile", (RedisValue)model.Mobile),
                    new HashEntry("Age", model.Age),
                    new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Roles)),
                    new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Tags)),
                    new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Data)),
                };
                database.HashSet("set-plain", hashEntries, CommandFlags.None);
            }
        }

        [Benchmark]
        public void Array_With_SerializerOptions()
        {
            foreach (var model in models)
            {
                var hashEntries = new HashEntry[]
                {
                    new HashEntry("Id", model.Id),
                    new HashEntry("FirstName", (RedisValue)model.FirstName),
                    new HashEntry("LastName", (RedisValue)model.LastName),
                    new HashEntry("Email", (RedisValue)model.Email),
                    new HashEntry("Mobile", (RedisValue)model.Mobile),
                    new HashEntry("Age", model.Age),
                    new HashEntry("Roles", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Roles, UserDtoJsonSerializer.Default.Options)),
                    new HashEntry("Tags", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Tags, UserDtoJsonSerializer.Default.Options)),
                    new HashEntry("Data", (RedisValue)JsonSerializer.SerializeToUtf8Bytes(model.Data, UserDtoJsonSerializer.Default.Options)),
                };
                database.HashSet("set-plain-so", hashEntries, CommandFlags.None);
            }
        }

        [Benchmark]
        public void SourceGenerator()
        {
            var d = RedisValue.Null;
            var f = new Utf8JsonReader(((ReadOnlyMemory<byte>)d).Span);
            var ff = JsonElement.ParseValue(ref f);
            var _model = database.HashGetAll("get-source").Parse();
            foreach (var model in models)
            {
                var hashEntries = model.GetHashEntries();
                database.HashSet("set-source", hashEntries, CommandFlags.None);
            }
        }

        [Benchmark]
        public void SourceGenerator_With_SerializerOptions()
        {
            foreach (var model in models)
            {
                var hashEntries = model.GetHashEntries(UserDtoJsonSerializer.Default.Options);
                database.HashSet("set-source-so", hashEntries, CommandFlags.None);
            }
        }

        [Benchmark]
        public void Map()
        {
            foreach (var model in models)
            {
                database.HashSetAll("set-map", model, null, flags: CommandFlags.None);
            }
        }

        [Benchmark]
        public void Map_With_SerializerOptions()
        {
            foreach (var model in models)
            {
                database.HashSetAll("set-map-so", model, UserDtoJsonSerializer.Default.Options, flags: CommandFlags.None);
            }
        }
    }
}