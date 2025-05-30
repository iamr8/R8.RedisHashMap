using System;

namespace R8.RedisHashMap
{
    public class CacheableConverterAttribute : Attribute
    {
        public CacheableConverterAttribute(Type converterType)
        {
        }
    }
}