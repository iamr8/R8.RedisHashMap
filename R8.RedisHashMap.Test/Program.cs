using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using R8.RedisHashMap.Test.Map;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test
{
    public class Class1
    {
        public static async Task Main(string[] args)
        {
            // var connectionMultiplexer = await ConnectionMultiplexer.ConnectAsync("localhost");
            // var db = connectionMultiplexer.GetDatabase();
            //
            // var stopWatch = System.Diagnostics.Stopwatch.StartNew();
            // await db.HashSetAsync("test-with-cache-dict-source", new Dictionary<int, UserDto>
            // {
            //     [1] = new UserDto
            //     {
            //         Id = 1,
            //         FirstName = "Arash",
            //         LastName = "Shabbeh",
            //         Email = "arash.shabbeh@gmail.com",
            //         Mobile = "09123456789",
            //         Roles = [UserRoleType.Admin, UserRoleType.User],
            //         Tags = ["super-admin", "moderator", "super-user", "developer"],
            //         Data = new Dictionary<string, string>
            //         {
            //             ["nationality"] = "Iranian",
            //             ["countryOfResidence"] = "Turkey",
            //             ["age"] = "34",
            //         },
            //         Duration = TimeSpan.FromDays(1)
            //     },
            //     [2] = new UserDto
            //     {
            //         Id = 2,
            //         FirstName = "John",
            //         LastName = "Doe",
            //         Email = "zaer.abood@migeyl.com",
            //         Mobile = "09123456789",
            //         Roles = [UserRoleType.Admin, UserRoleType.User],
            //         Tags = ["developer"],
            //         Duration = TimeSpan.FromMinutes(1)
            //     }
            // }, CustomCacheableContext.Default);
            // stopWatch.Stop();
            // Console.WriteLine($"HashSet with Dictionary: {stopWatch.ElapsedMilliseconds}ms");
            //
            // stopWatch.Restart();
            // await db.HashSetAsync("test-with-cache-source", new UserDto
            // {
            //     Id = 1,
            //     FirstName = "Arash",
            //     LastName = "Shabbeh",
            //     Email = "arash.shabbeh@gmail.com",
            //     Mobile = "09123456789",
            //     Roles = [UserRoleType.Admin, UserRoleType.User],
            //     Tags = ["super-admin", "moderator", "super-user", "developer"],
            //     Data = new Dictionary<string, string>
            //     {
            //         ["nationality"] = "Iranian",
            //         ["countryOfResidence"] = "Turkey",
            //         ["age"] = "34",
            //     },
            //     Duration = TimeSpan.FromDays(1)
            // }, CustomCacheableContext.Default);
            // stopWatch.Stop();
            // Console.WriteLine($"HashSet with Model: {stopWatch.ElapsedMilliseconds}ms");
            //
            // stopWatch.Restart();
            // var read12 = await db.HashGetAsync<UserDto>("test-with-cache-source", CustomCacheableContext.Default);
            // stopWatch.Stop();
            // Console.WriteLine($"HashGet with Model: {stopWatch.ElapsedMilliseconds}ms");
            //
            // stopWatch.Restart();
            // var read22 = await db.HashGetAsync<Dictionary<int, UserDto>>("test-with-cache-dict-source", CustomCacheableContext.Default);
            // stopWatch.Stop();
            // Console.WriteLine($"HashGet with Dictionary: {stopWatch.ElapsedMilliseconds}ms");

            BenchmarkRunner.Run<CacheBenchmark>();
        }
    }

    [SimpleJob(RunStrategy.Throughput)]
    [SimpleJob(RuntimeMoniker.Net60)]
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
        public void SourceGenerator()
        {
            foreach (var model in models)
            {
                // var hashEntries = model.GetHashEntries();
                // database.HashSet("set-source", hashEntries, CommandFlags.None);
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
    }
}