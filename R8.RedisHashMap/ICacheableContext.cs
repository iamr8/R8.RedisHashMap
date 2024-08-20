using System;

namespace R8.RedisHashMap
{
    public interface ICacheableContext
    {
        CacheableTypeInfo? GetTypeInfo(Type type);
    }
}