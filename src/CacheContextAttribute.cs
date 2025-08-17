using System;

namespace R8.RedisHashMap
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CacheContextAttribute : Attribute
    {
        public CacheFieldNamingStrategy NamingStrategy { get; set; } = CacheFieldNamingStrategy.PascalCase;
        public CacheGenerationMode GenerationMode { get; set; } = CacheGenerationMode.Default;
    }
}