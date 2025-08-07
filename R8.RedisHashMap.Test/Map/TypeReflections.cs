using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Serialization;
using StackExchange.Redis;

namespace R8.RedisHashMap.Test.Map;

public static class TypeReflections
{
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<RedisValue, CachedPropertyInfo>> CachedTypes = new();

    /// <summary>
    ///     Returns properties of a type.
    /// </summary>
    /// <param name="type">A <see cref="Type" /> to get properties from.</param>
    /// <returns>An array of public <see cref="PropertyInfo" />s.</returns>
    /// <exception cref="ArgumentNullException">When the type is null.</exception>
    internal static IReadOnlyDictionary<RedisValue, CachedPropertyInfo> GetCachedProperties(this Type type)
    {
        if (CachedTypes.TryGetValue(type, out var cachedProps))
            return cachedProps;

        var concreteProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dict = new Dictionary<RedisValue, CachedPropertyInfo>(concreteProperties.Length);
        ExtractProperties(concreteProperties);

        if (type.IsInterface)
            foreach (var interfaceType in type.GetInterfaces())
            {
                var interfaceProperties = interfaceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                ExtractProperties(interfaceProperties);
            }

        CachedTypes.TryAdd(type, dict);
        return dict;

        void ExtractProperties(PropertyInfo[] properties)
        {
            foreach (var prop in properties)
            {
                var propName = prop.Name.ToCamelCase();
                var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;
                var finalName = !string.IsNullOrWhiteSpace(jsonName) ? jsonName : propName;
                if (dict.ContainsKey(finalName))
                    continue;

                dict.Add(finalName, new CachedPropertyInfo(finalName, prop));
            }
        }
    }
}