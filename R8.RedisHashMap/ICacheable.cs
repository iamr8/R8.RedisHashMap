using System.Collections.Generic;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public interface ICacheable
    {
        // /// <summary>
        // /// Returns all hash entries according to the properties and values.
        // /// </summary>
        // HashEntry[] Set(IDatabaseAsync database, RedisKey prefixKey);
        //
        // /// <summary>
        // /// Returns all values according to the properties.
        // /// </summary>
        // RedisValue[] GetKeysAsRedisValue();
        //
        // /// <summary>
        // /// Returns all keys according to the properties.
        // /// </summary>
        // RedisKey[] GetKeysAsRedisKey(RedisKey prefixKey);
        //
        // /// <summary>
        // /// Returns all key-value pairs according to the properties.
        // /// </summary>
        // KeyValuePair<RedisKey, RedisValue>[] GetKeyValuePairs(RedisKey prefixKey);
        //
        // /// <summary>
        // /// Sets all values according to the properties.
        // /// </summary>
        // void Init(RedisValue[] fields, RedisValue[] values);
    }
}