using System;

namespace R8.RedisHashMap
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public class CacheObject : Attribute
    {
        public CacheableFieldNamingStrategy NamingStrategy { get; set; } = CacheableFieldNamingStrategy.PascalCase;
        public CacheableGenerationMode GenerationMode { get; set; } = CacheableGenerationMode.Default;
    }
}