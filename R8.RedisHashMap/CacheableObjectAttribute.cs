using System;

namespace R8.RedisHashMap
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class CacheableObjectAttribute : Attribute
    {
        public CacheableObjectAttribute()
        {
            NamingStrategy = CacheableFieldNamingStrategy.PascalCase;
        }

        public CacheableObjectAttribute(CacheableFieldNamingStrategy namingStrategy)
        {
            NamingStrategy = namingStrategy;
        }

        public CacheableFieldNamingStrategy NamingStrategy { get; }
    }
}