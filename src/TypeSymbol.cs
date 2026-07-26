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

        /// <summary>Element type when the value is <c>T[]</c> or <c>List&lt;T&gt;</c>, otherwise null.</summary>
        private readonly ITypeSymbol? _sequenceElementType;

        private readonly bool _isGenericList;
        private readonly bool _isStringStringDictionary;

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

            var namedType = type as INamedTypeSymbol;
            var genericNamespace = namedType?.ContainingNamespace?.ToDisplayString();
            _isGenericList = namedType != null &&
                             namedType.Name.Equals("List", StringComparison.Ordinal) &&
                             namedType.TypeArguments.Length == 1 &&
                             string.Equals(genericNamespace, "System.Collections.Generic", StringComparison.Ordinal);
            _isStringStringDictionary = namedType != null &&
                                        namedType.Name.Equals("Dictionary", StringComparison.Ordinal) &&
                                        namedType.TypeArguments.Length == 2 &&
                                        string.Equals(genericNamespace, "System.Collections.Generic", StringComparison.Ordinal) &&
                                        namedType.TypeArguments[0].SpecialType == SpecialType.System_String &&
                                        namedType.TypeArguments[1].SpecialType == SpecialType.System_String;

            if (IsArray)
                _sequenceElementType = ((IArrayTypeSymbol)type).ElementType;
            else if (_isGenericList)
                _sequenceElementType = namedType!.TypeArguments[0];

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

        /// <summary>
        ///     Indicates the property can be converted straight to a <see cref="RedisValue" /> without JSON serialization.
        /// </summary>
        public bool IsDirectWrite => HasConverter || (CastFromRedisValue && CastToRedisValue) || IsRedisValue || IsEnum;

        /// <summary>
        ///     Indicates the property is written by serializing its value as JSON.
        /// </summary>
        public bool IsJsonWrite => !IsDirectWrite;

        /// <summary>
        ///     Indicates the property benefits from a cached <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}" />.
        /// </summary>
        public bool RequiresJsonTypeInfo => IsJsonWrite && !IsJsonElement && !IsJsonDocument;

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

        /// <summary>
        ///     Returns the condition that determines whether the property value should be written,
        ///     or null when the value is always written (non-nullable value types).
        /// </summary>
        internal string? GetPresenceCondition(string valueIdentifier)
        {
            if (IsReadOnlyMemoryOfBytes)
                return $"{valueIdentifier}.Length > 0";

            if (IsJsonElement)
            {
                if (IsNullable)
                    return $"{valueIdentifier}.HasValue && {valueIdentifier}.Value.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.Value.ValueKind != JsonValueKind.Null";

                return $"{valueIdentifier}.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.ValueKind != JsonValueKind.Null";
            }

            if (IsJsonDocument)
                return $"{valueIdentifier} != {(IsNullable ? "null" : "default")} && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Null";

            if (IsValueType)
                return IsNullable ? $"{valueIdentifier}.HasValue" : null;

            if (IsString || IsArray)
                return $"{valueIdentifier} is {{ Length: > 0 }}";

            if (IsDictionary || IsCollection || IsList)
                return $"{valueIdentifier} is {{ Count: > 0 }}";

            if (IsIEnumerable)
                return $"{valueIdentifier}.Any()";

            return $"{valueIdentifier} != {(IsNullable ? "null" : "default")}";
        }

        /// <summary>
        ///     Builds the statement(s) that write a direct (non-JSON) property into the entries array
        ///     using a bounds-check-free <c>Unsafe.Add</c> write.
        /// </summary>
        internal string GetDirectWriteStatement(ContextOptions contextOptions)
        {
            var propertySymbol = Symbol!;
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "Unsafe.Add(ref entriesRef, index++) = ";

            if (HasConverter)
            {
                var hasDotValue = IsNullable && IsValueType;
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = {contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.GetBytes)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                {{
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                }}";
            }

            if (IsEnum)
                return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({_enumUnderlyingType}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            // Direct cast (numeric/string/bytes/bool) and RedisValue itself.
            return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable && IsValueType ? ".Value" : "")});";
        }

        /// <summary>
        ///     Builds the statement(s) that serialize a JSON property value into the shared Utf8JsonWriter.
        /// </summary>
        internal string GetJsonSerializeStatement(bool useSerializerContext)
        {
            var propertySymbol = Symbol!;
            var valueIdentifier = $"value_{propertySymbol.Name}{(IsNullable && IsValueType ? ".Value" : "")}";

            if (IsJsonElement)
                return $"{valueIdentifier}.WriteTo(utf8JsonWriter);";

            if (IsJsonDocument)
                return $"value_{propertySymbol.Name}.RootElement.WriteTo(utf8JsonWriter);";

            var fallback = useSerializerContext ? "serializerContext.Options" : "serializerOptions";
            return $@"if (typeInfoCache.TypeInfo_{propertySymbol.Name} != null)
                {{
                    JsonSerializer.Serialize(utf8JsonWriter, {valueIdentifier}, typeInfoCache.TypeInfo_{propertySymbol.Name});
                }}
                else
                {{
                    JsonSerializer.Serialize(utf8JsonWriter, {valueIdentifier}, {fallback});
                }}";
        }

        #region fast UTF-8 JSON writers

        private const string FastWriterType = "global::R8.RedisHashMap.JsonFastWriter";

        /// <summary>
        ///     Name of the <c>R8.RedisHashMap.FastJsonShape</c> member describing this value, or null when the value has
        ///     no hand-written UTF-8 emitter and must go through <c>System.Text.Json</c>.
        /// </summary>
        internal string? GetFastJsonShapeName()
        {
            if (_isStringStringDictionary)
                return "StringDictionary";

            if (_sequenceElementType == null)
                return null;

            if (_sequenceElementType.SpecialType == SpecialType.System_String)
                return "StringSequence";

            return GetIntegralWriterName(_sequenceElementType, out _) != null ? "NumberSequence" : null;
        }

        /// <summary>
        ///     Expression invoking the hand-written emitter; evaluates to false when the emitter cannot reproduce the
        ///     value byte-for-byte, in which case it leaves the buffer untouched for the System.Text.Json fallback.
        /// </summary>
        internal string? GetFastJsonWriteCall(string bufferWriterIdentifier, string valueIdentifier)
        {
            if (_isStringStringDictionary)
                return $"{FastWriterType}.TryWriteStringDictionary({bufferWriterIdentifier}, {valueIdentifier})";

            if (_sequenceElementType == null)
                return null;

            var elementName = _sequenceElementType.ToDisplayString();
            var span = _isGenericList
                ? $"(System.ReadOnlySpan<{elementName}>)System.Runtime.InteropServices.CollectionsMarshal.AsSpan({valueIdentifier})"
                : $"new System.ReadOnlySpan<{elementName}>({valueIdentifier})";

            if (_sequenceElementType.SpecialType == SpecialType.System_String)
                return $"{FastWriterType}.TryWriteStringSequence({bufferWriterIdentifier}, {span})";

            var writerName = GetIntegralWriterName(_sequenceElementType, out var keyword);
            if (writerName == null)
                return null;

            // Enums are reinterpreted as their underlying integral type; the spans are the same size and layout.
            var payload = _sequenceElementType.TypeKind == TypeKind.Enum
                ? $"System.Runtime.InteropServices.MemoryMarshal.Cast<{elementName}, {keyword}>({span})"
                : span;

            return $"{FastWriterType}.{writerName}({bufferWriterIdentifier}, {payload})";
        }

        /// <summary>
        ///     Expression producing the probe value that proves the emitter and System.Text.Json agree for this shape.
        /// </summary>
        internal string? GetFastJsonProbeExpression()
        {
            if (_isStringStringDictionary)
                return $"{FastWriterType}.ProbeStringDictionary";

            if (_sequenceElementType == null)
                return null;

            if (_sequenceElementType.SpecialType == SpecialType.System_String)
                return _isGenericList ? $"{FastWriterType}.ProbeStringList" : $"{FastWriterType}.ProbeStringArray";

            if (GetIntegralWriterName(_sequenceElementType, out _) == null)
                return null;

            var elementName = _sequenceElementType.ToDisplayString();
            return _isGenericList
                ? $"new System.Collections.Generic.List<{elementName}> {{ default({elementName}), ({elementName})1 }}"
                : $"new {elementName}[] {{ default({elementName}), ({elementName})1 }}";
        }

        private static string? GetIntegralWriterName(ITypeSymbol elementType, out string? keyword)
        {
            var specialType = elementType.TypeKind == TypeKind.Enum && elementType is INamedTypeSymbol namedEnum
                ? namedEnum.EnumUnderlyingType?.SpecialType ?? SpecialType.None
                : elementType.SpecialType;

            switch (specialType)
            {
                case SpecialType.System_SByte:
                    keyword = "sbyte";
                    return "TryWriteSByteSequence";
                case SpecialType.System_Byte:
                    keyword = "byte";
                    return "TryWriteByteSequence";
                case SpecialType.System_Int16:
                    keyword = "short";
                    return "TryWriteInt16Sequence";
                case SpecialType.System_UInt16:
                    keyword = "ushort";
                    return "TryWriteUInt16Sequence";
                case SpecialType.System_Int32:
                    keyword = "int";
                    return "TryWriteInt32Sequence";
                case SpecialType.System_UInt32:
                    keyword = "uint";
                    return "TryWriteUInt32Sequence";
                case SpecialType.System_Int64:
                    keyword = "long";
                    return "TryWriteInt64Sequence";
                case SpecialType.System_UInt64:
                    keyword = "ulong";
                    return "TryWriteUInt64Sequence";
                default:
                    keyword = null;
                    return null;
            }
        }

        #endregion

        /// <param name="typeInfoExpression">
        ///     Expression yielding the cached <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}" /> for
        ///     this property, so deserialization skips a metadata lookup per property per call. Null when unavailable.
        /// </param>
        internal string? GetGetterContent(ContextOptions contextOptions, ISymbol propertySymbol, string serializerParameterName, string? typeInfoExpression = null)
        {
            var setter = $"value_{propertySymbol.Name} = ";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            if (HasConverter) return $@"{setter}{contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.Parse)}(entry.Value);";

            if (CastFromRedisValue && CastToRedisValue)
                return IsValueType ? $"{setter}({typeIdentifier})entry.Value;" : $@"{setter}({Type})entry.Value;";

            if (IsEnum) return $"{setter}({Type})({_enumUnderlyingType})entry.Value;";

            if (IsJsonElement) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonElement)}();";

            if (IsJsonDocument) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonDocument)}();";

            if (IsValueType || IsReferenceType) // User-defined struct or class, parsed as JSON
            {
                if (typeInfoExpression == null)
                    return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>({serializerParameterName});";

                return $@"{setter}{typeInfoExpression} != null
                            ? entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>({typeInfoExpression})
                            : entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>({serializerParameterName});";
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