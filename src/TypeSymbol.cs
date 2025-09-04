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
        private readonly bool _isArray;
        private readonly bool _isBytesArray;
        private readonly bool _isCollection;
        private readonly bool _isDictionary;
        private readonly bool _isEnum;
        private readonly bool _isJsonDocument;
        private readonly bool _isJsonElement;
        private readonly bool _isList;

        private readonly bool _isPrimitiveType;
        private readonly bool _isReadOnlyMemory;
        private readonly bool _isReferenceType;

        private readonly bool _isString;
        private readonly bool _isValueType;
        private readonly bool _hasConverter;

        private TypeSymbol(SourceProductionContext context, ITypeSymbol type, ISymbol? symbol, bool isNullable)
        {
            Type = type;
            Symbol = symbol;

            _isValueType = type.IsValueType;
            _isPrimitiveType = _isValueType && (type.SpecialType == SpecialType.System_Boolean ||
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
            _isEnum = _isValueType && type.TypeKind == TypeKind.Enum;
            _isReferenceType = type.IsReferenceType;
            _isString = _isReferenceType && type.SpecialType == SpecialType.System_String;
            IsNullable = isNullable;
            _isArray = type is IArrayTypeSymbol;
            _isCollection = type.AllInterfaces.Any(x => x.Name.Equals(nameof(ICollection), StringComparison.Ordinal) ||
                                                        x.SpecialType == SpecialType.System_Collections_Generic_ICollection_T);
            _isList = type.AllInterfaces.Any(x => x.Name.Equals(nameof(IList), StringComparison.Ordinal) ||
                                                  x.SpecialType == SpecialType.System_Collections_Generic_IList_T);
            _isDictionary = type.AllInterfaces.Any(x => x.Name.Equals(nameof(IDictionary), StringComparison.Ordinal));
            _isJsonDocument = _isReferenceType && type.Name.Equals(nameof(JsonDocument), StringComparison.Ordinal);
            _isJsonElement = _isValueType && type.Name.Equals(nameof(JsonElement), StringComparison.Ordinal);

            _enumUnderlyingType = _isEnum ? ((INamedTypeSymbol)Type).EnumUnderlyingType : null;

            if (_isArray)
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

            _isReadOnlyMemory = type.Name.Equals("ReadOnlyMemory", StringComparison.Ordinal) && _arguments.Length > 0;
            _isBytesArray = _isArray && _arguments.Length == 1 && _arguments[0].Type.SpecialType == SpecialType.System_Byte;
            Converter = ConverterTypeSymbol.GetConverter(context, this);
            _hasConverter = Converter != null;
        }

        public ISymbol? Symbol { get; }
        public ITypeSymbol Type { get; }

        public bool IsNullable { get; }

        public bool HasJsonTypeInfo { get; private set; }
        public bool HasUtf8JsonWriter { get; private set; }

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

            if (_isArray) sb.Append("Array");

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
            if (_isReadOnlyMemory)
                return declareLocalVariable + $@"if ({valueIdentifier}.Length > 0)
                {{
                    {content}
                }}";

            if (_isJsonElement)
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

            if (_isJsonDocument)
                return declareLocalVariable + @$"if ({valueIdentifier} != {(IsNullable ? "null" : "default")} && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Undefined && {valueIdentifier}.RootElement.ValueKind != JsonValueKind.Null)
                {{
                    {content}
                }}";

            if (_isValueType)
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

            if (_isString || _isArray)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Length: > 0 }})
                {{
                    {content}
                }}";

            if (_isDictionary || _isCollection || _isList)
                return declareLocalVariable + $@"if ({valueIdentifier} is {{ Count: > 0 }})
                {{
                    {content}
                }}";

            return declareLocalVariable + $@"if ({valueIdentifier} != {(IsNullable ? "null" : "default")})
                {{
                    {content}
                }}";
        }

        internal string? GetSetterContentWithSerializerOptions(ContextOptions contextOptions, ISymbol propertySymbol)
        {
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "entries[++index] = ";

            if (_hasConverter)
            {
                var hasDotValue = IsNullable && _isValueType;
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = {contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.GetBytes)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                    if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                    {{
                        {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                    }}";
            }

            if (_isPrimitiveType)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (_isEnum)
                return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({_enumUnderlyingType}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (_isReadOnlyMemory)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (_isJsonElement)
            {
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (_isJsonDocument)
            {
                HasUtf8JsonWriter = true;
                return @$"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}.RootElement);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (_isValueType) // User-defined struct
            {
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")}, serializerOptions);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (_isString || _isBytesArray)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (_isReferenceType) // User-defined class
            {
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}, serializerOptions);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            return null;
        }

        internal string? GetSetterContentWithSerializerContext(ContextOptions contextOptions, ISymbol propertySymbol)
        {
            var fieldIdentifier = $"field_{propertySymbol.Name}";
            var valueIdentifier = $"value_{propertySymbol.Name}";
            const string setter = "entries[++index] = ";

            if (_hasConverter)
            {
                var hasDotValue = IsNullable && _isValueType;
                return $@"{nameof(RedisValue)} redis_{propertySymbol.Name} = {contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.GetBytes)}({valueIdentifier}{(hasDotValue ? ".Value" : "")});
                    if (!redis_{propertySymbol.Name}.{nameof(RedisValue.IsNullOrEmpty)})
                    {{
                        {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});
                    }}";
            }

            if (_isPrimitiveType)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (_isEnum)
                return $"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)})({_enumUnderlyingType}){valueIdentifier}{(IsNullable ? ".Value" : "")});";

            if (_isReadOnlyMemory)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (_isJsonElement)
            {
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")});
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (_isJsonDocument)
            {
                HasUtf8JsonWriter = true;
                return @$"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}.RootElement);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (_isValueType) // User-defined struct
            {
                HasJsonTypeInfo = true;
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}{(IsNullable ? ".Value" : "")}, serializerContext);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            if (_isString || _isBytesArray)
                return $@"{setter}new {nameof(HashEntry)}({fieldIdentifier}, ({nameof(RedisValue)}){valueIdentifier});";

            if (_isReferenceType) // User-defined class
            {
                HasJsonTypeInfo = true;
                HasUtf8JsonWriter = true;
                return $@"arrayBufferWriter ??= GetArrayBufferWriter();
                    utf8JsonWriter ??= GetUtf8JsonWriter(arrayBufferWriter);
                    {nameof(RedisValue)} redis_{propertySymbol.Name} = ({nameof(RedisValue)})GetBytes(arrayBufferWriter, utf8JsonWriter, {valueIdentifier}, serializerContext);
                    {setter}new {nameof(HashEntry)}({fieldIdentifier}, redis_{propertySymbol.Name});";
            }

            return null;
        }

        internal string? GetGetterContentWithSerializerOptions(ContextOptions contextOptions, ISymbol propertySymbol)
        {
            var setter = $"value_{propertySymbol.Name} = ";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            if (_hasConverter) return $@"{setter}{contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.Parse)}(entry.Value);";

            if (_isPrimitiveType) return $@"{setter}({typeIdentifier})entry.Value;";

            if (_isEnum) return $"{setter}({Type})({_enumUnderlyingType})entry.Value;";

            if (_isReadOnlyMemory) return $@"{setter}({nameof(ReadOnlyMemory<byte>)}<byte>)entry.Value;";

            if (_isJsonElement) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonElement)}();";

            if (_isJsonDocument) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonDocument)}();";

            if (_isValueType) // User-defined struct
                return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>(serializerOptions);";

            if (_isString) return $@"{setter}(string{(IsNullable ? "?" : "")})entry.Value;";

            if (_isBytesArray) return $@"{setter}(byte[])entry.Value;";

            if (_isReferenceType) // User-defined class
                return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>(serializerOptions);";

            return null;
        }

        internal string? GetGetterContentWithSerializerContext(ContextOptions contextOptions, ISymbol propertySymbol)
        {
            var setter = $"value_{propertySymbol.Name} = ";
            var typeIdentifier = $"{Type}{(IsNullable ? "?" : "")}";

            if (_hasConverter) return $@"{setter}{contextOptions.DisplayName}.Default.{Converter!.ConverterName}.{nameof(CacheValueConverter<string>.Parse)}(entry.Value);";

            if (_isPrimitiveType) return $@"{setter}({typeIdentifier})entry.Value;";

            if (_isEnum) return $"{setter}({Type})({_enumUnderlyingType})entry.Value;";

            if (_isReadOnlyMemory) return $@"{setter}({nameof(ReadOnlyMemory<byte>)}<byte>)entry.Value;";

            if (_isJsonElement) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonElement)}();";

            if (_isJsonDocument) return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.GetJsonDocument)}();";

            if (_isValueType) // User-defined struct
            {
                HasJsonTypeInfo = true;
                return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>(serializerContext);";
            }

            if (_isString) return $@"{setter}(string{(IsNullable ? "?" : "")})entry.Value;";

            if (_isBytesArray) return $@"{setter}(byte[])entry.Value;";

            if (_isReferenceType) // User-defined class
            {
                HasJsonTypeInfo = true;
                return $@"{setter}entry.Value.{nameof(PooledJsonSerializer.Parse)}<{Type}>(serializerContext);";
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
    }
}