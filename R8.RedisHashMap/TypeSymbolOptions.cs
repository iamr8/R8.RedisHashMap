using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    internal class TypeSymbolOptions
    {
        public INamedTypeSymbol TypeSymbol { get; set; }
        public TypeSymbol ObjectTypeSymbol { get; set; }
        public CacheableFieldNamingStrategy NamingStrategy { get; set; }
        public ImmutableArray<TypeSymbol> Properties { get; set; }
        public string FullQualifiedName { get; set; }
        public string AssemblyName { get; set; }
    }
}