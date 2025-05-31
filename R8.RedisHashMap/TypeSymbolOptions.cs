using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    internal class TypeSymbolOptions
    {
        public INamedTypeSymbol TypeSymbol { get; set; }
        public CacheableFieldNamingStrategy NamingStrategy { get; set; }
    }
}