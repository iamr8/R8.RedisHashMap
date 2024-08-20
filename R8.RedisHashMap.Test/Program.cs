using System.Security.Cryptography;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using StackExchange.Redis;
using R8.RedisHashMap;

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

            var summary = BenchmarkRunner.Run<CacheBenchmark>();
        }
    }

    [MemoryDiagnoser(false)]
    [ThreadingDiagnoser]
    public class CacheBenchmark
    {
        private UserDto model;
        private ConnectionMultiplexer connectionMultiplexer;
        private IDatabase database;

        [GlobalSetup]
        public void Setup()
        {
            connectionMultiplexer = ConnectionMultiplexer.Connect("localhost");
            database = connectionMultiplexer.GetDatabase();
            model = new UserDto
            {
                Id = 1,
                FirstName = "Arash",
                LastName = "Shabbeh",
                Email = "arash.shabbeh@gmail.com",
                Mobile = "09123456789",
                Roles = [UserRoleType.Admin, UserRoleType.User],
                Tags = ["super-admin", "moderator", "super-user", "developer"],
                Data = new Dictionary<string, string>
                {
                    ["nationality"] = "Iranian",
                    ["countryOfResidence"] = "Turkey",
                    ["age"] = "34",
                },
            };
        }

        [Benchmark(Baseline = true)]
        public void HMSET_Plain()
        {
            var hashEntries = new HashEntry[]
            {
                new HashEntry("Id", model.Id),
                new HashEntry("FirstName", model.FirstName),
                new HashEntry("LastName", model.LastName),
                new HashEntry("Email", model.Email),
                new HashEntry("Mobile", model.Mobile),
                new HashEntry("Roles", JsonSerializer.Serialize(model.Roles, CustomCacheableContext.Default.UserRoleTypeArray)),
                new HashEntry("Tags", JsonSerializer.Serialize(model.Tags, CustomCacheableContext.Default.StringArray)),
                new HashEntry("Data", JsonSerializer.Serialize(model.Data, CustomCacheableContext.Default.DictionaryStringString)),
            };
            database.HashSet("set-plain", hashEntries, CommandFlags.None);
        }

        [Benchmark]
        public void HMSET_Source()
        {
            database.HashSet("set-source", model, CustomCacheableContext.Default, CommandFlags.None);
        }
    }
}