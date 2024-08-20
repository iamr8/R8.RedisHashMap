using System;

namespace R8.RedisHashMap
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CacheableAttribute : Attribute
    {
        public Type Type { get; }

        public CacheableAttribute(Type type)
        {
            Type = type;
        }
    }
}