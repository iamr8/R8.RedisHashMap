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

        public static readonly DiagnosticDescriptor PropertyRequiresJsonSerialization = new DiagnosticDescriptor(
            id: "RH1005",
            title: "Property uses JSON serialization",
            messageFormat: "Property '{0}' of type '{1}' will be serialized/deserialized using JSON. Consider using a custom converter with [CacheConverter] attribute for better performance and reliability, or ensure the type is JSON-serializable.",
            description: "The property type is not natively supported by Redis and will be serialized/deserialized using JSON. This may cause issues if the type is not JSON-serializable or if you need custom serialization logic. Consider adding a [CacheConverter] attribute with a custom converter for better control.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static readonly DiagnosticDescriptor PropertyIEnumerableDegradesPerformance = new DiagnosticDescriptor(
            id: "RH1006",
            title: "Property uses IEnumerable type",
            messageFormat: "Property '{0}' of type '{1}' uses IEnumerable which may degrade performance. Consider using a more specific collection type like List<T> or Array for better performance.",
            description: "Using IEnumerable types may lead to performance degradation due to deferred execution and multiple enumerations. Consider using more specific collection types such as List<T> or Array to improve performance when storing data in Redis.",
            category: "SourceGenerator",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );
    }
}