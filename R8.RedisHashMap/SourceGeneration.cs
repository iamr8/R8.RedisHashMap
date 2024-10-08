﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace R8.RedisHashMap
{
    [Generator]
    public class SourceGeneration : IIncrementalGenerator
    {
        // private static readonly List<string> GeneratedTypes = new List<string>();

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // register a syntax receiver that will create source for any class that implements IRedisCacheModel
            var declares = context.SyntaxProvider
                .CreateSyntaxProvider(Predicate, Transform);

            var provider = context.CompilationProvider.Combine(declares.Collect());

            context.RegisterSourceOutput(provider, Execute);
        }

        private void Execute(SourceProductionContext ctx, (Compilation Left, ImmutableArray<(ClassDeclarationSyntax Class, ISymbol Symbol, Dictionary<string, object> Attributes, List<ITypeSymbol> Types)?> Right) tuple)
        {
            foreach (var right in tuple.Right)
            {
                if (right is null)
                    continue;

                var (classDeclaration, contextSymbol, attrProps, types) = right.Value;
                if (types.Count == 0)
                    continue;

                attrProps.TryAdd(nameof(CacheableOptionsAttribute.IncludeFields), false);
                attrProps.TryAdd(nameof(CacheableOptionsAttribute.IncludePrivate), false);

                var typeFullQualifiedName = contextSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                // GenerateCacheableContext(ctx, typeFullQualifiedName, contextSymbol);
                GenerateGetCacheableTypeInfo(ctx, typeFullQualifiedName, contextSymbol, types);
                foreach (var typeSymbol in types)
                {
                    var props = GetPropertySymbols(typeSymbol, attrProps);
                    if (props.Length == 0)
                        continue;

                    GenerateModel(ctx, typeFullQualifiedName, contextSymbol, typeSymbol, props);
                }
            }
        }

        private static void GenerateGetCacheableTypeInfo(SourceProductionContext ctx, string typeFullQualifiedName, ISymbol contextSymbol, IList<ITypeSymbol> types)
        {
            var sourceText = SourceText.From($@"// Original source: {typeFullQualifiedName}
// <auto-generated/>
#nullable enable

namespace {contextSymbol.ContainingNamespace}
{{
    public partial class {contextSymbol.Name} : ICacheableContext
    {{
        CacheableTypeInfo? ICacheableContext.GetTypeInfo(Type type)
        {{
            {string.Join(@"
            ", types.Select(type => $@"if (type == typeof({type}))
            {{
                return CreateCacheable_{GetDisplayName(type)}();
            }}"))}

            return null;
        }}
    }}
}}", Encoding.UTF8);

            var fileName = $"{contextSymbol.Name}.GetCacheableTypeInfo.g.cs";

            ctx.AddSource(fileName, sourceText);
        }

        private static void GenerateModel(SourceProductionContext ctx, string typeFullQualifiedName, ISymbol contextSymbol, ITypeSymbol typeSymbol, ISymbol[] includingSymbols)
        {
            var isEnumerable = IsAssignedToIEnumerable(typeSymbol);
            var displayName = GetDisplayName(typeSymbol);
            var sourceText = SourceText.From($@"// Original source: {typeFullQualifiedName}
// <auto-generated/>
#nullable enable

namespace {contextSymbol.ContainingNamespace}
{{
    public partial class {contextSymbol.Name}
    {{
        private CacheableTypeInfo<{typeSymbol}>? _{displayName}Cacheable;

        public CacheableTypeInfo<{typeSymbol}> {displayName}Cacheable
        {{
            get => _{displayName}Cacheable ??= (CacheableTypeInfo<{typeSymbol}>)((ICacheableContext)this).GetTypeInfo(typeof({typeSymbol}));
        }}

        private global::R8.RedisHashMap.CacheableTypeInfo<{typeSymbol}> CreateCacheable_{displayName}()
        {{
            var typeInfo = new global::R8.RedisHashMap.CacheableTypeInfo<{typeSymbol}>
            {{
                ObjectCreator = () => new {typeSymbol}(),
                PropertyMetadataInitializer = _ => Cacheable{displayName}PropInit(),
                OriginatingResolver = this
            }};

            return typeInfo;
        }}

        private global::System.ReadOnlyMemory<CacheablePropertyInfo> Cacheable{displayName}PropInit()
        {{
            var jsonTypeInfoResolver = this as global::System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver;
            {GetCacheablePropertyInfos(contextSymbol, typeSymbol, includingSymbols, isEnumerable)}
            return properties;
        }}
    }}
}}", Encoding.UTF8);

            var fileName = $"{contextSymbol.Name}.{displayName}.g.cs";

            ctx.AddSource(fileName, sourceText);
        }

        private static string GetCacheablePropertyInfos(ISymbol contextSymbol, ITypeSymbol typeSymbol, ISymbol[] includingSymbols, bool isEnumerable)
        {
            if (isEnumerable)
            {
                if (typeSymbol is INamedTypeSymbol nts)
                {
                    if (nts.TypeArguments.Length > 0)
                    {
                        return @$"global::System.Memory<CacheablePropertyInfo> properties = new CacheablePropertyInfo[{nts.TypeArguments.Length}];

            {string.Join(@$"
            ", nts.TypeArguments.Select((type, i) =>
            {
                var isNullable = TryGetNullableUnderlyingType(type, out var underlyingType);
                var defaultCreator = GetDefaultCreator(type);
                var generator = GetGenerator(contextSymbol, type, isNullable);
                var parser = GetParser(contextSymbol, type);

                return GetArgumentCacheablePropertyInfo(typeSymbol, type, i, defaultCreator, generator, parser);
            }))}
            ";
                    }
                }
            }

            return @$"global::System.Memory<CacheablePropertyInfo> properties = new CacheablePropertyInfo[{includingSymbols.Length}];

            {string.Join(@$"
            ", includingSymbols.Select((symbol, i) =>
            {
                var type = GetTypeSymbol(symbol);
                var isNullable = TryGetNullableUnderlyingType(type, out var underlyingType);
                var defaultCreator = GetDefaultCreator(type);
                var generator = GetGenerator(contextSymbol, type, isNullable);
                var parser = GetParser(contextSymbol, type);

                return GetCacheablePropertyInfo(typeSymbol, symbol, type, i, defaultCreator, generator, parser, underlyingType);
            }))}
            ";
        }

        private static string GetArgumentCacheablePropertyInfo(ITypeSymbol objectTypeSymbol, ITypeSymbol? argumentTypeSymbol, int index, string defaultCreator, string generator, string parser)
        {
            return $@"var info{index} = new CacheablePropertyInfoValues<{argumentTypeSymbol}>
            {{
                DeclaringType = typeof({objectTypeSymbol}),
                DefaultCreator = {defaultCreator},
                Converter = null,
                Generator = {generator},
                Parser = {parser},
                ValueType = typeof({argumentTypeSymbol}),
            }};

            properties.Span[{index}] = CacheableMetadataServices.CreatePropertyInfo<{argumentTypeSymbol}>(info{index});
";
        }

        private static string GetCacheablePropertyInfo(ITypeSymbol objectTypeSymbol, ISymbol argumentSymbol, ITypeSymbol argumentTypeSymbol, int index, string defaultCreator, string generator, string parser, ITypeSymbol underlyingType)
        {
            return $@"var info{index} = new CacheablePropertyInfoValues<{argumentTypeSymbol}>
            {{
                DeclaringType = typeof({objectTypeSymbol}),
                DefaultCreator = {defaultCreator},
                Converter = null,
                Getter = obj => (({objectTypeSymbol})obj).{argumentSymbol.Name},
                Setter = (obj, value) => (({objectTypeSymbol})obj).{argumentSymbol.Name} = value!,
                Generator = {generator},
                Parser = {parser},
                ValueType = typeof({underlyingType}),
                PropertyName = ""{argumentSymbol.Name}""
            }};

            properties.Span[{index}] = CacheableMetadataServices.CreatePropertyInfo<{argumentTypeSymbol}>(info{index});
";
        }

        private static string GetParser(ISymbol contextSymbol, ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsValueType)
            {
                if (typeSymbol.IsUnmanagedType)
                {
                    if (typeSymbol.TypeKind == TypeKind.Enum)
                    {
                        var underlyingType = (INamedTypeSymbol)typeSymbol;
                        var enumType = underlyingType!.EnumUnderlyingType;
                        return $"value => ({underlyingType})({enumType})value";
                    }
                    else if (typeSymbol.Name.Equals("Nullable", StringComparison.Ordinal))
                    {
                        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            if (namedTypeSymbol.TypeArguments.Length == 1)
                            {
                                var genericType = namedTypeSymbol.TypeArguments[0];
                                return GetParser(contextSymbol, genericType);
                            }
                        }
                        else
                        {
                            return $"value => throw new global::System.NotSupportedException($\"AAA Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
                        }
                    }
                    else
                    {
                        return $"value => ({typeSymbol})value";
                    }
                }
                else
                {
                    return $"() => throw new global::System.NotSupportedException($\"000 Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
                }
            }
            else if (typeSymbol.IsReferenceType)
            {
                if (typeSymbol.SpecialType == SpecialType.System_String)
                {
                    return $"value => ({typeSymbol})value";
                }
                else if (typeSymbol.Name.Equals(nameof(JsonDocument), StringComparison.Ordinal))
                {
                    return $"value => global::System.Text.Json.JsonDocument.Parse((ReadOnlyMemory<byte>)value)";
                }
                else
                {
                    var underlyingType = TryGetNullableUnderlyingType(typeSymbol, out var underlying) ? underlying : typeSymbol;
                    var jsonSourceGeneratedTypeInfo = $"{contextSymbol.Name}.Default.{GetDisplayName(typeSymbol)}";
                    return $"value => global::System.Text.Json.JsonSerializer.Deserialize<{underlyingType}>(((global::System.ReadOnlyMemory<byte>)value).Span, {jsonSourceGeneratedTypeInfo})!";
                }
            }

            return $"() => throw new global::System.NotSupportedException($\"CCC Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
        }

        private static string GetGenerator(ISymbol contextSymbol, ITypeSymbol typeSymbol, bool nullable = false)
        {
            if (typeSymbol.IsValueType)
            {
                if (typeSymbol.IsUnmanagedType)
                {
                    if (typeSymbol.TypeKind == TypeKind.Enum)
                    {
                        var underlyingType = (INamedTypeSymbol)typeSymbol;
                        var enumType = underlyingType!.EnumUnderlyingType;
                        return nullable
                            ? $"value => value.HasValue ? (global::StackExchange.Redis.RedisValue)({enumType})value : global::StackExchange.Redis.RedisValue.Null"
                            : $"value => (global::StackExchange.Redis.RedisValue)({enumType})value";
                    }
                    else if (typeSymbol.Name.Equals("Nullable", StringComparison.Ordinal))
                    {
                        if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
                        {
                            if (namedTypeSymbol.TypeArguments.Length == 1)
                            {
                                var genericType = namedTypeSymbol.TypeArguments[0];
                                return GetGenerator(contextSymbol, genericType, true);
                            }
                        }
                        else
                        {
                            return $"value => throw new global::System.NotSupportedException($\"AAA Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
                        }
                    }
                    else
                    {
                        return nullable
                            ? $"value => value.HasValue ? (global::StackExchange.Redis.RedisValue)value : global::StackExchange.Redis.RedisValue.Null"
                            : $"value => (global::StackExchange.Redis.RedisValue)value";
                    }
                }
                else
                {
                    return $"() => throw new global::System.NotSupportedException($\"000 Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
                }
            }
            else if (typeSymbol.IsReferenceType)
            {
                if (typeSymbol.SpecialType == SpecialType.System_String)
                {
                    return $"value => (global::StackExchange.Redis.RedisValue)value";
                }
                else if (typeSymbol.Name.Equals(nameof(JsonDocument), StringComparison.Ordinal))
                {
                    return $"value => (global::StackExchange.Redis.RedisValue)value.RootElement.GetBytesFromBase64()";
                }
                else
                {
                    var jsonSourceGeneratedTypeInfo = $"{contextSymbol.Name}.Default.{GetDisplayName(typeSymbol)}";
                    return $"value => (global::StackExchange.Redis.RedisValue) global::System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(value, {jsonSourceGeneratedTypeInfo})";
                }
            }

            return $"() => throw new global::System.NotSupportedException($\"CCC Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
        }

        private static string GetDefaultCreator(ITypeSymbol typeSymbol)
        {
            if (typeSymbol.IsValueType)
            {
                if (typeSymbol.IsUnmanagedType)
                {
                    if (typeSymbol.Name.Equals("Nullable", StringComparison.Ordinal))
                    {
                        return $"() => null";
                    }
                    else
                    {
                        return $"() => default({typeSymbol})";
                    }
                }
                else
                {
                    return $"() => throw new global::System.NotSupportedException($\"000 Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
                }
            }
            else if (typeSymbol.IsReferenceType)
            {
                var isNullable = TryGetNullableUnderlyingType(typeSymbol, out var u);
                var underlyingType = isNullable ? u : typeSymbol;
                if (typeSymbol.SpecialType == SpecialType.System_String)
                {
                    if (isNullable)
                    {
                        return $"() => null";
                    }
                    else
                    {
                        return $"() => default({typeSymbol})";
                    }
                }
                else if (typeSymbol.Kind == SymbolKind.ArrayType)
                {
                    if (isNullable)
                    {
                        return $"() => null";
                    }
                    else
                    {
                        if (underlyingType is IArrayTypeSymbol ats)
                        {
                            var valueType = ats.ElementType;
                            return $"Array.Empty<{valueType}>";
                        }
                        else
                        {
                            return $"() => throw new global::System.NotSupportedException($\"GGG Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
                        }
                    }
                }
                else if (IsAssignedToIEnumerable(underlyingType))
                {
                    if (isNullable)
                    {
                        return $"() => null";
                    }
                    else
                    {
                        return $"() => new {underlyingType}()";
                    }
                }
                else
                {
                    if (isNullable)
                    {
                        return $"() => null";
                    }
                    else
                    {
                        return $"() => default({typeSymbol})";
                    }
                }
            }

            return $"() => throw new global::System.NotSupportedException($\"CCC Cannot convert {{value}} to {{typeof({typeSymbol})}}. Targeted property: {{nameof({typeSymbol.Name})}}\");";
        }

        private static bool IsAssignedToIEnumerable(ITypeSymbol typeSymbol)
        {
            return typeSymbol.AllInterfaces.Any(x => x.Name.Equals(nameof(IEnumerable), StringComparison.Ordinal));
        }

        private static bool TryGetNullableUnderlyingType(ITypeSymbol typeSymbol, out ITypeSymbol underlyingTypeSymbol)
        {
            if (typeSymbol.Name.Equals(nameof(Nullable), StringComparison.Ordinal))
            {
                return TryGetUnderlyingType(typeSymbol, out underlyingTypeSymbol);
            }
            else if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
            {
                underlyingTypeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
                return true;
            }

            underlyingTypeSymbol = typeSymbol;
            return false;
        }

        private static bool TryGetUnderlyingType(ITypeSymbol typeSymbol, out ITypeSymbol underlyingTypeSymbol)
        {
            if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                if (namedTypeSymbol.TypeArguments.Length == 1)
                {
                    underlyingTypeSymbol = namedTypeSymbol.TypeArguments[0];
                    return true;
                }
            }

            underlyingTypeSymbol = typeSymbol;
            return false;
        }

        private static string GetDisplayName(ITypeSymbol typeSymbol)
        {
            var name = typeSymbol.Name;
            if (typeSymbol is INamedTypeSymbol nts)
            {
                if (nts.TypeArguments.Length > 0)
                {
                    name += $"{string.Join("", nts.TypeArguments.Select(x => x.Name))}";
                }
            }
            else if (typeSymbol is IArrayTypeSymbol ats)
            {
                name = $"{ats.ElementType.Name}Array";
            }

            return name;
        }

        private static ITypeSymbol GetTypeSymbol(ISymbol symbol)
        {
            return symbol switch
            {
                IPropertySymbol ps => ps.Type,
                IFieldSymbol fs => fs.Type,
                _ => null
            };
        }

        private static (ClassDeclarationSyntax classDeclaration, ISymbol classSymbol, Dictionary<string, object> attributeProps, List<ITypeSymbol> types)? Transform(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            if (!(syntaxContext.Node is ClassDeclarationSyntax classDeclaration))
                return null;

            var classSymbol = ModelExtensions.GetDeclaredSymbol(syntaxContext.SemanticModel, classDeclaration, cancellationToken: cancellationToken);
            if (classSymbol == null)
                return null;

            var attributeProps = new Dictionary<string, object>();
            var types = new List<ITypeSymbol>();
            foreach (var attributeSyntax in classDeclaration.AttributeLists.SelectMany(attributeList => attributeList.Attributes))
            {
                if (!(attributeSyntax.Name is IdentifierNameSyntax nameSyntax))
                    continue;

                if (!nameSyntax.Identifier.Text.Equals(nameof(JsonSerializableAttribute).Replace("Attribute", ""), StringComparison.Ordinal))
                    continue;

                if (attributeSyntax.ArgumentList is null)
                    continue;

                foreach (var attributeArgumentSyntax in attributeSyntax.ArgumentList.Arguments)
                {
                    // var name = attributeArgumentSyntax.Expression.NameEquals?.Name.Identifier.Text;
                    // if (name == null)
                    // continue;

                    // var expressionSyntax = attributeArgumentSyntax.Expression;
                    // var operation = syntaxContext.SemanticModel.GetOperation(expressionSyntax);
                    // var argumentValue = operation!.ConstantValue.Value!;
                    // attributeProps.Add(name, argumentValue);

                    var expressionSyntax = attributeArgumentSyntax.Expression;
                    var operation = syntaxContext.SemanticModel.GetOperation(expressionSyntax) as ITypeOfOperation;
                    var typeOperand = operation?.TypeOperand;
                    types.Add(typeOperand);
                }
            }

            return (classDeclaration, classSymbol, attributeProps, types);
        }

        private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            if (!(syntaxNode is ClassDeclarationSyntax classDeclaration))
                return false;

            if (!classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword) && !classDeclaration.Modifiers.Any(SyntaxKind.InternalKeyword))
                return false;

            if (!classDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return false;

            var baseList = classDeclaration.BaseList;
            if (baseList == null)
                return false;

            var types = baseList.Types;
            if (types.Count == 0)
                return false;

            foreach (var type in types)
            {
                if (!(type.Type is IdentifierNameSyntax identifier))
                    continue;

                if (!identifier.Identifier.Text.Equals(nameof(JsonSerializerContext), StringComparison.Ordinal))
                    continue;

                return true;
            }

            return false;
        }

        private static ISymbol[] GetPropertySymbols(ISymbol typeSymbol, Dictionary<string, object> attrProps)
        {
            Memory<ISymbol> props = new ISymbol[1024];
            var lastIndex = -1;

            if (typeSymbol.Kind != SymbolKind.NamedType)
                return Array.Empty<ISymbol>();

            var includePrivate = (bool)attrProps[nameof(CacheableOptionsAttribute.IncludePrivate)];
            var includeFields = (bool)attrProps[nameof(CacheableOptionsAttribute.IncludeFields)];

            var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
            foreach (var symbol in namedTypeSymbol.GetMembers())
            {
                if (symbol is IPropertySymbol propSymbol)
                {
                    if (propSymbol.GetMethod == null || propSymbol.SetMethod == null)
                        continue;

                    if (propSymbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        if (!includePrivate)
                            continue;
                    }

                    props.Span[++lastIndex] = propSymbol;
                }
                else if (symbol is IFieldSymbol fieldSymbol)
                {
                    if (!includeFields)
                        continue;

                    if (fieldSymbol.DeclaredAccessibility == Accessibility.Private)
                    {
                        if (!includePrivate)
                            continue;
                    }

                    if (fieldSymbol.AssociatedSymbol != null)
                        continue;

                    if (fieldSymbol.IsReadOnly)
                        continue;

                    props.Span[++lastIndex] = fieldSymbol;
                }
                else
                {
                    continue;
                }
            }

            var baseType = namedTypeSymbol.BaseType;
            while (baseType != null)
            {
                foreach (var propSymbol in baseType.GetMembers().OfType<IPropertySymbol>())
                {
                    if (propSymbol.DeclaredAccessibility != Accessibility.Public)
                        continue;
                    if (propSymbol.GetMethod == null || propSymbol.SetMethod == null)
                        continue;

                    var exists = false;
                    for (var i = 0; i < lastIndex + 1; i++)
                    {
                        exists = props.Span[i].Name.Equals(propSymbol.Name, StringComparison.Ordinal);
                        if (exists)
                            break;
                    }

                    if (!exists)
                        props.Span[++lastIndex] = propSymbol;
                }

                baseType = baseType.BaseType;
            }

            if (lastIndex == -1)
                return Array.Empty<ISymbol>();

            props = props[..(lastIndex + 1)];
            return props.ToArray();
        }
    }
}