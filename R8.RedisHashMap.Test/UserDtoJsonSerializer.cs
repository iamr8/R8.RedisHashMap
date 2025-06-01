using System.Text.Json.Serialization;

namespace R8.RedisHashMap.Test;

[JsonSerializable(typeof(UserDto))]
public partial class UserDtoJsonSerializer : JsonSerializerContext
{
}