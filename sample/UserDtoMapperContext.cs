using R8.RedisHashMap.Test.Objects;

namespace R8.RedisHashMap.Test;

[CacheContext(NamingStrategy = CacheFieldNamingStrategy.SnakeCase)]
[CacheObject(typeof(UserDto), GenerationMode = CacheGenerationMode.Serialization)] // Must override the default generation mode
[CacheObject(typeof(AdminDto))]
[CacheObject(typeof(PhysicalCardCacheModel))]
[CacheObject(typeof(LocalizedValueCollection))]
[CacheObject(typeof(Class1.Person))]
public partial class UserDtoMapperContext
{
}