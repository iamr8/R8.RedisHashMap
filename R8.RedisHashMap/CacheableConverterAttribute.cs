using System;

namespace R8.RedisHashMap
{
    public class CacheableConverterAttribute : Attribute
    {
        public Type ConverterType { get; }

        public CacheableConverterAttribute(Type ConverterType)
        {
            this.ConverterType = ConverterType;
        }
    }
}