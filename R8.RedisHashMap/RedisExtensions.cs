using System.Threading.Tasks;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public static class RedisExtensions
    {
        /// <summary>
        /// Sets the specified fields to their respective values in the hash stored at key.
        /// This command overwrites any specified fields that already exist in the hash, leaving other unspecified fields untouched.
        /// If key does not exist, a new key holding a hash is created.
        /// </summary>
        /// <param name="db">The redis database.</param>
        /// <param name="key">The key of the hash.</param>
        /// <param name="hashModel">The hash model to set.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <remarks><seealso href="https://redis.io/commands/hmset"/></remarks>
        public static Task HashSetAsync<T>(this IDatabaseAsync db, RedisKey key, T hashModel, CommandFlags flags = CommandFlags.None) where T : class, IRedisHashMap
        {
            var hashEntries = hashModel.GetHashEntries();
            return db.HashSetAsync(key, hashEntries, flags);
        }

        /// <summary>
        /// Sets the specified fields to their respective values in the hash stored at key.
        /// This command overwrites any specified fields that already exist in the hash, leaving other unspecified fields untouched.
        /// If key does not exist, a new key holding a hash is created.
        /// </summary>
        /// <param name="db">The redis database.</param>
        /// <param name="key">The key of the hash.</param>
        /// <param name="hashModel">The hash model to set.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <remarks><seealso href="https://redis.io/commands/hmset"/></remarks>
        public static void HashSet<T>(this IDatabase db, RedisKey key, T hashModel, CommandFlags flags = CommandFlags.None) where T : class, IRedisHashMap
        {
            var hashEntries = hashModel.GetHashEntries();
            db.HashSet(key, hashEntries, flags);
        }

        /// <summary>
        /// Returns the values associated with the specified fields in the hash stored at key.
        /// For every field that does not exist in the hash, a nil value is returned.Because a non-existing keys are treated as empty hashes, running HMGET against a non-existing key will return a list of nil values.
        /// </summary>
        /// <param name="db">The redis database.</param>
        /// <param name="key">The key of the hash.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <returns>A model with values associated with the given fields.</returns>
        /// <remarks><seealso href="https://redis.io/commands/hmget"/></remarks>
        public static T HashGetAll<T>(this IDatabase db, RedisKey key, CommandFlags flags = CommandFlags.None) where T : class, IRedisHashMap, new()
        {
            var hashModel = new T();
            var fields = hashModel.GetHashFields();
            var values = db.HashGet(key, fields, flags);
            hashModel.Init(fields, values);
            return hashModel;
        }

        /// <summary>
        /// Returns the values associated with the specified fields in the hash stored at key.
        /// For every field that does not exist in the hash, a nil value is returned.Because a non-existing keys are treated as empty hashes, running HMGET against a non-existing key will return a list of nil values.
        /// </summary>
        /// <param name="db">The redis database.</param>
        /// <param name="key">The key of the hash.</param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the model with values associated with the given fields.</returns>
        /// <remarks><seealso href="https://redis.io/commands/hmget"/></remarks>
        public static async Task<T> HashGetAllAsync<T>(this IDatabaseAsync db, RedisKey key, CommandFlags flags = CommandFlags.None) where T : class, IRedisHashMap, new()
        {
            var hashModel = new T();
            var fields = hashModel.GetHashFields();
            var values = await db.HashGetAsync(key, fields, flags);
            hashModel.Init(fields, values);
            return hashModel;
        }
    }
}