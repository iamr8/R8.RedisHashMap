using System.Collections;
using System.Text;
using System.Text.Json;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Map;

public static class MapCache
{
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

        databaseConnection.HashSet(cacheKey, he, flags: flags);
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

    private static readonly byte[][] IgnoredValues =
    {
        Encoding.UTF8.GetBytes("\"\""),
        Encoding.UTF8.GetBytes("null"),
        Encoding.UTF8.GetBytes("\"null\"")
    };
}