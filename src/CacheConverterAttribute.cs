using System;

namespace R8.RedisHashMap
{
    public class CacheConverterAttribute : Attribute
    {
        public CacheConverterAttribute(Type converterType)
        {
            ConverterType = converterType;
        }

        public Type ConverterType { get; }
    }
}