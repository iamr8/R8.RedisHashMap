using Microsoft.CodeAnalysis;

namespace R8.RedisHashMap
{
    public static class SourceGeneratorDiagnostics
    {
        public static readonly DiagnosticDescriptor MismatchedConverterGenericType = new DiagnosticDescriptor(
            id: "RH1001",
            title: "Mismatched converter target type",
            messageFormat: "The generic type '{0}' of the converter does not match the property type",
            description: "The generic type of the converter must match the type of the property it is applied to.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor AbstractContext = new DiagnosticDescriptor(
            id: "RH1002",
            title: "Context cannot be abstract",
            messageFormat: "The context class '{0}' cannot be abstract.",
            description: "The context class must be a concrete class to be used in the source generator.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}