using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace R8.RedisHashMap
{
    public partial class SourceGenerator
    {
        /// <summary>
        /// Predicate for filtering syntax nodes that may contain CacheContext or CacheObject attributes.
        /// </summary>
        private static bool Predicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
        {
            return TryGetContextSyntax(syntaxNode, out _);
        }

        /// <summary>
        /// Transforms the syntax node into a tuple containing the context symbol, syntax, and attribute information.
        /// </summary>
        private static (INamedTypeSymbol ContextSymbol, TypeDeclarationSyntax Syntax, AttributeSyntax CacheContextAttrSyntax, List<AttributeSyntax> CacheObjectAttrListSyntax)? Transform(GeneratorSyntaxContext syntaxContext, CancellationToken cancellationToken)
        {
            if (!TryGetContextSyntax(syntaxContext.Node, out var syntax))
                return null;

            var symbol = ModelExtensions.GetDeclaredSymbol(syntaxContext.SemanticModel, syntax, cancellationToken);
            if (!(symbol is INamedTypeSymbol contextTypeSymbol))
                return null;

            var cacheObjectAttrSyntaxList = new List<AttributeSyntax>();
            AttributeSyntax? cacheContextAttrSyntax = null;
            foreach (var attrSyntax in syntax.AttributeLists.SelectMany(attrsListSyntax => attrsListSyntax.Attributes))
            {
                var attrName = attrSyntax.Name.ToString();
                if (attrName == nameof(CacheObjectAttribute).Replace(nameof(Attribute), ""))
                {
                    cacheObjectAttrSyntaxList.Add(attrSyntax);
                }
                else if (attrName == nameof(CacheContextAttribute).Replace(nameof(Attribute), ""))
                {
                    cacheContextAttrSyntax = attrSyntax;
                }
            }

            if (cacheContextAttrSyntax == null || cacheObjectAttrSyntaxList.Count == 0)
                return null;

            return (contextTypeSymbol, syntax, cacheContextAttrSyntax, cacheObjectAttrSyntaxList);
        }

        /// <summary>
        /// Tries to extract a type declaration syntax from a syntax node.
        /// </summary>
        private static bool TryGetContextSyntax(SyntaxNode syntaxNode, [NotNullWhen(true)] out TypeDeclarationSyntax? syntax)
        {
            if (!(syntaxNode is ClassDeclarationSyntax classDeclaration))
            {
                if (!(syntaxNode is RecordDeclarationSyntax recordDeclaration) || recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.ClassKeyword))
                {
                    syntax = null;
                    return false;
                }

                syntax = recordDeclaration;
            }
            else
            {
                syntax = classDeclaration;
            }

            if (syntax.AttributeLists.Count == 0)
                return false;

            return true;
        }

        /// <summary>
        /// Configures context options based on the CacheContext attribute.
        /// </summary>
        private static void ConfigureByCacheContext(AttributeSyntax cacheContextAttrSyntax, ContextOptions options)
        {
            if (cacheContextAttrSyntax.ArgumentList != null)
            {
                foreach (var argSyntax in cacheContextAttrSyntax.ArgumentList.Arguments)
                {
                    if (!(argSyntax.Expression is MemberAccessExpressionSyntax { Expression: IdentifierNameSyntax identifierSyntax } expressionSyntax))
                        continue;

                    var propertyName = argSyntax.NameEquals!.Name.Identifier.Text;
                    // var typeName = identifierSyntax.Identifier.Text;
                    var value = expressionSyntax.Name.Identifier.Text;
                    switch (propertyName)
                    {
                        case nameof(CacheContextAttribute.NamingStrategy):
                        {
                            options.NamingStrategy = value switch
                            {
                                nameof(CacheFieldNamingStrategy.PascalCase) => CacheFieldNamingStrategy.PascalCase,
                                nameof(CacheFieldNamingStrategy.CamelCase) => CacheFieldNamingStrategy.CamelCase,
                                nameof(CacheFieldNamingStrategy.SnakeCase) => CacheFieldNamingStrategy.SnakeCase,
                            };
                            break;
                        }
                        case nameof(CacheContextAttribute.GenerationMode):
                        {
                            options.GenerationMode = value switch
                            {
                                nameof(CacheGenerationMode.Default) => CacheGenerationMode.Default,
                                nameof(CacheGenerationMode.Serialization) => CacheGenerationMode.Serialization,
                                nameof(CacheGenerationMode.Deserialization) => CacheGenerationMode.Deserialization,
                            };
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Configures context options based on the CacheObject attributes.
        /// </summary>
        private static void ConfigureByCacheObjects(SourceProductionContext ctx, SemanticModel semanticModel, List<AttributeSyntax> cacheObjectAttrSyntaxList, ContextOptions options, CancellationToken cancellationToken)
        {
            foreach (var attributeSyntax in cacheObjectAttrSyntaxList)
            {
                if (attributeSyntax.ArgumentList == null)
                    continue;

                ObjectOptions? objectTypeOptions = null;

                foreach (var argSyntax in attributeSyntax.ArgumentList.Arguments)
                {
                    if (argSyntax.Expression is TypeOfExpressionSyntax typeOfExpressionSyntax)
                    {
                        if (objectTypeOptions != null)
                            throw new InvalidOperationException("Only one type can be specified for the CacheObject attribute.");

                        if (!(semanticModel.GetTypeInfo(typeOfExpressionSyntax.Type, cancellationToken).Type is INamedTypeSymbol typeSymbol))
                            continue;

                        objectTypeOptions = GetObjectOptions(ctx, typeSymbol);
                    }
                    else if (argSyntax.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                    {
                        if (!(memberAccessExpressionSyntax.Expression is IdentifierNameSyntax identifierNameSyntax))
                            continue;

                        var name = identifierNameSyntax.Identifier.Text;
                        var value = memberAccessExpressionSyntax.Name.Identifier.Text;
                        switch (name)
                        {
                            case nameof(CacheObjectAttribute.GenerationMode):
                            {
                                objectTypeOptions!.GenerationMode = value switch
                                {
                                    nameof(CacheGenerationMode.Default) => CacheGenerationMode.Default,
                                    nameof(CacheGenerationMode.Serialization) => CacheGenerationMode.Serialization,
                                    nameof(CacheGenerationMode.Deserialization) => CacheGenerationMode.Deserialization,
                                };
                                break;
                            }
                        }
                    }
                }

                if (objectTypeOptions != null)
                    options.Types.Add(objectTypeOptions);
            }
        }

        /// <summary>
        /// Creates context options from the context type symbol and its attributes.
        /// </summary>
        private static ContextOptions GetContextOptions(SourceProductionContext ctx, INamedTypeSymbol contextTypeSymbol, TypeDeclarationSyntax syntax, AttributeSyntax cacheContextAttrSyntax, SemanticModel semanticModel, List<AttributeSyntax> cacheObjectAttrSyntaxList)
        {
            var objectTypeSymbol = TypeSymbol.Create(ctx, contextTypeSymbol);
            var contextOptions = new ContextOptions
            {
                ObjectTypeSymbol = objectTypeSymbol,
                DisplayName = objectTypeSymbol.GetDisplayName(),
                Namespace = contextTypeSymbol.ContainingNamespace.ToString(),
                // FullQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                // AssemblyName = typeSymbol.ContainingAssembly.Name,
                AccessibilityModifier = contextTypeSymbol.DeclaredAccessibility switch
                {
                    Accessibility.Public => "public",
                    Accessibility.Internal => "internal",
                    _ => "public"
                },
                Keyword = syntax.Keyword,
            };
            ConfigureByCacheContext(cacheContextAttrSyntax, contextOptions);
            ConfigureByCacheObjects(ctx, semanticModel, cacheObjectAttrSyntaxList, contextOptions, ctx.CancellationToken);

            foreach (var typeOptions in contextOptions.Types)
            {
                typeOptions.HelperName = $"{contextOptions.DisplayName}_{typeOptions.DisplayName}RedisHelper";
                typeOptions.HelperTypeName = $"{contextOptions.Namespace}.{typeOptions.HelperName}";
            }

            return contextOptions;
        }

        /// <summary>
        /// Creates object options from the type symbol, extracting property and converter information.
        /// </summary>
        private static ObjectOptions? GetObjectOptions(SourceProductionContext ctx, INamedTypeSymbol typeSymbol)
        {
            var members = typeSymbol.GetMembers();
            if (!members.Any(member => member.DeclaredAccessibility == Accessibility.Public || member.DeclaredAccessibility == Accessibility.Internal))
                return null;

            var objectTypeSymbol = TypeSymbol.Create(ctx, typeSymbol);
            var options = new ObjectOptions
            {
                ObjectTypeSymbol = objectTypeSymbol,
                DisplayName = objectTypeSymbol.GetDisplayName(),
                Namespace = typeSymbol.ContainingNamespace.ToString(),
                AccessibilityModifier = typeSymbol.DeclaredAccessibility switch
                {
                    Accessibility.Public => "public",
                    Accessibility.Internal => "internal",
                    _ => "public"
                },
            };

            if (options.ObjectTypeSymbol.Converter != null && !options.Converters.Any(c => SymbolEqualityComparer.Default.Equals(c.ConverterType, options.ObjectTypeSymbol.Converter!.ConverterType)))
                options.Converters.Add(options.ObjectTypeSymbol.Converter);

            // properties
            foreach (var symbol in members)
            {
                if (symbol.IsStatic)
                    continue;

                if (symbol is IPropertySymbol propertySymbol)
                {
                    var attributes = propertySymbol.GetAttributes();
                    if (attributes.Any(x => x.AttributeClass.Name.Equals(nameof(JsonIgnoreAttribute), StringComparison.Ordinal)))
                        continue; // [JsonIgnore]

                    if (propertySymbol.IsWriteOnly || propertySymbol.IsReadOnly)
                        continue; // No getter method && no setter method

                    // if (propertySymbol.SetMethod!.IsInitOnly)
                    // {
                    //     // init;
                    //     ctx.ReportDiagnostic(Diagnostic.Create(SourceGeneratorDiagnostics.SetterMethodRequired, propertySymbol.Locations.First(), propertySymbol.Name));
                    //     continue;
                    // }

                    if (propertySymbol.IsIndexer)
                        continue; // this[]
                }
                else if (symbol is IFieldSymbol fieldSymbol)
                {
                    if (fieldSymbol.AssociatedSymbol != null)
                        continue; // Backing field

                    if (fieldSymbol.IsReadOnly)
                        continue; // readonly int Foo;
                }
                else
                {
                    continue;
                }

                var item = TypeSymbol.Create(ctx, symbol);
                options.Properties.Add(item);

                if (item.Converter != null && !options.Converters.Any(c => SymbolEqualityComparer.Default.Equals(c.ConverterType, item.Converter!.ConverterType)))
                    options.Converters.Add(item.Converter);
            }

            if (options.Properties.Count == 0)
                return null;

            return options;
        }
    }
}
