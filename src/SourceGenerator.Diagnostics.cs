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

        public static readonly DiagnosticDescriptor TopLevelClass = new DiagnosticDescriptor(
            id: "RH1003",
            title: "Context class must be top-level",
            messageFormat: "{0} class cannot be nested inside another class",
            description: "The context class cannot be nested inside another class. It must be a top-level class.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor SetterMethodRequired = new DiagnosticDescriptor(
            id: "RH1004",
            title: "Setter method required",
            messageFormat: "{0} property must be { get; set; } instead of { get; init; }",
            description: "The property must have a setter method to be used in the source generator.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true);
    }
}