using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using StackExchange.Redis;

namespace R8.RedisHashMap
{
    /// <summary>
    ///     Provides utility methods for serializing and deserializing objects
    ///     to and from Redis values using JSON serialization and string encoding.
    ///     This class is intended to optimize operations with Redis by reusing buffers
    ///     and minimizing allocations during serialization and deserialization processes.
    /// </summary>
    public static class PooledJsonSerializer
    {
        public static readonly JsonWriterOptions ReusableJsonWriterOptions = new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        };

        public static ReadOnlyMemory<byte> GetBytes<TValue>(Utf8JsonWriter jsonWriter, ArrayBufferWriter<byte> bufferWriter, TValue value, JsonSerializerOptions? serializerOptions)
        {
            if (value is JsonElement element)
            {
                element.WriteTo(jsonWriter);
            }
            else
            {
                JsonSerializer.Serialize(jsonWriter, value, serializerOptions);
            }

            jsonWriter.Flush();
            return bufferWriter.WrittenMemory;
        }

        public static ReadOnlyMemory<byte> GetBytes<TValue>(Utf8JsonWriter jsonWriter, ArrayBufferWriter<byte> bufferWriter, TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            JsonSerializer.Serialize(jsonWriter, value, jsonTypeInfo);
            jsonWriter.Flush();
            return bufferWriter.WrittenMemory;
        }

        /// <summary>
        ///     Deserializes a given RedisValue into the specified type using JSON serialization options.
        /// </summary>
        /// <typeparam name="TValue">The type into which the RedisValue should be deserialized.</typeparam>
        /// <param name="value">The RedisValue containing the JSON data to be deserialized.</param>
        /// <param name="serializerOptions">The serialization options to customize deserialization behavior.</param>
        /// <returns>The deserialized object of type TValue.</returns>
        public static TValue Parse<TValue>(this RedisValue value, JsonSerializerOptions? serializerOptions)
        {
            var bytes = ((ReadOnlyMemory<byte>)value).Span;
            return JsonSerializer.Deserialize<TValue>(bytes, serializerOptions)!;
        }

        /// <summary>
        ///     Deserializes a given RedisValue to the specified type using the provided JSON type information.
        /// </summary>
        /// <typeparam name="TValue">The type to which the RedisValue will be deserialized.</typeparam>
        /// <param name="value">The RedisValue containing the JSON-encoded data to be deserialized.</param>
        /// <param name="jsonTypeInfo">The JSON type information to guide the deserialization process.</param>
        /// <returns>The deserialized value of the specified type.</returns>
        public static TValue Parse<TValue>(this RedisValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            var bytes = ((ReadOnlyMemory<byte>)value).Span;
            return JsonSerializer.Deserialize(bytes, jsonTypeInfo)!;
        }

        /// <summary>
        ///     Parses a RedisValue into the specified type using the given JsonSerializerContext.
        /// </summary>
        /// <typeparam name="T">The type into which the RedisValue should be deserialized.</typeparam>
        /// <param name="value">The RedisValue containing the serialized data to parse.</param>
        /// <param name="serializerContext">A JsonSerializerContext providing type-specific serializer metadata for the deserialization process.</param>
        /// <returns>An instance of type T deserialized from the given RedisValue.</returns>
        public static T Parse<T>(this RedisValue value, JsonSerializerContext serializerContext)
        {
            return serializerContext.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> jsonType
                ? value.Parse<T>(jsonType)
                : value.Parse<T>(serializerContext.Options);
        }

        /// <summary>
        ///     Deserializes a RedisValue into a JsonElement.
        /// </summary>
        /// <param name="value">The RedisValue containing JSON data to be deserialized.</param>
        /// <returns>A JsonElement representing the deserialized JSON structure.</returns>
        public static JsonElement GetJsonElement(this RedisValue value)
        {
            var bytes = ((ReadOnlyMemory<byte>)value).Span;
            var utf8JsonReader = new Utf8JsonReader(bytes);
            return JsonElement.ParseValue(ref utf8JsonReader);
        }

        /// <summary>
        ///     Deserializes a RedisValue into a JsonDocument.
        /// </summary>
        /// <param name="value">The RedisValue containing the data to be deserialized.</param>
        /// <returns>A JsonDocument representing the deserialized data.</returns>
        public static JsonDocument GetJsonDocument(this RedisValue value)
        {
            var bytes = (ReadOnlyMemory<byte>)value;
            return JsonDocument.Parse(bytes);
        }
    }
}