using System;
using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    [DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
    public class TypeSymbol : IEquatable<TypeSymbol>
    {
        private TypeSymbol(ITypeSymbol type, ISymbol? symbol, bool isNullable)
        {
            Type = type;
            Symbol = symbol;

            IsPropertyOrField = symbol != null;
            IsValueType = type.IsValueType;
            IsPrimitiveType = IsValueType && (type.SpecialType == SpecialType.System_Boolean ||
                                              type.SpecialType == SpecialType.System_Char ||
                                              type.SpecialType == SpecialType.System_SByte ||
                                              type.SpecialType == SpecialType.System_Byte ||
                                              type.SpecialType == SpecialType.System_Int16 ||
                                              type.SpecialType == SpecialType.System_UInt16 ||
                                              type.SpecialType == SpecialType.System_Int32 ||
                                              type.SpecialType == SpecialType.System_UInt32 ||
                                              type.SpecialType == SpecialType.System_Int64 ||
                                              type.SpecialType == SpecialType.System_UInt64 ||
                                              type.SpecialType == SpecialType.System_Decimal ||
                                              type.SpecialType == SpecialType.System_Single ||
                                              type.SpecialType == SpecialType.System_Double);
            IsEnum = IsValueType && type.TypeKind == TypeKind.Enum;
            IsReferenceType = type.IsReferenceType;
            IsString = IsReferenceType && type.SpecialType == SpecialType.System_String;
            IsNullable = isNullable;
            IsArray = type is IArrayTypeSymbol;
            IsCollection = type.AllInterfaces.Any(x => x.Name.Equals(nameof(ICollection), StringComparison.Ordinal) ||
                                                       x.SpecialType == SpecialType.System_Collections_Generic_ICollection_T);
            IsList = type.AllInterfaces.Any(x => x.Name.Equals(nameof(IList), StringComparison.Ordinal) ||
                                                 x.SpecialType == SpecialType.System_Collections_Generic_IList_T);
            IsDictionary = type.AllInterfaces.Any(x => x.Name.Equals(nameof(IDictionary), StringComparison.Ordinal));
            IsJsonDocument = IsReferenceType && type.Name.Equals(nameof(JsonDocument), StringComparison.Ordinal);
            IsJsonElement = IsValueType && type.Name.Equals(nameof(JsonElement), StringComparison.Ordinal);

            EnumUnderlyingType = IsEnum ? ((INamedTypeSymbol)Type).EnumUnderlyingType : null;

            if (IsArray)
            {
                var arrayType = (IArrayTypeSymbol)type;
                Arguments = new[] { Create(arrayType.ElementType) }.ToImmutableArray();
            }
            else if (type is INamedTypeSymbol nts && nts.TypeArguments.Length > 0)
            {
                Arguments = nts.TypeArguments.Select(Create).ToImmutableArray();
            }
            else
            {
                Arguments = ImmutableArray<TypeSymbol>.Empty;
            }

            IsReadOnlyMemory = type.Name.Equals("ReadOnlyMemory", StringComparison.Ordinal) && this.Arguments.Length > 0;
            IsBytesArray = IsArray && Arguments.Length == 1 && this.Arguments[0].Type.SpecialType == SpecialType.System_Byte;
            Converter = ConverterTypeSymbol.GetConverter(this);
        }

        public static TypeSymbol Create(ITypeSymbol typeSymbol)
        {
            if (typeSymbol is { IsValueType: true, IsUnmanagedType: true } && typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                if (typeSymbol is INamedTypeSymbol nts)
                {
                    var genericType = nts.TypeArguments[0];
                    return new TypeSymbol(genericType, null, true);
                }
                else
                {
                    throw new NotSupportedException($"Cannot convert {typeSymbol} to {typeof(TypeSymbol)}");
                }
            }
            else
            {
                var isNullable = TryGetNullableUnderlyingType(typeSymbol, out var underlyingTypeSymbol);
                return new TypeSymbol(isNullable ? underlyingTypeSymbol! : typeSymbol, null, isNullable);
            }
        }

        public static TypeSymbol Create(ISymbol symbol)
        {
            var typeSymbol = GetTypeSymbol(symbol);
            if (typeSymbol is { IsValueType: true, IsUnmanagedType: true } && typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                if (typeSymbol is INamedTypeSymbol nts)
                {
                    var genericType = nts.TypeArguments[0];
                    return new TypeSymbol(genericType, symbol, true);
                }
                else
                {
                    throw new NotSupportedException($"Cannot convert {typeSymbol} to {typeof(TypeSymbol)}");
                }
            }
            else
            {
                var isNullable = TryGetNullableUnderlyingType(typeSymbol, out var underlyingTypeSymbol);
                return new TypeSymbol(isNullable ? underlyingTypeSymbol! : typeSymbol, symbol, isNullable);
            }
        }

        public ISymbol? Symbol { get; }
        public ITypeSymbol Type { get; }

        public ITypeSymbol? EnumUnderlyingType { get; }

        public ImmutableArray<TypeSymbol> Arguments { get; }

        public bool IsPropertyOrField { get; }
        public bool IsNullable { get; }

        /// <inheritdoc cref="ITypeSymbol.IsValueType"/>
        public bool IsValueType { get; }

        public bool IsPrimitiveType { get; }

        public bool IsEnum { get; }
        public bool IsReadOnlyMemory { get; }

        /// <inheritdoc cref="ITypeSymbol.IsReferenceType"/>
        public bool IsReferenceType { get; }

        public bool IsString { get; }
        public bool IsArray { get; }
        public bool IsBytesArray { get; }
        public bool IsCollection { get; }
        public bool IsList { get; }
        public bool IsDictionary { get; }
        public bool IsJsonDocument { get; }
        public bool IsJsonElement { get; }
        public ConverterTypeSymbol? Converter { get; }

        public static ITypeSymbol GetTypeSymbol(ISymbol symbol)
        {
            return symbol switch
            {
                IPropertySymbol ps => ps.Type,
                IFieldSymbol fs => fs.Type,
                _ => null
            };
        }

        public string GetDisplayName()
        {
            var sb = new StringBuilder();
            if (this.IsNullable)
            {
                sb.Append("Nullable");
            }

            sb.Append(this.Type.Name);

            if (this.Arguments.Length > 0)
            {
                foreach (var argument in this.Arguments)
                {
                    sb.Append(argument.GetDisplayName());
                }
            }

            if (this.IsArray)
            {
                sb.Append("Array");
            }

            return sb.ToString();
        }

        public bool TryGetConverter([NotNullWhen(true)] out ConverterTypeSymbol? converter)
        {
            if (this.Symbol != null)
            {
                converter = ConverterTypeSymbol.GetConverter(this);
                if (converter != null)
                    return true;
            }
            else
            {
                converter = null;
            }

            return false;
        }

        public static bool TryGetNullableUnderlyingType(ITypeSymbol typeSymbol, out ITypeSymbol? underlyingTypeSymbol)
        {
            if (typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                {
                    if (namedTypeSymbol.TypeArguments.Length == 1)
                    {
                        underlyingTypeSymbol = namedTypeSymbol.TypeArguments[0];
                        return true;
                    }
                }
            }
            else if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            {
                underlyingTypeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                return true;
            }

            underlyingTypeSymbol = null;
            return false;
        }

        public string? GetSetter(ISymbol propertySymbol, bool considerJsonTypeInfo, out bool hasSerializeJson, out bool hasSerializeString, out bool hasSerializeJsonElement)
        {
            var propertyIdentifier = $"obj.{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            var typeIdentifier = $"{this.Type}{(this.IsNullable ? "?" : "")}";

            var content = GetSetterContent(propertySymbol, considerJsonTypeInfo, out hasSerializeJson, out hasSerializeString, out hasSerializeJsonElement);

            var declareLocalVariable = $@"{typeIdentifier} {valueIdentifier} = {propertyIdentifier};
                ";
            if (this.IsReadOnlyMemory)
            {
                return declareLocalVariable + $@"if ({valueIdentifier}.Length > 0)
                {{
                    {content}
                }}";
            }

            if (this.IsJsonElement)
            {
                if (this.IsNullable)
                {
                    return declareLocalVariable + $@"if ({valueIdentifier}.HasValue && {valueIdentifier}.Value.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.Value.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";
                }
                else
                {
                    return declareLocalVariable + $@"if ({valueIdentifier}.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";
                }
            }

            if (this.IsJsonDocument)
            {
                return declareLocalVariable + @$"if ({valueIdentifier} != {(this.IsNullable ? "null" : "default")} && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";
            }

            if (this.IsValueType)
            {
                if (this.IsNullable)
                {
                    return declareLocalVariable + $@"if ({valueIdentifier}.HasValue)
                {{
                    {content}
                }}";
                }
                else
                {
                    return declareLocalVariable + $@"{{
                    {content}
                }}";
                }
            }

            if (this.IsString)
            {
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Length: > 0 }})
                {{
                    {content}
                }}";
            }

            if (this.IsArray)
            {
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Length: > 0 }})
                {{
                    {content}
                }}";
            }

            if (this.IsDictionary || this.IsCollection || this.IsList)
            {
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Count: > 0 }})
                {{
                    {content}
                }}";
            }

            return declareLocalVariable + $@"if ({valueIdentifier} != {(this.IsNullable ? "null" : "default")})
                {{
                    {content}
                }}";
        }

        private string? GetSetterContent(ISymbol propertySymbol, bool considerJsonTypeInfo, out bool hasSerializeJson, out bool hasSerializeString, out bool hasSerializeJsonElement)
        {
            hasSerializeString = false;
            hasSerializeJson = false;
            hasSerializeJsonElement = false;
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "entries[++index] = ";

            if (this.TryGetConverter(out var converter))
            {
                var hasDotValue = this.IsNullable && this.IsValueType;
                return $@"{converter.ConverterType} converter_{converter.ConverterName} = {converter.ConverterType}Instance.Default;
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = converter_{converter.ConverterName}.{nameof(RedisValueConverter<string>.ConvertToRedisValue)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                    if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                    {{
                        {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                    }}";
            }
            else
            {
                if (this.IsPrimitiveType)
                {
                    return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(this.IsNullable ? ".Value" : "")});";
                }
                else if (this.IsEnum)
                {
                    return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({this.EnumUnderlyingType}){valueIdentifier}{(this.IsNullable ? ".Value" : "")});";
                }
                else if (this.IsReadOnlyMemory)
                {
                    return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";
                }
                else if (this.IsJsonElement)
                {
                    hasSerializeJsonElement = true;
                    return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = SerializeJsonElement(bufferWriter, {valueIdentifier}{(this.IsNullable ? ".Value" : "")});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
                }
                else if (this.IsJsonDocument)
                {
                    hasSerializeJson = true;
                    return @$"{nameof(RedisValue)} redis_{propertySymbol.Name} = SerializeJsonElement(bufferWriter, {valueIdentifier}.RootElement);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
                }
                else if (this.IsValueType) // User-defined struct
                {
                    hasSerializeJson = true;
                    return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = SerializeJson(bufferWriter, {valueIdentifier}{(this.IsNullable ? ".Value" : "")}, serializerOptions);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
                }
                else if (this.IsString)
                {
                    hasSerializeString = true;
                    return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = SerializeString(bufferWriter, {valueIdentifier}.AsSpan());
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
                }
                else if (this.IsBytesArray)
                {
                    return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";
                }
                else if (this.IsReferenceType) // User-defined class
                {
                    hasSerializeJson = true;
                    if (considerJsonTypeInfo)
                    {
                        return $@"JsonTypeInfo<{this.Type}>? jsonTypeInfo = jsonSerializerContext.GetTypeInfo(typeof({this.Type})) as JsonTypeInfo<{this.Type}>;
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = SerializeJson(bufferWriter, {valueIdentifier}, jsonTypeInfo);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
                    }
                    else
                    {
                        return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = SerializeJson(bufferWriter, {valueIdentifier}, serializerOptions);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
                    }
                }
            }

            return null;
        }

        public string? GetGetterContent(ISymbol propertySymbol)
        {
            var setter = $"obj.{propertySymbol.Name} = ";
            var typeIdentifier = $"{this.Type}{(this.IsNullable ? "?" : "")}";

            if (this.TryGetConverter(out var converter))
            {
                return $@"{converter.ConverterType} converter_{converter.ConverterName} = {converter.ConverterType}Instance.Default;
                    {setter}converter_{converter.ConverterName}.{nameof(RedisValueConverter<string>.ConvertFromRedisValue)}(entry.Value);";
            }
            else
            {
                if (this.IsPrimitiveType)
                {
                    return $@"{setter}({typeIdentifier})entry.Value;";
                }
                else if (this.IsEnum)
                {
                    return $"{setter}({this.Type})({this.EnumUnderlyingType})entry.Value;";
                }
                else if (this.IsReadOnlyMemory)
                {
                    return $@"{setter}({nameof(ReadOnlyMemory<byte>)}<byte>)entry.Value;";
                }
                else if (this.IsJsonElement)
                {
                    return $@"var utf8JsonReader = new Utf8JsonReader(((ReadOnlyMemory<byte>)entry.Value).Span);
                    {setter}JsonElement.ParseValue(ref utf8JsonReader);";
                }
                else if (this.IsJsonDocument)
                {
                    return @$"var jsonDoc = JsonDocument.Parse((ReadOnlyMemory<byte>)entry.Value);
                    {setter}jsonDoc;";
                }
                else if (this.IsValueType) // User-defined struct
                {
                    return $@"{setter}DeserializeJson<{this.Type}>(entry.Value, serializerOptions);";
                }
                else if (this.IsString)
                {
                    return $@"{setter}DeserializeString(entry.Value);";
                }
                else if (this.IsBytesArray)
                {
                    return $@"{setter}(byte[])entry.Value;";
                }
                else if (this.IsReferenceType) // User-defined class
                {
                    return $@"{setter}DeserializeJson<{this.Type}>(entry.Value, serializerOptions);";
                }
            }

            return null;
        }

        public override string ToString()
        {
            return Type.ToString();
        }

        public string GetDebuggerDisplay()
        {
            var sb = new StringBuilder();
            sb.Append('(');
            sb.Append(Type);
            sb.Append(')');
            if (this.Symbol != null)
                sb.Append(Symbol.Name);
            return sb.ToString();
        }

        public bool Equals(TypeSymbol other)
        {
            return SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol) &&
                   SymbolEqualityComparer.Default.Equals(Type, other.Type);
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeSymbol other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Symbol, Type);
        }

        public static bool operator ==(TypeSymbol left, TypeSymbol right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TypeSymbol left, TypeSymbol right)
        {
            return !left.Equals(right);
        }
    }
}