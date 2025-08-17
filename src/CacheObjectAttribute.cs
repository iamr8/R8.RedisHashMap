using System;

namespace R8.RedisHashMap
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
    public class CacheObjectAttribute : Attribute
    {
        public Type Type { get; }
        public CacheGenerationMode GenerationMode { get; set; } = CacheGenerationMode.Default;

        public CacheObjectAttribute(Type type)
        {
            Type = type;
        }
    }
}