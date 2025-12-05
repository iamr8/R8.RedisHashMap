using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace R8.RedisHashMap
{
    /// <summary>
    /// Incremental source generator for Redis hash map serialization/deserialization.
    /// Generates helper classes for converting objects to and from Redis HashEntry arrays.
    /// </summary>
    /// <remarks>
    /// This generator is split into partial classes for better organization:
    /// <list type="bullet">
    ///   <item><description>SourceGenerator.cs - Entry point with Initialize and Execute methods</description></item>
    ///   <item><description>SourceGenerator.Parser.cs - Syntax parsing, transformation and configuration</description></item>
    ///   <item><description>SourceGenerator.Emitter.cs - Source code generation/emission</description></item>
    ///   <item><description>SourceGenerator.Helpers.cs - Helper methods for building code strings</description></item>
    ///   <item><description>SourceGenerator.Diagnostics.cs - Diagnostic descriptors</description></item>
    /// </list>
    /// </remarks>
    [Generator]
    public partial class SourceGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Initializes the incremental generator pipeline.
        /// </summary>
        /// <param name="context">The initialization context providing access to the syntax and compilation providers.</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register a syntax provider that filters and transforms relevant syntax nodes
            var declares = context.SyntaxProvider
                .CreateSyntaxProvider(Predicate, Transform);

            // Combine with compilation for access to semantic model
            var provider = context.CompilationProvider.Combine(declares.Collect());

            // Register the source output callback
            context.RegisterSourceOutput(provider, Execute);
        }

        /// <summary>
        /// Executes the source generation for all collected context declarations.
        /// </summary>
        private static void Execute(SourceProductionContext ctx, (Compilation Left, ImmutableArray<(INamedTypeSymbol ContextSymbol, TypeDeclarationSyntax Syntax, AttributeSyntax CacheContextAttrSyntax, List<AttributeSyntax> CacheObjectAttrListSyntax)?> Right) tuple)
        {
            foreach (var syntaxTuple in tuple.Right)
            {
                if (syntaxTuple == null)
                    continue;

                var (contextTypeSymbol, syntax, cacheContextAttrSyntax, cacheObjectAttrSyntaxList) = syntaxTuple.Value;
                
                // Validate the context class is not abstract
                if (contextTypeSymbol.IsAbstract)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(SourceGeneratorDiagnostics.AbstractContext, syntax.GetLocation(), contextTypeSymbol.Name));
                    continue;
                }

                // Validate the context class is not nested
                if (contextTypeSymbol.ContainingType != null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(SourceGeneratorDiagnostics.TopLevelClass, syntax.GetLocation(), contextTypeSymbol.Name));
                    continue;
                }

                // Get the semantic model and build context options
                var semanticModel = tuple.Left.GetSemanticModel(syntax.SyntaxTree);
                var contextOptions = GetContextOptions(ctx, contextTypeSymbol, syntax, cacheContextAttrSyntax, semanticModel, cacheObjectAttrSyntaxList);
                
                if (contextOptions.Types.Count == 0)
                    continue;

                // Generate the main context source
                GenerateContextSource(ctx, contextOptions);

                // Generate helper and property names for each type
                foreach (var typeOptions in contextOptions.Types)
                {
                    GenerateHelperSource(ctx, contextOptions, typeOptions);
                    GeneratePropertyNamesSource(ctx, contextOptions, typeOptions);
                }

                // Generate converters source if there are any converters
                if (contextOptions.Converters.Count > 0)
                {
                    GenerateConvertersSource(ctx, contextOptions);
                }
            }
        }
    }
}