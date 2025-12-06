using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Map;

public static class MapCache
{
    private static readonly byte[][] IgnoredValues =
    {
        Encoding.UTF8.GetBytes("\"\""),
        Encoding.UTF8.GetBytes("null"),
        Encoding.UTF8.GetBytes("\"null\"")
    };

    public static void HashSetAll<TModel>(this IDatabase databaseConnection, RedisKey cacheKey, TModel model, JsonSerializerOptions serializerOptions, CommandFlags flags = CommandFlags.FireAndForget)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        var hashEntries = GetHashEntries(model, serializerOptions);
        if (hashEntries == null || hashEntries.Count == 0)
            throw new ArgumentNullException(nameof(hashEntries));

        if (hashEntries.Count == 0)
            throw new ArgumentException("No fields to set", nameof(hashEntries));

        var he = hashEntries.Where(x => x.Value.IsNullOrEmpty == false).ToArray();

        databaseConnection.HashSet(cacheKey, he, flags);
    }

    internal static IList<HashEntry> GetHashEntries<TModel>(TModel model, JsonSerializerOptions serializerOptions)
    {
        switch (model)
        {
            case HashEntry[] arr:
            {
                return arr;
            }
            case List<HashEntry> arr:
            {
                return arr;
            }
            case IEnumerable<HashEntry> list:
            {
                return list.ToArray();
            }
            case IDictionary dictionary:
            {
                if (dictionary.Count == 0)
                    return Array.Empty<HashEntry>();

                Memory<HashEntry> mem = new HashEntry[dictionary.Count];

                var lastIndex = -1;
                foreach (DictionaryEntry entry in dictionary)
                {
                    var k = entry.Key;
                    var v = dictionary[k];
                    var key = k switch
                    {
                        byte[] bytes => (RedisValue)bytes,
                        RedisValue redisValue => redisValue,
                        string str => (RedisValue)str.ToCamelCase(),
                        _ => (RedisValue)k.ToString().ToCamelCase()
                    };
                    if (v is not null)
                    {
                        var value = v.SerializeToRedisValue(v.GetType(), serializerOptions);
                        mem.Span[++lastIndex] = new HashEntry(key, value);
                    }
                    else
                    {
                        mem.Span[++lastIndex] = new HashEntry(key, RedisValue.Null);
                    }
                }

                mem = mem[..(lastIndex + 1)];
                var arr = mem.ToArray();
                return arr;
            }
            case IList:
            {
                throw new NotSupportedException("List models cannot be converted to HashEntries.");
            }
            default:
            {
                var props = model.GetType().GetCachedProperties();
                if (props.Count == 0)
                    return Array.Empty<HashEntry>();

                Memory<HashEntry> mem = new HashEntry[props.Count];

                var lastIndex = -1;
                foreach (var (key, prop) in props)
                {
                    var v = prop.Property.GetValue(model);
                    var value = v.SerializeToRedisValue(prop.PropertyType, serializerOptions);
                    mem.Span[++lastIndex] = new HashEntry(prop.Name, value);
                }

                mem = mem[..(lastIndex + 1)];
                var arr = mem.ToArray();
                return arr;
            }
        }
    }

    public static RedisValue SerializeToRedisValue(this object value, Type type, JsonSerializerOptions serializerOptions)
    {
        switch (value)
        {
            case null:
                return RedisValue.Null;
            case byte[] bytes:
            {
                if (bytes.Length == 0 || bytes.All(x => x == 0) || IgnoredValues.Any(x => x.SequenceEqual(bytes)))
                    return RedisValue.Null;
                return (RedisValue)bytes;
            }
            case RedisValue redisValue:
                return redisValue;
            default:
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value, type, serializerOptions);
                if (IgnoredValues.Any(x => x.SequenceEqual(bytes)))
                    return RedisValue.Null;
                if (type == typeof(string))
                {
                    var str = Encoding.UTF8.GetString(bytes);
                    var trimmed = str.Trim('"');
                    bytes = Encoding.UTF8.GetBytes(trimmed);
                }

                return (RedisValue)bytes;
            }
        }
    }

    private static object Deserialize(this RedisValue redisValue, Type propertyType, JsonSerializerOptions serializerOptions)
    {
        if (redisValue.IsNullOrEmpty)
            return default;

        object key;
        if (propertyType == typeof(RedisValue))
        {
            key = redisValue;
        }
        else if (propertyType == typeof(byte[]))
        {
            key = (byte[])redisValue;
        }
        else if (propertyType == typeof(string))
        {
            var str = redisValue.ToString();
            if (!str.StartsWith("\""))
                str = $"\"{str}\"";
            if (!str.EndsWith("\""))
                str += "\"";
            key = JsonSerializer.Deserialize(str, propertyType, serializerOptions);
        }
        else
        {
            key = JsonSerializer.Deserialize(((ReadOnlyMemory<byte>)redisValue).Span, propertyType, serializerOptions);
        }

        return key;
    }

    public static Type GetUnderlyingType(this Type type, bool ignoreNullability = true)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var resultType = type.GetEnumerableUnderlyingType() ?? type;
        if (ignoreNullability)
            resultType = Nullable.GetUnderlyingType(resultType) ?? resultType;

        return resultType;
    }

    public static Type GetEnumerableUnderlyingType(this Type type)
    {
        return type.GetGenericUnderlyingType(typeof(IList<>));
    }

    public static Type GetGenericUnderlyingType(this Type type, Type genericType)
    {
        if (type == null)
            throw new ArgumentNullException(nameof(type));

        var interfaces = type.GetInterfaces();
        if (interfaces.Length == 0)
            return null;

        var underlyingType = interfaces
            .Select(interfaceType => new
            {
                Interface = interfaceType,
                HasGeneric = interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == genericType
            })
            .Where(tuple => tuple.HasGeneric)
            .Select(tuple => tuple.Interface.GetGenericArguments())
            .Where(genericTypes => genericTypes.Length > 0)
            .Select(genericTypes => genericTypes[0])
            .FirstOrDefault();

        return underlyingType;
    }

    internal static bool TryDeserialize<T>(this HashEntry[] hashEntries, JsonSerializerOptions serializerOptions, [MaybeNullWhen(false)] out T model) where T : new()
    {
        var type = typeof(T);
        type = type.IsValueType ? type.GetUnderlyingType() : type;
        var props = type.GetCachedProperties();

        model = default;
        var output = new T();
        var hit = 0;
        foreach (var hashEntry in hashEntries)
        {
            if (!props.TryGetValue(hashEntry.Name, out var cachedProp))
                continue;

            var redisValue = hashEntry.Value;
            if (cachedProp.IsRequired)
            {
                if (redisValue.IsNullOrEmpty)
                    return false; // Required property is missed.
            }
            else
            {
                if (redisValue.IsNullOrEmpty)
                    continue;
            }

            if (!cachedProp.HasSetMethod)
                throw new InvalidOperationException($"Property '{cachedProp.Property.Name}' does not have a setter.");

            try
            {
                var value = redisValue.Deserialize(cachedProp.PropertyType, serializerOptions);
                cachedProp.Property.SetValue(output, value);
                hit++;
            }
            catch (JsonException e)
            {
                throw new InvalidCastException($"Cannot convert RedisValue to {cachedProp.PropertyType.Name}.", e);
            }
        }

        if (hit == 0)
            return false; // No property is set.

        model = output;
        return true;
    }
}