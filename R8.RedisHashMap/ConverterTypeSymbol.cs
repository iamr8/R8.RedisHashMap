using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    public class ConverterTypeSymbol : IEquatable<ConverterTypeSymbol>
    {
        private ConverterTypeSymbol(ITypeSymbol converterTypeSymbol, TypeSymbol targetTypeSymbol)
        {
            ConverterType = converterTypeSymbol;
            TargetType = targetTypeSymbol;
            ConverterName = GetName(ConverterType.Name);
            FieldName = $"field_{ConverterName}";
        }

        public string FieldName { get; }
        public string ConverterName { get; }
        public ITypeSymbol ConverterType { get; }
        public TypeSymbol TargetType { get; }

        private static string GetName(string name)
        {
            if (name.EndsWith("Converter"))
            {
                name = name.Split("Converter")[0];
            }

            if (name.StartsWith("RedisValue"))
            {
                name = name.Split("RedisValue")[1];
            }
            else if (name.StartsWith("Redis"))
            {
                name = name.Split("Redis")[1];
            }

            return name;
        }

        public static ConverterTypeSymbol? GetConverter(TypeSymbol propertySymbol)
        {
            if (propertySymbol.Symbol == null)
                return null;

            var attributes = propertySymbol.Symbol.GetAttributes();
            var converterAttr = attributes.FirstOrDefault(c => c.AttributeClass?.Name.Equals(nameof(CacheableConverterAttribute), StringComparison.Ordinal) == true);
            if (converterAttr == null || !(converterAttr.ConstructorArguments[0].Value! is ITypeSymbol converterTypeSymbol))
                return null;

            var baseType = converterTypeSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name.Equals(nameof(RedisValueConverter), StringComparison.Ordinal))
                    break;

                baseType = baseType.BaseType;
            }

            if (baseType == null || !baseType.Name.Equals(nameof(RedisValueConverter), StringComparison.Ordinal))
                return null;

            var targetTypeSymbol = TypeSymbol.Create(baseType.TypeArguments[0]);
            if (!SymbolEqualityComparer.Default.Equals(targetTypeSymbol.Type, propertySymbol.Type))
                return null;

            return new ConverterTypeSymbol(converterTypeSymbol, targetTypeSymbol);
        }

        public bool Equals(ConverterTypeSymbol? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return SymbolEqualityComparer.Default.Equals(ConverterType, other.ConverterType) && TargetType.Equals(other.TargetType);
        }

        public override bool Equals(object? obj)
        {
            return obj is ConverterTypeSymbol other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = SymbolEqualityComparer.Default.GetHashCode(ConverterType);
            return HashCode.Combine(hashCode, TargetType);
        }

        public static bool operator ==(ConverterTypeSymbol? left, ConverterTypeSymbol? right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(ConverterTypeSymbol? left, ConverterTypeSymbol? right)
        {
            if (ReferenceEquals(left, null))
                return !ReferenceEquals(right, null);

            return !left.Equals(right);
        }
    }
}