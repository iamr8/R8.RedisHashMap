using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    public partial class SourceGenerator
    {
        /// <summary>
        /// Builds the GetHashEntries method body for serialization.
        /// </summary>
        private static string BuildGetHashEntries(ContextOptions contextOptions, ObjectOptions typeOptions)
        {
            return $@"
        /// <summary>
        /// Generates an array of <see cref=""HashEntry""/> from the current object's properties using the specified <see cref=""JsonSerializerContext""/>.
        /// </summary>
        /// <param name=""obj"">The {typeOptions.ObjectTypeSymbol} instance to serialize.</param>
        /// <param name=""serializerContext"">The <see cref=""JsonSerializerContext""/> used to serialize certain fields into JSON format.</param>
        /// <returns>An array of <see cref=""HashEntry""/> containing serialized representations of the object's properties.</returns>
        public HashEntry[] GetHashEntries({typeOptions.ObjectTypeSymbol} obj, JsonSerializerContext serializerContext)
        {{
{BuildGetHashEntriesBody(contextOptions, typeOptions, useSerializerContext: true)}
        }}

        /// <summary>
        /// Converts the current instance of the {typeOptions.DisplayName} class into an array of <see cref=""HashEntry""/> objects.
        /// </summary>
        /// <param name=""obj"">The {typeOptions.ObjectTypeSymbol} instance to serialize.</param>
        /// <param name=""serializerOptions"">Optional <see cref=""JsonSerializerOptions""/> used for serializing the properties of the {typeOptions.ObjectTypeSymbol}.</param>
        /// <returns>An array of <see cref=""HashEntry""/> representing the fields and values of the {typeOptions.ObjectTypeSymbol} instance.</returns>
        public HashEntry[] GetHashEntries({typeOptions.ObjectTypeSymbol} obj, JsonSerializerOptions? serializerOptions = null)
        {{
{BuildGetHashEntriesBody(contextOptions, typeOptions, useSerializerContext: false)}
        }}
";
        }

        /// <summary>
        /// Builds the body of a GetHashEntries overload. The layout is optimized to beat hand-written code:
        /// - exact-size result array written through Unsafe.Add (no ArrayPool, no bounds checks, no final copy when all properties are present)
        /// - all JSON properties are serialized back-to-back into one thread-static no-memset buffer, then copied once
        ///   into a single uninitialized byte[] that each RedisValue slices via ReadOnlyMemory (1 allocation for N properties)
        /// - JsonTypeInfo instances are resolved once per options/context instance and cached
        /// </summary>
        private static string BuildGetHashEntriesBody(ContextOptions contextOptions, ObjectOptions typeOptions, bool useSerializerContext)
        {
            var properties = typeOptions.Properties;
            var jsonProperties = properties.Where(p => p.IsJsonWrite).ToList();
            var serializerArgument = useSerializerContext ? "serializerContext" : "serializerOptions";

            var sb = new StringBuilder();
            sb.AppendLine($"            HashEntry[] entries = new HashEntry[{properties.Count}];");
            sb.AppendLine("            ref HashEntry entriesRef = ref MemoryMarshal.GetArrayDataReference(entries);");
            sb.AppendLine("            int index = 0;");
            sb.AppendLine();

            foreach (var property in properties)
            {
                if (!property.IsDirectWrite)
                    continue;

                var name = property.Symbol!.Name;
                var typeIdentifier = $"{property.Type}{(property.IsNullable ? "?" : "")}";
                var condition = property.GetPresenceCondition($"value_{name}");
                var statement = property.GetDirectWriteStatement(contextOptions);

                sb.AppendLine($"            {typeIdentifier} value_{name} = obj.{name};");
                if (condition != null)
                {
                    sb.AppendLine($"            if ({condition})");
                    sb.AppendLine("            {");
                    sb.AppendLine($"                {statement}");
                    sb.AppendLine("            }");
                }
                else
                {
                    sb.AppendLine($"            {statement}");
                }

                sb.AppendLine();
            }

            if (jsonProperties.Count > 0)
            {
                sb.AppendLine("            global::R8.RedisHashMap.PooledBufferWriter bufferWriter = null;");
                sb.AppendLine("            __JsonTypeInfoCache typeInfoCache = null;");
                sb.AppendLine();

                foreach (var property in jsonProperties)
                {
                    var name = property.Symbol!.Name;
                    var typeIdentifier = $"{property.Type}{(property.IsNullable ? "?" : "")}";
                    var condition = property.GetPresenceCondition($"value_{name}");
                    var serializeStatement = property.GetJsonSerializeStatement(useSerializerContext);
                    var fastWriteCall = property.RequiresJsonTypeInfo ? property.GetFastJsonWriteCall("bufferWriter", $"value_{name}") : null;
                    var indent = fastWriteCall != null ? "        " : "";

                    sb.AppendLine($"            int start_{name} = -1;");
                    sb.AppendLine($"            int length_{name} = 0;");
                    sb.AppendLine($"            {typeIdentifier} value_{name} = obj.{name};");
                    sb.AppendLine(condition != null ? $"            if ({condition})" : "            // Always present (non-nullable value type)");
                    sb.AppendLine("            {");
                    sb.AppendLine("                if (bufferWriter == null)");
                    sb.AppendLine("                {");
                    sb.AppendLine("                    bufferWriter = GetPooledBufferWriter();");
                    sb.AppendLine($"                    typeInfoCache = GetTypeInfoCache({serializerArgument});");
                    sb.AppendLine("                }");
                    sb.AppendLine();
                    sb.AppendLine($"                start_{name} = bufferWriter.WrittenCount;");

                    if (fastWriteCall != null)
                    {
                        // The hand-written emitter is attempted first; it rewinds the buffer and reports false when it
                        // cannot reproduce System.Text.Json byte-for-byte, so the fallback below stays correct.
                        sb.AppendLine($"                if (!(typeInfoCache.Fast_{name} && {fastWriteCall}))");
                        sb.AppendLine("                {");
                    }

                    sb.AppendLine($"{indent}                Utf8JsonWriter utf8JsonWriter = GetUtf8JsonWriter(bufferWriter, typeInfoCache);");
                    sb.AppendLine($"{indent}                {serializeStatement}");
                    sb.AppendLine($"{indent}                utf8JsonWriter.Flush();");

                    if (fastWriteCall != null)
                        sb.AppendLine("                }");

                    sb.AppendLine($"                length_{name} = bufferWriter.WrittenCount - start_{name};");
                    sb.AppendLine("            }");
                    sb.AppendLine();
                }

                sb.AppendLine("            if (bufferWriter != null && bufferWriter.WrittenCount > 0)");
                sb.AppendLine("            {");
                sb.AppendLine("                byte[] jsonBlob = bufferWriter.ToArrayAndReset();");
                // Re-taken here so that no interior pointer stays live across the serialization calls above,
                // which would otherwise pin a GC-tracked slot for the whole method.
                sb.AppendLine("                entriesRef = ref MemoryMarshal.GetArrayDataReference(entries);");
                foreach (var property in jsonProperties)
                {
                    var name = property.Symbol!.Name;
                    sb.AppendLine($"                if (start_{name} >= 0)");
                    sb.AppendLine("                {");
                    sb.AppendLine($"                    Unsafe.Add(ref entriesRef, index++) = new HashEntry(field_{name}, (RedisValue)new ReadOnlyMemory<byte>(jsonBlob, start_{name}, length_{name}));");
                    sb.AppendLine("                }");
                }

                sb.AppendLine("            }");
                sb.AppendLine();
            }

            sb.AppendLine($"            if (index == {properties.Count})");
            sb.AppendLine("                return entries;");
            sb.AppendLine();
            sb.AppendLine("            if (index == 0)");
            sb.AppendLine("                return Array.Empty<HashEntry>();");
            sb.AppendLine();
            sb.AppendLine("            HashEntry[] result = new HashEntry[index];");
            sb.AppendLine("            Array.Copy(entries, 0, result, 0, index);");
            sb.Append("            return result;");

            return sb.ToString();
        }

        /// <summary>
        /// Builds the per-helper JsonTypeInfo cache: metadata is resolved once per options/context instance
        /// (a single ReferenceEquals check per call afterwards) instead of a GetTypeInfo lookup per property per object.
        /// </summary>
        private static string BuildTypeInfoCache(ObjectOptions typeOptions)
        {
            var cachedProperties = typeOptions.Properties.Where(p => p.RequiresJsonTypeInfo).ToList();
            var fastProperties = cachedProperties.Where(p => p.GetFastJsonShapeName() != null).ToList();

            var fields = string.Join(@"
            ", cachedProperties.Select(p => $"public JsonTypeInfo<{p.Type}> TypeInfo_{p.Symbol!.Name};")
                .Concat(fastProperties.Select(p => $"public bool Fast_{p.Symbol!.Name};")));
            var optionsInitializers = string.Join(@"
                    ", cachedProperties.Select(p => $"TypeInfo_{p.Symbol!.Name} = global::R8.RedisHashMap.PooledJsonSerializer.GetTypeInfoOrNull<{p.Type}>(options),")
                .Prepend("WriterOptions = global::R8.RedisHashMap.PooledJsonSerializer.CreateWriterOptions(options),"));
            var contextInitializers = string.Join(@"
                    ", cachedProperties.Select(p => $"TypeInfo_{p.Symbol!.Name} = serializerContext.GetTypeInfo(typeof({p.Type})) as JsonTypeInfo<{p.Type}>,")
                .Prepend("WriterOptions = global::R8.RedisHashMap.PooledJsonSerializer.CreateWriterOptions(serializerContext.Options),"));

            // Proving the hand-written emitters byte-for-byte equal to System.Text.Json is done once per options
            // instance; a divergent encoder, naming policy, number handling or converter simply disables the fast path.
            var fastValidations = string.Join(@"
                ", fastProperties.Select(p =>
                $"cache.Fast_{p.Symbol!.Name} = global::R8.RedisHashMap.JsonFastWriter.Validate(global::R8.RedisHashMap.FastJsonShape.{p.GetFastJsonShapeName()}, {p.GetFastJsonProbeExpression()}, cache.TypeInfo_{p.Symbol!.Name}, options);"));

            var optionsValidations = fastProperties.Count == 0
                ? ""
                : $@"
                {fastValidations}";
            var contextValidations = fastProperties.Count == 0
                ? ""
                : $@"
                JsonSerializerOptions options = serializerContext.Options;
                {fastValidations}";

            return $@"
        private sealed class __JsonTypeInfoCache
        {{
            public object Key;
            public JsonWriterOptions WriterOptions;
            {fields}
        }}

        private __JsonTypeInfoCache _optionsTypeInfoCache;
        private __JsonTypeInfoCache _contextTypeInfoCache;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private __JsonTypeInfoCache GetTypeInfoCache(JsonSerializerOptions? serializerOptions)
        {{
            JsonSerializerOptions options = serializerOptions ?? JsonSerializerOptions.Default;
            __JsonTypeInfoCache cache = _optionsTypeInfoCache;
            return cache != null && object.ReferenceEquals(cache.Key, options) ? cache : BuildTypeInfoCache(options);
        }}

        [MethodImpl(MethodImplOptions.NoInlining)]
        private __JsonTypeInfoCache BuildTypeInfoCache(JsonSerializerOptions options)
        {{
            __JsonTypeInfoCache cache = new __JsonTypeInfoCache
            {{
                Key = options,
                {optionsInitializers}
            }};{optionsValidations}
            _optionsTypeInfoCache = cache;
            return cache;
        }}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private __JsonTypeInfoCache GetTypeInfoCache(JsonSerializerContext serializerContext)
        {{
            __JsonTypeInfoCache cache = _contextTypeInfoCache;
            return cache != null && object.ReferenceEquals(cache.Key, serializerContext) ? cache : BuildTypeInfoCache(serializerContext);
        }}

        [MethodImpl(MethodImplOptions.NoInlining)]
        private __JsonTypeInfoCache BuildTypeInfoCache(JsonSerializerContext serializerContext)
        {{
            __JsonTypeInfoCache cache = new __JsonTypeInfoCache
            {{
                Key = serializerContext,
                {contextInitializers}
            }};{contextValidations}
            _contextTypeInfoCache = cache;
            return cache;
        }}";
        }

        /// <summary>
        /// Builds the FromHashEntries method body for deserialization.
        /// </summary>
        private static string BuildFromHashEntries(ContextOptions contextOptions, ObjectOptions typeOptions)
        {
            // Deserialization resolves each property's metadata once per options/context instance too, instead of
            // paying a JsonTypeInfo lookup for every property of every entry array.
            var usesTypeInfoCache = typeOptions.Properties.Exists(p => p.RequiresJsonTypeInfo);
            string? ReadTypeInfoExpression(TypeSymbol property) =>
                usesTypeInfoCache && property.RequiresJsonTypeInfo ? $"typeInfoCache.TypeInfo_{property.Symbol!.Name}" : null;

            var optionsCacheLookup = usesTypeInfoCache ? "__JsonTypeInfoCache typeInfoCache = GetTypeInfoCache(serializerOptions);" : "";
            var contextCacheLookup = usesTypeInfoCache ? "__JsonTypeInfoCache typeInfoCache = GetTypeInfoCache(serializerContext);" : "";

            return $@"/// <summary>
        /// Initializes a new instance of the <see cref=""{typeOptions.ObjectTypeSymbol}""/> class by mapping the given hash entries to its properties.
        /// </summary>
        /// <param name=""entries"">An array of <see cref=""HashEntry""/> containing the data used to populate the <see cref=""{typeOptions.ObjectTypeSymbol}""/>.</param>
        /// <param name=""serializerOptions"">Optional <see cref=""JsonSerializerOptions""/> used for deserializing certain fields.</param>
        /// <returns>A <see cref=""{typeOptions.ObjectTypeSymbol}""/> instance populated with values from the hash entries.</returns>
        public {typeOptions.ObjectTypeSymbol}{(typeOptions.ObjectTypeSymbol.IsNullable ? "?" : "")} FromHashEntries(HashEntry[] entries, JsonSerializerOptions? serializerOptions = null)
        {{
            var length = entries.Length;
            if (length == 0)
                return {(typeOptions.ObjectTypeSymbol.IsNullable ? "null" : "default")};

            {typeOptions.ObjectTypeSymbol}{(typeOptions.ObjectTypeSymbol.IsNullable ? "?" : "")} obj;
            {optionsCacheLookup}
            {string.Join(@"
            ", typeOptions.Properties.Select(c => $"{c.Type}{(c.IsNullable ? "?" : "")} value_{c.Symbol!.Name} = {(c.IsNullable ? "null" : "default")};"))}
            
            for (int i = 0; i < length; i++)
            {{
                HashEntry entry = entries[i];
                RedisValue name = entry.Name;
                if (name.IsNullOrEmpty)
                    continue;

                switch (name)
                {{
                    {string.Join(@"
                    ", typeOptions.Properties.Select(propertyType => {
                        var wrapper = propertyType.GetGetterContent(contextOptions, propertyType.Symbol!, "serializerOptions", ReadTypeInfoExpression(propertyType));
                        return $@"case prop_{propertyType.Symbol!.Name}: {{ {wrapper ?? $"throw new NotSupportedException($\"Cannot convert `{propertyType.Type}` to `RedisValue` for `{propertyType.Symbol}`.\");"} break; }}";
                    }))}
                }}
            }}

            obj = new {typeOptions.ObjectTypeSymbol}
            {{
                {string.Join(@"
                ", typeOptions.Properties.Select(c => $"{c.Symbol!.Name} = value_{c.Symbol!.Name},"))}
            }};

            return obj;
        }}

        /// <summary>
        /// Creates a new instance of the <see cref=""{typeOptions.ObjectTypeSymbol}""/> class by mapping the given hash entries
        /// to its properties using the specified serializer context.
        /// </summary>
        /// <param name=""entries"">An array of <see cref=""HashEntry""/> containing the data to populate the <see cref=""{typeOptions.ObjectTypeSymbol}""/> instance.</param>
        /// <param name=""serializerContext"">The <see cref=""JsonSerializerContext""/> used for deserialization of certain fields requiring type information.</param>
        /// <returns>A <see cref=""{typeOptions.ObjectTypeSymbol}""/> instance populated with values from the hash entries.</returns>
        public {typeOptions.ObjectTypeSymbol}{(typeOptions.ObjectTypeSymbol.IsNullable ? "?" : "")} FromHashEntries(HashEntry[] entries, JsonSerializerContext serializerContext)
        {{
            var length = entries.Length;
            if (length == 0)
                return {(typeOptions.ObjectTypeSymbol.IsNullable ? "null" : "default")};

            {typeOptions.ObjectTypeSymbol}{(typeOptions.ObjectTypeSymbol.IsNullable ? "?" : "")} obj;
            {contextCacheLookup}
            {string.Join(@"
            ", typeOptions.Properties.Select(c => $"{c.Type}{(c.IsNullable ? "?" : "")} value_{c.Symbol!.Name} = {(c.IsNullable ? "null" : "default")};"))}
            
            for (int i = 0; i < length; i++)
            {{
                HashEntry entry = entries[i];
                RedisValue name = entry.Name;
                if (name.IsNullOrEmpty)
                    continue;

                switch (name)
                {{
                    {string.Join(@"
                    ", typeOptions.Properties.Select(propertyType => {
                        var wrapper = propertyType.GetGetterContent(contextOptions, propertyType.Symbol!, "serializerContext", ReadTypeInfoExpression(propertyType));
                        return $@"case prop_{propertyType.Symbol!.Name}: {{ {wrapper ?? $"throw new NotSupportedException($\"Cannot convert `{propertyType.Type}` to `RedisValue` for `{propertyType.Symbol}`.\");"} break; }}";
                    }))}
                }}
            }}

            obj = new {typeOptions.ObjectTypeSymbol}
            {{
                {string.Join(@"
                ", typeOptions.Properties.Select(c => $"{c.Symbol!.Name} = value_{c.Symbol!.Name},"))}
            }};

            return obj;
        }}";
        }

        /// <summary>
        /// Builds the property field constants and RedisValue fields for property names.
        /// </summary>
        private static string BuildPropertyFields(ContextOptions contextOptions, ObjectOptions typeOptions)
        {
            var stringBuilder = new StringBuilder();
            for (var index = 0; index < typeOptions.Properties.Count; index++)
            {
                var propertyType = typeOptions.Properties[index];
                var namingStrategy = contextOptions.NamingStrategy switch
                {
                    CacheFieldNamingStrategy.CamelCase => propertyType.Symbol!.Name.ToCamelCase(),
                    CacheFieldNamingStrategy.SnakeCase => propertyType.Symbol!.Name.ToSnakeCase(),
                    CacheFieldNamingStrategy.PascalCase => propertyType.Symbol!.Name,
                    _ => throw new System.NotSupportedException($"Unsupported naming strategy: {contextOptions.NamingStrategy}")
                };
                stringBuilder.Append($"private const string prop_{propertyType.Symbol!.Name} = \"{namingStrategy}\";\n\t\t");
                stringBuilder.Append($"private static readonly RedisValue field_{propertyType.Symbol!.Name} = new RedisValue(prop_{propertyType.Symbol!.Name});");

                if (index < typeOptions.Properties.Count - 1) stringBuilder.Append("\n\n\t\t");
            }

            return stringBuilder.ToString();
        }
    }
}