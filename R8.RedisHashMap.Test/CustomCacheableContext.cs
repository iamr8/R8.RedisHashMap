using System.Text.Json.Serialization;

namespace R8.RedisHashMap.Test;

[JsonSerializable(typeof(UserDto))]
[JsonSerializable(typeof(Dictionary<string, UserDto>))]
public partial class CustomCacheableContext : JsonSerializerContext
{
}