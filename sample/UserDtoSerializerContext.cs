using System.Text.Json.Serialization;
using R8.RedisHashMap.Test.Objects;

namespace R8.RedisHashMap.Test;

[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(AdminDto))]
// [JsonSerializable(typeof(PhysicalCardCacheModel))]
[JsonSerializable(typeof(LocalizedValueCollection))]
[JsonSerializable(typeof(Class1.Person))]
public partial class UserDtoSerializerContext : JsonSerializerContext
{
}