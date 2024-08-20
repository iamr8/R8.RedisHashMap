using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json.Serialization.Metadata;
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
        /// <param name="cacheKey">The key of the hash.</param>
        /// <param name="model">The hash model to set.</param>
        /// <param name="cacheableContext"></param>
        /// <param name="flags">The flags to use for this operation.</param>
        /// <remarks><seealso href="https://redis.io/commands/hmset"/></remarks>
        public static void HashSet<TModel>(this IDatabase db, RedisKey cacheKey, TModel model, ICacheableContext cacheableContext, CommandFlags flags = CommandFlags.None) where TModel : class
        {
            var typeInfo = cacheableContext.GetTypeInfo(typeof(TModel)) as CacheableTypeInfo<TModel> ?? throw new InvalidOperationException();
            if (model is IDictionary dictionary)
            {
                typeInfo.HashSetByDictionary(db, cacheKey, dictionary, flags);
            }
            else if (model is IEnumerable enumerable)
            {
                throw new NotImplementedException();
            }
            else
            {
                typeInfo.HashSetByModel(db, cacheKey, model, flags);
            }
        }

        public static Task HashSetAsync<TModel>(this IDatabase db, RedisKey cacheKey, TModel model, ICacheableContext cacheableContext, CommandFlags flags = CommandFlags.None) where TModel : class
        {
            var typeInfo = cacheableContext.GetTypeInfo(typeof(TModel)) as CacheableTypeInfo<TModel> ?? throw new InvalidOperationException();
            if (model is IDictionary dictionary)
            {
                return typeInfo.HashSetByDictionaryAsync(db, cacheKey, dictionary, flags);
            }
            else if (model is IEnumerable enumerable)
            {
                throw new NotImplementedException();
            }
            else
            {
                return typeInfo.HashSetByModelAsync(db, cacheKey, model, flags);
            }
        }

        public static TModel HashGet<TModel>(this IDatabase db, RedisKey cacheKey, ICacheableContext cacheableContext, CommandFlags flags = CommandFlags.None) where TModel : class
        {
            var typeInfo = cacheableContext.GetTypeInfo(typeof(TModel)) as CacheableTypeInfo<TModel> ?? throw new InvalidOperationException();
            if (typeof(IDictionary).IsAssignableFrom(typeof(TModel)))
            {
                return typeInfo.HashGetByDictionary(db, cacheKey, Array.Empty<RedisValue>(), flags);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(typeof(TModel)))
            {
                throw new NotImplementedException();
            }
            else
            {
                return typeInfo.HashGetByModel(db, cacheKey, Array.Empty<RedisValue>(), flags);
            }
        }

        public static Task<TModel> HashGetAsync<TModel>(this IDatabase db, RedisKey cacheKey, ICacheableContext cacheableContext, CommandFlags flags = CommandFlags.None) where TModel : class
        {
            var typeInfo = cacheableContext.GetTypeInfo(typeof(TModel)) as CacheableTypeInfo<TModel> ?? throw new InvalidOperationException();
            if (typeof(TModel) == typeof(IDictionary))
            {
                return typeInfo.HashGetByDictionaryAsync(db, cacheKey, Array.Empty<RedisValue>(), flags);
            }
            else if (typeof(TModel).IsAssignableFrom(typeof(IEnumerable)))
            {
                throw new NotImplementedException();
            }
            else
            {
                return typeInfo.HashGetByModelAsync(db, cacheKey, Array.Empty<RedisValue>(), flags);
            }
        }
    }
}