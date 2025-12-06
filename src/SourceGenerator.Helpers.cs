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
            var writeContentsWithSerializerOptions = BuildWriteContentsWithSerializerOptions(contextOptions, typeOptions.Properties);
            var writeContentsWithSerializerContext = BuildWriteContentsWithSerializerContext(contextOptions, typeOptions.Properties);
            return $@"
        /// <summary>
        /// Generates an array of <see cref=""HashEntry""/> from the current object's properties using the specified <see cref=""JsonSerializerContext""/>.
        /// </summary>
        /// <param name=""obj"">The {typeOptions.ObjectTypeSymbol} instance to serialize.</param>
        /// <param name=""serializerContext"">The <see cref=""JsonSerializerContext""/> used to serialize certain fields into JSON format.</param>
        /// <returns>An array of <see cref=""HashEntry""/> containing serialized representations of the object's properties.</returns>
        public HashEntry[] GetHashEntries({typeOptions.ObjectTypeSymbol} obj, JsonSerializerContext serializerContext)
        {{
            {(typeOptions.Properties.Any(c => c.HasUtf8JsonWriter) ? @"ArrayBufferWriter<byte>? arrayBufferWriter = null;
            Utf8JsonWriter? utf8JsonWriter = null;
            " : "")}int index = -1;
            HashEntry[] pooledArray = arrayPool.Rent({typeOptions.Properties.Count});

            try
            {{
                {writeContentsWithSerializerContext}
                
                if (index == -1)
                    return Array.Empty<HashEntry>();

                int finalCount = index + 1;
                HashEntry[] resultArray = new HashEntry[finalCount];
                Array.Copy(pooledArray, 0, resultArray, 0, finalCount);
                return resultArray;
            }}
            finally
            {{
                arrayPool.Return(pooledArray, clearArray: false);
            }}
        }}

        /// <summary>
        /// Converts the current instance of the {typeOptions.DisplayName} class into an array of <see cref=""HashEntry""/> objects.
        /// </summary>
        /// <param name=""obj"">The {typeOptions.ObjectTypeSymbol} instance to serialize.</param>
        /// <param name=""serializerOptions"">Optional <see cref=""JsonSerializerOptions""/> used for serializing the properties of the {typeOptions.ObjectTypeSymbol}.</param>
        /// <returns>An array of <see cref=""HashEntry""/> representing the fields and values of the {typeOptions.ObjectTypeSymbol} instance.</returns>
        public HashEntry[] GetHashEntries({typeOptions.ObjectTypeSymbol} obj, JsonSerializerOptions? serializerOptions = null)
        {{
            {(typeOptions.Properties.Any(c => c.HasUtf8JsonWriter) ? @"ArrayBufferWriter<byte>? arrayBufferWriter = null;
            Utf8JsonWriter? utf8JsonWriter = null;
            " : "")}int index = -1;
            HashEntry[] pooledArray = arrayPool.Rent({typeOptions.Properties.Count});

            try
            {{
                {writeContentsWithSerializerOptions}
                
                if (index == -1)
                    return Array.Empty<HashEntry>();

                int finalCount = index + 1;
                HashEntry[] resultArray = new HashEntry[finalCount];
                Array.Copy(pooledArray, 0, resultArray, 0, finalCount);
                return resultArray;
            }}
            finally
            {{
                arrayPool.Return(pooledArray, clearArray: false);
            }}
        }}
";
        }

        /// <summary>
        /// Builds the FromHashEntries method body for deserialization.
        /// </summary>
        private static string BuildFromHashEntries(ContextOptions contextOptions, ObjectOptions typeOptions)
        {
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
                        var wrapper = propertyType.GetGetterContent(contextOptions, propertyType.Symbol!, "serializerOptions");
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
                        var wrapper = propertyType.GetGetterContent(contextOptions, propertyType.Symbol!, "serializerContext");
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
        /// Builds the property setter contents using JsonSerializerOptions.
        /// </summary>
        private static string BuildWriteContentsWithSerializerOptions(ContextOptions contextOptions, IReadOnlyList<TypeSymbol> propertyTypes)
        {
            var stringBuilder = new StringBuilder();
            var propertyTypesLength = propertyTypes.Count;
            for (var index = 0; index < propertyTypesLength; index++)
            {
                var propertyType = propertyTypes[index];
                // var parser = GetParser(objectTypeSymbol, property);
                var propertySymbol = propertyType.Symbol;
                var content = propertyType.GetSetterContent(contextOptions, propertySymbol!, "serializerOptions");
                var wrapper = propertyType.GetSetterWrapper(propertySymbol!, content);
                if (wrapper != null)
                {
                    stringBuilder.Append($"{wrapper}\n");
                    if (index < propertyTypesLength - 1) stringBuilder.Append("\n                ");
                }
                else
                {
                    stringBuilder.AppendLine($"throw new NotSupportedException($\"Cannot convert `{propertyType.Type}` to `RedisValue` for `{propertySymbol}`.\");");
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Builds the property setter contents using JsonSerializerContext.
        /// </summary>
        private static string BuildWriteContentsWithSerializerContext(ContextOptions contextOptions, IReadOnlyList<TypeSymbol> propertyTypes)
        {
            var stringBuilder = new StringBuilder();
            var propertyTypesLength = propertyTypes.Count;
            for (var index = 0; index < propertyTypesLength; index++)
            {
                var propertyType = propertyTypes[index];
                var propertySymbol = propertyType.Symbol;
                // var serializerContextTypeName = propertyType.GetSerializerContextTypeName();
                var content = propertyType.GetSetterContent(contextOptions, propertySymbol!, $"serializerContext");
                var wrapper = propertyType.GetSetterWrapper(propertySymbol!, content);

                if (wrapper != null)
                {
                    stringBuilder.Append($"{wrapper}\n");
                    if (index < propertyTypesLength - 1) stringBuilder.Append("\n                ");
                }
                else
                {
                    stringBuilder.AppendLine($"throw new NotSupportedException($\"Cannot convert `{propertyType.Type}` to `RedisValue` for `{propertySymbol}`.\");");
                }
            }

            return stringBuilder.ToString();
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