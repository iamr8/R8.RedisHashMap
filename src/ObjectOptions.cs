using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    [DebuggerDisplay("{ObjectTypeSymbol}")]
    internal class ObjectOptions
    {
        public TypeSymbol ObjectTypeSymbol { get; set; }

        public CacheGenerationMode? GenerationMode { get; set; }
        public List<TypeSymbol> Properties { get; } = new List<TypeSymbol>();
        public List<ConverterTypeSymbol> Converters { get; } = new List<ConverterTypeSymbol>();
        public string Namespace { get; set; }
        public string DisplayName { get; set; }
        public string AccessibilityModifier { get; set; }
        public string HelperName { get; set; }
        public string HelperTypeName { get; set; }
    }

    [DebuggerDisplay("{ObjectTypeSymbol}")]
    internal class ContextOptions
    {
        public TypeSymbol ObjectTypeSymbol { get; set; }
        public string DisplayName { get; set; }
        public string Namespace { get; set; }
        public string AccessibilityModifier { get; set; }
        public SyntaxToken Keyword { get; set; }

        public CacheGenerationMode GenerationMode { get; set; }
        public CacheFieldNamingStrategy NamingStrategy { get; set; }

        public List<ObjectOptions> Types { get; } = new List<ObjectOptions>();

        public List<ConverterTypeSymbol> Converters => Types
            .SelectMany(c => c.Converters)
            .GroupBy(c => c.ConverterType, SymbolEqualityComparer.Default)
            .Select(c => c.First())
            .ToList();
    }
}