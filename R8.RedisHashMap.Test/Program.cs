using StackExchange.Redis;
using R8.RedisHashMap;

namespace R8.RedisHashMap.Test
{
    public class Class1
    {
        public static void Main(string[] args)
        {
            var connectionMultiplexer = ConnectionMultiplexer.Connect("localhost");
            var db = connectionMultiplexer.GetDatabase();
            db.HashSetAsync<CacheModel>("test", new CacheModel
            {
                CStringDictionaryProp = new Dictionary<string, C>
                {
                    ["key1"] = new C { StringProp = "123" },
                    ["key2"] = new C { StringProp = "456" },
                    ["key3"] = new C { StringProp = "789" },
                }
            });
            db.HashSet<CacheModel>("test", new CacheModel
            {
                CStringDictionaryProp = new Dictionary<string, C>
                {
                    ["key1"] = new C { StringProp = "123" },
                    ["key2"] = new C { StringProp = "456" },
                    ["key3"] = new C { StringProp = "789" },
                }
            });
            var batch = db.CreateBatch();
            batch.HashSetAsync<CacheModel>("test", new CacheModel
            {
                CStringDictionaryProp = new Dictionary<string, C>
                {
                    ["key1"] = new C { StringProp = "123" },
                    ["key2"] = new C { StringProp = "456" },
                    ["key3"] = new C { StringProp = "789" },
                }
            });
            
            var m = db.HashGetAll<CacheModel>("test");
            var m2 = db.HashGetAllAsync<CacheModel>("test");
            var m3 = batch.HashGetAllAsync<CacheModel>("test");
        }
    }
}