using R8.RedisHashMap.Tests.Models;

namespace R8.RedisHashMap.Tests;

[CacheContext(NamingStrategy = CacheFieldNamingStrategy.CamelCase)]
[CacheObject(typeof(TestUser))]
[CacheObject(typeof(TestProduct))]
[CacheObject(typeof(TestSession))]
[CacheObject(typeof(AdvancedTestModel))]
public partial class TestCacheContext
{
}