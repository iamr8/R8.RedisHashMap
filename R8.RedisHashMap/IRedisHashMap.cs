using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public interface IRedisHashMap
    {
        /// <summary>
        /// Returns all hash entries according to the properties and values.
        /// </summary>
        HashEntry[] GetHashEntries();

        /// <summary>
        /// Returns all hash fields according to the properties.
        /// </summary>
        RedisValue[] GetHashFields();

        /// <summary>
        /// Sets all values according to the properties.
        /// </summary>
        void Init(RedisValue[] fields, RedisValue[] values);
    }
}