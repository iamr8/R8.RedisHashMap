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

            IsReadOnlyMemory = type.Name.Equals("ReadOnlyMemory", StringComparison.Ordinal) && Arguments.Length > 0;
            IsBytesArray = IsArray && Arguments.Length == 1 && Arguments[0].Type.SpecialType == SpecialType.System_Byte;
            Converter = ConverterTypeSymbol.GetConverter(this);
        }

        public ISymbol? Symbol { get; }
        public ITypeSymbol Type { get; }

        public ITypeSymbol? EnumUnderlyingType { get; }

        public ImmutableArray<TypeSymbol> Arguments { get; }

        public bool IsPropertyOrField { get; }
        public bool IsNullable { get; }

        /// <inheritdoc cref="ITypeSymbol.IsValueType" />
        public bool IsValueType { get; }

        public bool IsPrimitiveType { get; }

        public bool IsEnum { get; }
        public bool IsReadOnlyMemory { get; }

        /// <inheritdoc cref="ITypeSymbol.IsReferenceType" />
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

        public bool Equals(TypeSymbol other)
        {
            return SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol) &&
                   SymbolEqualityComparer.Default.Equals(Type, other.Type);
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

                throw new NotSupportedException($"Cannot convert {typeSymbol} to {typeof(TypeSymbol)}");
            }

            var isNullable = TryGetNullableUnderlyingType(typeSymbol, out var underlyingTypeSymbol);
            return new TypeSymbol(isNullable ? underlyingTypeSymbol! : typeSymbol, null, isNullable);
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

                throw new NotSupportedException($"Cannot convert {typeSymbol} to {typeof(TypeSymbol)}");
            }

            var isNullable = TryGetNullableUnderlyingType(typeSymbol, out var underlyingTypeSymbol);
            return new TypeSymbol(isNullable ? underlyingTypeSymbol! : typeSymbol, symbol, isNullable);
        }

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
            if (IsNullable) sb.Append("Nullable");

            sb.Append(Type.Name);

            if (Arguments.Length > 0)
                foreach (var argument in Arguments)
                    sb.Append(argument.GetDisplayName());

            if (IsArray) sb.Append("Array");

            return sb.ToString();
        }

        public bool TryGetConverter([NotNullWhen(true)] out ConverterTypeSymbol? converter)
        {
            if (Symbol != null)
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
                    if (namedTypeSymbol.TypeArguments.Length == 1)
                    {
                        underlyingTypeSymbol = namedTypeSymbol.TypeArguments[0];
                        return true;
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

        public string? GetSetterWrapper(ISymbol propertySymbol, string? content)
        {
            var propertyIdentifier = $"this.{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            var declareLocalVariable = $@"{typeIdentifier} {valueIdentifier} = {propertyIdentifier};
                ";
            if (IsReadOnlyMemory)
                return declareLocalVariable + $@"if ({valueIdentifier}.Length > 0)
                {{
                    {content}
                }}";

            if (IsJsonElement)
            {
                if (IsNullable)
                    return declareLocalVariable + $@"if ({valueIdentifier}.HasValue && {valueIdentifier}.Value.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.Value.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";

                return declareLocalVariable + $@"if ({valueIdentifier}.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";
            }

            if (IsJsonDocument)
                return declareLocalVariable + @$"if ({valueIdentifier} != {(IsNullable ? "null" : "default")} && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";

            if (IsValueType)
            {
                if (IsNullable)
                    return declareLocalVariable + $@"if ({valueIdentifier}.HasValue)
                {{
                    {content}
                }}";

                return declareLocalVariable + $@"{{
                    {content}
                }}";
            }

            if (IsString)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Length: > 0 }})
                {{
                    {content}
                }}";

            if (IsArray)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Length: > 0 }})
                {{
                    {content}
                }}";

            if (IsDictionary || IsCollection || IsList)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Count: > 0 }})
                {{
                    {content}
                }}";

            return declareLocalVariable + $@"if ({valueIdentifier} != {(IsNullable ? "null" : "default")})
                {{
                    {content}
                }}";
        }

        internal string? GetSetterContentWithSerializerOptions(ISymbol propertySymbol, out ConverterTypeSymbol? converter)
        {
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "entries[++index] = ";

            if (TryGetConverter(out converter))
            {
                var hasDotValue = IsNullable && IsValueType;
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = converter_{converter.ConverterName}.{nameof(RedisValueConverter<string>.ToRedisValue)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                    if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                    {{
                        {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                    }}";
            }

            if (IsPrimitiveType) return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (IsEnum) return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({EnumUnderlyingType}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (IsReadOnlyMemory) return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (IsJsonElement)
                return $@"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}(bufferWriter, jsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsJsonDocument)
                return @$"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}(bufferWriter, jsonWriter, {valueIdentifier}.RootElement);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsValueType) // User-defined struct
                return $@"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}<{Type}>(bufferWriter, jsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")}, serializerOptions);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsString)
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}(bufferWriter, {valueIdentifier});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsBytesArray) return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (IsReferenceType) // User-defined class
                return $@"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}<{Type}>(bufferWriter, jsonWriter, {valueIdentifier}, serializerOptions);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            return null;
        }

        internal string? GetSetterContentWithSerializerContext(ISymbol propertySymbol, out ConverterTypeSymbol? converter)
        {
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "entries[++index] = ";

            if (TryGetConverter(out converter))
            {
                var hasDotValue = IsNullable && IsValueType;
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = converter_{converter.ConverterName}.{nameof(RedisValueConverter<string>.ToRedisValue)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                    if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                    {{
                        {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                    }}";
            }

            if (IsPrimitiveType) return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (IsEnum) return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({EnumUnderlyingType}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (IsReadOnlyMemory) return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (IsJsonElement)
                return $@"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}(bufferWriter, jsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsJsonDocument)
                return @$"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}(bufferWriter, jsonWriter, {valueIdentifier}.RootElement);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsValueType) // User-defined struct
                return $@"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    JsonTypeInfo<{Type}> jsonTypeInfo = GetJsonTypeInfo<{Type}>(serializerContext);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}<{Type}>(bufferWriter, jsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")}, jsonTypeInfo);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsString)
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}(bufferWriter, {valueIdentifier});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            if (IsBytesArray) return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (IsReferenceType) // User-defined class
                return $@"jsonWriter ??= GetUtf8JsonWriter(bufferWriter);
                    JsonTypeInfo<{Type}> jsonTypeInfo = GetJsonTypeInfo<{Type}>(serializerContext);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Serialize)}<{Type}>(bufferWriter, jsonWriter, {valueIdentifier}, jsonTypeInfo);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";

            return null;
        }

        public string? GetGetterContentWithSerializerOptions(ISymbol propertySymbol, out ConverterTypeSymbol? converter)
        {
            var setter = $"obj.{propertySymbol.Name} = ";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            if (TryGetConverter(out converter)) return $@"{setter}converter_{converter.ConverterName}.{nameof(RedisValueConverter<string>.FromRedisValue)}(entry.Value);";

            if (IsPrimitiveType) return $@"{setter}({typeIdentifier})entry.Value;";

            if (IsEnum) return $"{setter}({Type})({EnumUnderlyingType})entry.Value;";

            if (IsReadOnlyMemory) return $@"{setter}({nameof(ReadOnlyMemory<byte>)}<byte>)entry.Value;";

            if (IsJsonElement) return $@"{setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.DeserializeToJsonElement)}(entry.Value);";

            if (IsJsonDocument) return $@"{setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.DeserializeToJsonDocument)}(entry.Value);";

            if (IsValueType) // User-defined struct
                return $@"{setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Deserialize)}<{Type}>(entry.Value, serializerOptions);";

            if (IsString) return $@"{setter}(string?)entry.Value;";

            if (IsBytesArray) return $@"{setter}(byte[])entry.Value;";

            if (IsReferenceType) // User-defined class
                return $@"{setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Deserialize)}<{Type}>(entry.Value, serializerOptions);";

            return null;
        }

        public string? GetGetterContentWithSerializerContext(ISymbol propertySymbol, out ConverterTypeSymbol? converter)
        {
            var setter = $"obj.{propertySymbol.Name} = ";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            if (TryGetConverter(out converter)) return $@"{setter}converter_{converter.ConverterName}.{nameof(RedisValueConverter<string>.FromRedisValue)}(entry.Value);";

            if (IsPrimitiveType) return $@"{setter}({typeIdentifier})entry.Value;";

            if (IsEnum) return $"{setter}({Type})({EnumUnderlyingType})entry.Value;";

            if (IsReadOnlyMemory) return $@"{setter}({nameof(ReadOnlyMemory<byte>)}<byte>)entry.Value;";

            if (IsJsonElement) return $@"{setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.DeserializeToJsonElement)}(entry.Value);";

            if (IsJsonDocument) return $@"{setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.DeserializeToJsonDocument)}(entry.Value);";

            if (IsValueType) // User-defined struct
                return $@"JsonTypeInfo<{Type}> jsonTypeInfo = GetJsonTypeInfo<{Type}>(serializerContext);
                        {setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Deserialize)}<{Type}>(entry.Value, jsonTypeInfo);";

            if (IsString) return $@"{setter}(string?)entry.Value;";

            if (IsBytesArray) return $@"{setter}(byte[])entry.Value;";

            if (IsReferenceType) // User-defined class
                return $@"JsonTypeInfo<{Type}> jsonTypeInfo = GetJsonTypeInfo<{Type}>(serializerContext);
                        {setter}{nameof(RedisJsonSerializer)}.{nameof(RedisJsonSerializer.Deserialize)}<{Type}>(entry.Value, jsonTypeInfo);";

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
            if (Symbol != null)
                sb.Append(Symbol.Name);
            return sb.ToString();
        }

        public override bool Equals(object? obj)
        {
            return obj is TypeSymbol other && Equals(other);
        }

        public override int GetHashCode()
        {
            return SymbolEqualityComparer.Default.GetHashCode(Symbol) ^
                   SymbolEqualityComparer.Default.GetHashCode(Type);
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