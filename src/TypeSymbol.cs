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
        private readonly ImmutableArray<TypeSymbol> _arguments;
        private readonly ITypeSymbol? _enumUnderlyingType;
        public readonly bool HasConverter;

        public readonly bool IsIEnumerable;
        public readonly bool IsArray;
        public readonly bool IsCollection;
        public readonly bool IsDictionary;
        public readonly bool IsList;

        public readonly bool IsString;
        public readonly bool IsReferenceType;
        public readonly bool IsJsonDocument;

        public readonly bool IsValueType;
        public readonly bool IsEnum;
        public readonly bool IsJsonElement;
        public readonly bool IsReadOnlyMemoryOfBytes;

        public readonly bool CastToRedisValue;
        public readonly bool CastFromRedisValue;
        public readonly bool IsRedisValue;

        private TypeSymbol(SourceProductionContext context, ITypeSymbol type, ISymbol? symbol, bool isNullable)
        {
            Type = type;
            Symbol = symbol;

            IsValueType = type.IsValueType;
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

            // When directly uses IEnumerable<>
            IsIEnumerable = type.AllInterfaces.Any(x => x.Name.Equals(nameof(IEnumerable), StringComparison.Ordinal) ||
                                                        x.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T) ||
                            type.Name.Equals(nameof(IEnumerable), StringComparison.Ordinal) ||
                            type.SpecialType == SpecialType.System_Collections_IEnumerable;
            IsJsonDocument = IsReferenceType && type.Name.Equals(nameof(JsonDocument), StringComparison.Ordinal);
            IsJsonElement = IsValueType && type.Name.Equals(nameof(JsonElement), StringComparison.Ordinal);

            _enumUnderlyingType = IsEnum ? ((INamedTypeSymbol)Type).EnumUnderlyingType : null;

            if (IsArray)
            {
                var arrayType = (IArrayTypeSymbol)type;
                _arguments = new[] { Create(context, arrayType.ElementType) }.ToImmutableArray();
            }
            else if (type is INamedTypeSymbol nts && nts.TypeArguments.Length > 0)
            {
                _arguments = nts.TypeArguments.Select(typeSymbol => Create(context, typeSymbol)).ToImmutableArray();
            }
            else
            {
                _arguments = ImmutableArray<TypeSymbol>.Empty;
            }

            IsReadOnlyMemoryOfBytes = type.Name.Equals(nameof(ReadOnlyMemory<byte>), StringComparison.Ordinal) && _arguments.Length == 1 && _arguments[0].Type.SpecialType == SpecialType.System_Byte;
            IsRedisValue = type.Name.Equals(nameof(RedisValue), StringComparison.Ordinal);
            Converter = ConverterTypeSymbol.GetConverter(context, this);
            HasConverter = Converter != null;

            CastToRedisValue = type.SpecialType == SpecialType.System_Int32 ||
                               type.SpecialType == SpecialType.System_UInt32 ||
                               type.SpecialType == SpecialType.System_Int64 ||
                               type.SpecialType == SpecialType.System_UInt64 ||
                               type.SpecialType == SpecialType.System_Double ||
                               (type.Name.Equals(nameof(Memory<byte>), StringComparison.Ordinal) && _arguments.Length == 1 && _arguments[0].Type.SpecialType == SpecialType.System_Byte) ||
                               IsReadOnlyMemoryOfBytes ||
                               type.SpecialType == SpecialType.System_String ||
                               (IsArray && _arguments.Length == 1 && _arguments[0].Type.SpecialType == SpecialType.System_Byte) ||
                               type.SpecialType == SpecialType.System_Boolean;
            CastFromRedisValue = type.SpecialType == SpecialType.System_Boolean ||
                                 type.SpecialType == SpecialType.System_Int32 ||
                                 type.SpecialType == SpecialType.System_UInt32 ||
                                 type.SpecialType == SpecialType.System_Int64 ||
                                 type.SpecialType == SpecialType.System_UInt64 ||
                                 type.SpecialType == SpecialType.System_Double ||
                                 type.SpecialType == SpecialType.System_Decimal ||
                                 type.SpecialType == SpecialType.System_Single ||
                                 type.SpecialType == SpecialType.System_String ||
                                 IsReadOnlyMemoryOfBytes ||
                                 (IsArray && _arguments.Length == 1 && _arguments[0].Type.SpecialType == SpecialType.System_Byte);
        }

        public ISymbol? Symbol { get; }
        public ITypeSymbol Type { get; }

        public bool IsNullable { get; }

        public bool HasJsonTypeInfo { get; private set; }
        public bool HasUtf8JsonWriter { get; private set; }

        public bool IsBuiltinType =>
            (CastFromRedisValue && CastToRedisValue) ||
            IsRedisValue ||
            IsEnum;

        internal ConverterTypeSymbol? Converter { get; }

        public bool Equals(TypeSymbol other)
        {
            return SymbolEqualityComparer.Default.Equals(Symbol, other.Symbol) &&
                   SymbolEqualityComparer.Default.Equals(Type, other.Type);
        }

        public static TypeSymbol Create(SourceProductionContext context, ITypeSymbol typeSymbol)
        {
            if (typeSymbol is { IsValueType: true, IsUnmanagedType: true } && typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                if (!(typeSymbol is INamedTypeSymbol nts))
                    throw new NotSupportedException($"Cannot convert {typeSymbol} to {typeof(TypeSymbol)}");

                var genericType = nts.TypeArguments[0];
                return new TypeSymbol(context, genericType, null, true);
            }

            var mustShowNullableSign = TryGetNullableUnderlyingType(typeSymbol, out var underlyingTypeSymbol);
            return new TypeSymbol(context, mustShowNullableSign ? underlyingTypeSymbol! : typeSymbol, null, mustShowNullableSign);
        }

        public static TypeSymbol Create(SourceProductionContext context, ISymbol symbol)
        {
            var typeSymbol = GetTypeSymbol(symbol);
            if (typeSymbol is { IsValueType: true, IsUnmanagedType: true } && typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                if (!(typeSymbol is INamedTypeSymbol nts))
                    throw new NotSupportedException($"Cannot convert {typeSymbol} to {typeof(TypeSymbol)}");

                var genericType = nts.TypeArguments[0];
                return new TypeSymbol(context, genericType, symbol, true);
            }

            if (typeSymbol is null)
                throw new NotSupportedException($"Cannot convert {symbol} to {typeof(TypeSymbol)}");

            var mustShowNullableSign = TryGetNullableUnderlyingType(typeSymbol, out var underlyingTypeSymbol);
            return new TypeSymbol(context, mustShowNullableSign ? underlyingTypeSymbol! : typeSymbol, symbol, mustShowNullableSign);
        }

        private static ITypeSymbol? GetTypeSymbol(ISymbol symbol)
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

            if (_arguments.Length > 0)
                foreach (var argument in _arguments)
                    sb.Append(argument.GetDisplayName());

            if (IsArray) sb.Append("Array");

            return sb.ToString();
        }

        private static bool TryGetNullableUnderlyingType(ITypeSymbol typeSymbol, out ITypeSymbol? underlyingTypeSymbol)
        {
            if (typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                if (typeSymbol is INamedTypeSymbol { TypeArguments: { Length: 1 } } namedTypeSymbol)
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
            var propertyIdentifier = $"obj.{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            var declareLocalVariable = $@"{typeIdentifier} {valueIdentifier} = {propertyIdentifier};
                ";
            if (IsReadOnlyMemoryOfBytes)
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

            if (IsString || IsArray)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Length: > 0 }})
                {{
                    {content}
                }}";

            if (IsDictionary || IsCollection || IsList)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Count: > 0 }})
                {{
                    {content}
                }}";

            if (IsIEnumerable)
                return declareLocalVariable + $@"if ({valueIdentifier}.Any())
                {{
                    {content}
                }}";

            return declareLocalVariable + $@"if ({valueIdentifier} != {(IsNullable ? "null" : "default")})
                {{
                    {content}
                }}";
        }

        internal string? GetSetterContent(ContextOptions contextOptions, ISymbol propertySymbol, string serializerParameterName)
        {
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "pooledArray[++index] = ";

            if (HasConverter)
            {
                var hasDotValue = IsNullable && IsValueType;
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = {contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.GetBytes)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                    if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                    {{
                        {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                    }}";
            }

            if (CastFromRedisValue && CastToRedisValue)
            {
                if (IsValueType)
                    return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";
            }

            if (IsRedisValue)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (IsEnum)
                return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({_enumUnderlyingType}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (IsJsonElement)
            {
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(PooledJsonSerializer)}.{nameof(PooledJsonSerializer.GetBytes)}(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (IsJsonDocument)
            {
                HasUtf8JsonWriter = true;
                return @$"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(PooledJsonSerializer)}.{nameof(PooledJsonSerializer.GetBytes)}(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}.RootElement);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (IsValueType) // User-defined struct
            {
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(PooledJsonSerializer)}.{nameof(PooledJsonSerializer.GetBytes)}(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")}, {serializerParameterName});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (IsReferenceType) // User-defined class
            {
                HasJsonTypeInfo = true;
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)}){nameof(PooledJsonSerializer)}.{nameof(PooledJsonSerializer.GetBytes)}(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}, {serializerParameterName});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            return null;
        }

        internal string? GetGetterContent(ContextOptions contextOptions, ISymbol propertySymbol, string serializerParameterName)
        {
            var setter = $"value_{propertySymbol.Name} = ";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            if (HasConverter) return $@"{setter}{contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.Parse)}(entry.Value);";

            if (CastFromRedisValue && CastToRedisValue)
                return IsValueType ? $"{setter}({typeIdentifier})entry.Value;" : $@"{setter}({Type})entry.Value;";

            if (IsEnum) return $"{setter}({Type})({_enumUnderlyingType})entry.Value;";

            if (IsJsonElement) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonElement)}();";

            if (IsJsonDocument) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonDocument)}();";

            if (IsValueType) // User-defined struct
                return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>({serializerParameterName});";

            if (IsReferenceType) // User-defined class
                return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>({serializerParameterName});";

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

        public string GetSerializerContextTypeName()
        {
            if (IsArray)
            {
                return $"{_arguments[0].Type.Name}Array";
            }

            if (IsList)
            {
                return $"List{_arguments[0].Type.Name}";
            }

            if (IsDictionary)
            {
                return $"Dictionary{_arguments[0].Type}{_arguments[1].Type.Name}";
            }

            return Type.Name;
        }
    }
}