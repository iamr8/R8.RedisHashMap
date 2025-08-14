using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    internal class TypeSymbolOptions
    {
        public INamedTypeSymbol TypeSymbol { get; set; }
        public TypeSymbol ObjectTypeSymbol { get; set; }
        public CacheableFieldNamingStrategy NamingStrategy { get; set; }
        public CacheableGenerationMode GenerationMode { get; set; }
        public List<TypeSymbol> Properties { get; set; } = new List<TypeSymbol>();
        public List<ConverterTypeSymbol> Converters { get; set; } = new List<ConverterTypeSymbol>();
        public string FullQualifiedName { get; set; }
        public string AssemblyName { get; set; }
        public string Namespace { get; set; }
        public string DisplayName { get; set; }
        public string AccessibilityModifier { get; set; }
        public SyntaxToken Keyword { get; set; }
    }
}