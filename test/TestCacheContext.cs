using R8.RedisHashMap.Tests.Models;

namespace R8.RedisHashMap.Tests;

[CacheContext(NamingStrategy = CacheFieldNamingStrategy.CamelCase)]
[CacheObject(typeof(TestUser))]
[CacheObject(typeof(TestProduct))]
[CacheObject(typeof(TestSession))]
[CacheObject(typeof(AdvancedTestModel))]
[CacheObject(typeof(StressModel))]
[CacheObject(typeof(ConverterAnnotatedModel))]
public partial class TestCacheContext
{
}