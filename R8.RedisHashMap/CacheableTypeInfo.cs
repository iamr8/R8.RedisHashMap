using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    public class CacheableTypeInfo
    {
        public ICacheableContext OriginatingResolver { get; set; }
    }

    public class CacheableTypeInfo<T> : CacheableTypeInfo
    {
        public Func<T> ObjectCreator { get; set; } = null!;
        public Func<ReadOnlyMemory<CacheablePropertyInfo>>? PropertyMetadataInitializer { get; set; }

        public void HashSetByModel(IDatabase database, RedisKey cacheKey, [NotNull] T model, CommandFlags flags)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!TryHashSetByModel(model, out var addingHashEntries, out var deletingFields))
                return;

            if (deletingFields.Length > 0)
                database.HashDelete(cacheKey, deletingFields, flags);
            if (addingHashEntries.Length > 0)
                database.HashSet(cacheKey, addingHashEntries, flags);
        }

        public Task HashSetByModelAsync(IDatabase database, RedisKey cacheKey, [DisallowNull] T model, CommandFlags flags)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            if (!TryHashSetByModel(model, out var addingHashEntries, out var deletingFields))
                return Task.CompletedTask;

            var tasks = new List<Task>();
            if (deletingFields.Length > 0)
                tasks.Add(database.HashDeleteAsync(cacheKey, deletingFields, flags));
            if (addingHashEntries.Length > 0)
                tasks.Add(database.HashSetAsync(cacheKey, addingHashEntries, flags));
            return Task.WhenAll(tasks);
        }

        public void HashSetByDictionary(IDatabase database, RedisKey cacheKey, IDictionary dictionary, CommandFlags flags)
        {
            if (!TryHashSetByDictionary(dictionary, out var addingHashEntries))
                return;

            if (addingHashEntries.Length > 0)
                database.HashSet(cacheKey, addingHashEntries, flags);
        }

        public Task HashSetByDictionaryAsync(IDatabase database, RedisKey cacheKey, IDictionary dictionary, CommandFlags flags)
        {
            if (!TryHashSetByDictionary(dictionary, out var addingHashEntries))
                return Task.CompletedTask;

            return database.HashSetAsync(cacheKey, addingHashEntries, flags);
        }

        private bool TryHashSetByDictionary(IDictionary dictionary, out HashEntry[] addingHashEntries)
        {
            var props = this.PropertyMetadataInitializer!.Invoke();
            var keyProp = props.Span[0];
            var valueProp = props.Span[1];

            // Should we delete all items before set???

            Span<HashEntry> _addingHashEntries = new HashEntry[dictionary.Count];
            var lastAddingIndex = -1;
            var valueTypeChecked = false;
            var keyTypeChecked = false;
            // foreach (DictionaryEntry entry in dictionary)
            // {
            //     var key = entry.Key;
            //     if (!keyTypeChecked)
            //     {
            //         var keyType = key.GetType();
            //         if (keyType != keyProp.ValueType)
            //         {
            //             // LOG
            //             continue;
            //         }
            //
            //         keyTypeChecked = true;
            //     }
            //
            //     if (!valueTypeChecked)
            //     {
            //         var valueType = entry.Value.GetType();
            //         if (valueType != valueProp.ValueType)
            //         {
            //             // LOG
            //             continue;
            //         }
            //
            //         valueTypeChecked = true;
            //     }
            //
            //     var value = entry.Value;
            //     if (value.Equals(default))
            //     {
            //         // LOG
            //         continue;
            //     }
            //
            //     // var convertedValue = prop.Converter?.ConvertToRedisValue(value) ?? RedisValue.Unbox(value);
            //     // if (convertedValue.Equals(default))
            //     // {
            //     //     deletingFields.Span[++lastDeletingIndex] = prop.PropertyName;
            //     //     continue;
            //     // }
            //
            //     var redisValue = valueProp.Generate!.Invoke(value);
            //
            //     if (!redisValue.IsNullOrEmpty)
            //     {
            //         _addingHashEntries[++lastAddingIndex] = new HashEntry(key.ToString(), redisValue);
            //     }
            //     else
            //     {
            //         // LOG
            //     }
            // }
            //
            // if (lastAddingIndex == -1)
            // {
            //     addingHashEntries = Array.Empty<HashEntry>();
            //     return false;
            // }

            addingHashEntries = _addingHashEntries.Slice(0, lastAddingIndex + 1).ToArray();
            return true;
        }

        private bool TryHashSetByModel([DisallowNull] T model, out HashEntry[] addingHashEntries, out RedisValue[] deletingFields)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var props = this.PropertyMetadataInitializer!.Invoke();

            Span<HashEntry> _addingHashEntries = new HashEntry[props.Length];
            Span<RedisValue> _deletingFields = new RedisValue[props.Length];
            var lastAddingIndex = -1;
            var lastDeletingIndex = -1;
            // for (var i = 0; i < props.Length; i++)
            // {
            //     var prop = props.Span[i];
            //     var value = prop.Get?.Invoke(model);
            //     if (value == null || value.Equals(default))
            //     {
            //         _deletingFields[++lastDeletingIndex] = prop.FieldName;
            //         continue;
            //     }
            //
            //     var redisValue = prop.Generate!.Invoke(value);
            //
            //     if (!redisValue.IsNullOrEmpty)
            //     {
            //         _addingHashEntries[++lastAddingIndex] = new HashEntry(prop.FieldName, redisValue);
            //     }
            //     else
            //     {
            //         _deletingFields[++lastDeletingIndex] = prop.FieldName;
            //     }
            // }

            // What happens when `lastDeletingIndex` and `lastAddingIndex` are both -1? LOG
            if (lastAddingIndex == -1 && lastDeletingIndex == -1)
            {
                addingHashEntries = Array.Empty<HashEntry>();
                deletingFields = Array.Empty<RedisValue>();
                return false;
            }

            addingHashEntries = Array.Empty<HashEntry>();
            deletingFields = Array.Empty<RedisValue>();

            if (lastDeletingIndex > -1)
            {
                deletingFields = _deletingFields.Slice(0, lastDeletingIndex + 1).ToArray();
            }

            if (lastAddingIndex > -1)
            {
                addingHashEntries = _addingHashEntries.Slice(0, lastAddingIndex + 1).ToArray();
            }

            return true;
        }

        public T HashGetByModel(IDatabase database, RedisKey cacheKey, ICollection<RedisValue> fields, CommandFlags flags)
        {
            var jsonTypeInfoResolver = this.OriginatingResolver as IJsonTypeInfoResolver;
            var hashEntries = HashGet(database, cacheKey, fields, flags);
            return MapHashEntriesToModel(hashEntries);
        }

        public T HashGetByDictionary(IDatabase database, RedisKey cacheKey, ICollection<RedisValue> fields, CommandFlags flags)
        {
            var jsonTypeInfoResolver = this.OriginatingResolver as IJsonTypeInfoResolver;
            var hashEntries = HashGet(database, cacheKey, fields, flags);
            return MapHashEntriesToDictionary(hashEntries);
        }

        public async Task<T> HashGetByDictionaryAsync(IDatabase database, RedisKey cacheKey, ICollection<RedisValue> fields, CommandFlags flags)
        {
            var jsonTypeInfoResolver = this.OriginatingResolver as IJsonTypeInfoResolver;
            var hashEntries = await HashGetAsync(database, cacheKey, fields, flags);
            return MapHashEntriesToDictionary(hashEntries);
        }

        public async Task<T> HashGetByModelAsync(IDatabase database, RedisKey cacheKey, ICollection<RedisValue> fields, CommandFlags flags)
        {
            var jsonTypeInfoResolver = this.OriginatingResolver as IJsonTypeInfoResolver;
            var hashEntries = await HashGetAsync(database, cacheKey, fields, flags);
            return MapHashEntriesToModel(hashEntries);
        }

        private static HashEntry[] HashGet(IDatabase database, RedisKey cacheKey, ICollection<RedisValue> fields, CommandFlags flags)
        {
            HashEntry[] hashEntries;
            if (fields.Count == 0)
            {
                hashEntries = database.HashGetAll(cacheKey, flags);
            }
            else
            {
                hashEntries = database.HashGet(cacheKey, fields as RedisValue[] ?? fields.ToArray(), flags).Zip(fields, (value, field) => new HashEntry(field, value)).ToArray();
            }

            return hashEntries;
        }

        private static async Task<HashEntry[]> HashGetAsync(IDatabase database, RedisKey cacheKey, ICollection<RedisValue> fields, CommandFlags flags)
        {
            HashEntry[] hashEntries;
            if (fields.Count == 0)
            {
                hashEntries = await database.HashGetAllAsync(cacheKey, flags);
            }
            else
            {
                hashEntries = (await database.HashGetAsync(cacheKey, fields as RedisValue[] ?? fields.ToArray(), flags)).Zip(fields, (value, field) => new HashEntry(field, value)).ToArray();
            }

            return hashEntries;
        }

        private T MapHashEntriesToDictionary(HashEntry[] hashEntries)
        {
            // var obj = this.ObjectCreator.Invoke();
            // if (!(obj is IDictionary dictionary))
            //     throw new InvalidOperationException();
            // var props = this.PropertyMetadataInitializer!.Invoke();
            // var keyProp = props.Span[0];
            // var valueProp = props.Span[1];
            // for (var i = 0; i < hashEntries.Length; i++)
            // {
            //     var hashEntry = hashEntries[i];
            //     if (hashEntry.Name.IsNullOrEmpty)
            //     {
            //         // LOG
            //         continue;
            //     }
            //
            //     object? value;
            //     if (hashEntry.Value.IsNullOrEmpty)
            //     {
            //         value = valueProp.SetDefault!.Invoke();
            //     }
            //     else
            //     {
            //         value = valueProp.Parse!.Invoke(hashEntry.Value);
            //         // var convertedValue = prop.Converter?.ConvertFromRedisValue(readingValue) ?? readingValue;
            //     }
            //
            //     var key = keyProp.Parse!.Invoke(hashEntry.Name);
            //     dictionary.Add(key!, value);
            // }
            //
            // return obj;
            return default;
        }

        private T MapHashEntriesToModel(HashEntry[] hashEntries)
        {
            // var obj = this.ObjectCreator.Invoke();
            // var props = this.PropertyMetadataInitializer!.Invoke();
            // for (var i = 0; i < props.Length; i++)
            // {
            //     var prop = props.Span[i];
            //     var hashEntry = hashEntries.SingleOrDefault(x => x.Name.Equals(prop.FieldName));
            //     if (hashEntry.Name.IsNullOrEmpty)
            //     {
            //         // LOG
            //         continue;
            //     }
            //
            //     object? value;
            //     if (hashEntry.Value.IsNullOrEmpty)
            //     {
            //         value = prop.SetDefault!.Invoke();
            //     }
            //     else
            //     {
            //         value = prop.Parse!.Invoke(hashEntry.Value);
            //         // var convertedValue = prop.Converter?.ConvertFromRedisValue(readingValue) ?? readingValue;
            //     }
            //
            //     prop.Set!.Invoke(obj!, value);
            // }
            //
            // return obj;
            return default;
        }
    }
}