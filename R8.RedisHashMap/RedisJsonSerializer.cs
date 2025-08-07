using System;
using System.Buffers;
using System.Text;
using System.Text.Json;
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
    public static class RedisJsonSerializer
    {
        private static readonly Encoding Encoding = Encoding.UTF8;

        /// <summary>
        ///     Specifies the default configuration options for <see cref="Utf8JsonWriter" /> to be reused across
        ///     various JSON serialization and deserialization operations. This helps to reduce overhead by minimizing
        ///     the creation of multiple configurations and ensuring consistent behavior throughout the application.
        /// </summary>
        public static readonly JsonWriterOptions ReusableJsonWriterOptions = new JsonWriterOptions
        {
            Indented = false,
            SkipValidation = false
        };

        /// <summary>
        ///     Serializes a given value to JSON and writes it to a buffer.
        ///     Returns the serialized JSON as a RedisValue.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="bufferWriter">The buffer writer used for storing the serialized output.</param>
        /// <param name="jsonWriter">The JSON writer used for writing the serialized data.</param>
        /// <param name="value">The value to be serialized to JSON.</param>
        /// <param name="serializerOptions">The serializer options to customize JSON serialization behavior.</param>
        /// <returns>A RedisValue containing the serialized JSON data.</returns>
        public static ReadOnlyMemory<byte> Serialize<TValue>(ArrayBufferWriter<byte> bufferWriter, Utf8JsonWriter jsonWriter, TValue value, JsonSerializerOptions? serializerOptions)
        {
            bufferWriter.Clear();
            jsonWriter.Reset(bufferWriter);
            JsonSerializer.Serialize(jsonWriter, value, serializerOptions);
            jsonWriter.Flush();
            return bufferWriter.WrittenMemory;
        }

        /// <summary>
        ///     Serializes a given value to JSON and writes it to a buffer.
        ///     Returns the serialized JSON as a RedisValue.
        /// </summary>
        /// <typeparam name="TValue">The type of the value to serialize.</typeparam>
        /// <param name="bufferWriter">The buffer writer used to store the serialized output.</param>
        /// <param name="jsonWriter">The JSON writer used to write the serialized data.</param>
        /// <param name="value">The value to be serialized into JSON.</param>
        /// <param name="jsonTypeInfo">The JSON type information for the value being serialized.</param>
        /// <returns>A RedisValue containing the serialized JSON data.</returns>
        public static ReadOnlyMemory<byte> Serialize<TValue>(ArrayBufferWriter<byte> bufferWriter, Utf8JsonWriter jsonWriter, TValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            bufferWriter.Clear();
            jsonWriter.Reset(bufferWriter);
            JsonSerializer.Serialize(jsonWriter, value, jsonTypeInfo);
            jsonWriter.Flush();
            return bufferWriter.WrittenMemory;
        }

        /// <summary>
        ///     Serializes a given JSON element to a buffer and returns the serialized data as a read-only memory of bytes.
        /// </summary>
        /// <param name="bufferWriter">The buffer writer used to store the serialized output.</param>
        /// <param name="jsonWriter">The JSON writer used to format the serialized data.</param>
        /// <param name="value">The JSON element to serialize.</param>
        /// <returns>A read-only memory of bytes representing the serialized data.</returns>
        public static ReadOnlyMemory<byte> Serialize(ArrayBufferWriter<byte> bufferWriter, Utf8JsonWriter jsonWriter, in JsonElement value)
        {
            bufferWriter.Clear();
            jsonWriter.Reset(bufferWriter);
            value.WriteTo(jsonWriter);
            jsonWriter.Flush();
            return bufferWriter.WrittenMemory;
        }

        /// <summary>
        ///     Serializes a given string to a buffer and returns it as a RedisValue.
        /// </summary>
        /// <param name="bufferWriter">The buffer writer used for storing the serialized output.</param>
        /// <param name="str">The string value to be serialized.</param>
        /// <returns>A RedisValue containing the serialized string data.</returns>
        public static ReadOnlyMemory<byte> Serialize(ArrayBufferWriter<byte> bufferWriter, string str)
        {
            bufferWriter.Clear();
            var value = str.AsSpan();
            var byteCount = Encoding.GetByteCount(value);
            var bytes = bufferWriter.GetSpan(byteCount);
            var bytesWritten = Encoding.GetBytes(value, bytes);
            bufferWriter.Advance(bytesWritten);
            return bufferWriter.WrittenMemory;
        }

        /// <summary>
        ///     Deserializes a given RedisValue into the specified type using JSON serialization options.
        /// </summary>
        /// <typeparam name="TValue">The type into which the RedisValue should be deserialized.</typeparam>
        /// <param name="value">The RedisValue containing the JSON data to be deserialized.</param>
        /// <param name="serializerOptions">The serialization options to customize deserialization behavior.</param>
        /// <returns>The deserialized object of type TValue.</returns>
        public static TValue Deserialize<TValue>(in RedisValue value, JsonSerializerOptions? serializerOptions)
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
        public static TValue Deserialize<TValue>(in RedisValue value, JsonTypeInfo<TValue> jsonTypeInfo)
        {
            var bytes = ((ReadOnlyMemory<byte>)value).Span;
            return JsonSerializer.Deserialize(bytes, jsonTypeInfo)!;
        }

        /// <summary>
        ///     Deserializes a RedisValue into a JsonElement.
        /// </summary>
        /// <param name="value">The RedisValue containing JSON data to be deserialized.</param>
        /// <returns>A JsonElement representing the deserialized JSON structure.</returns>
        public static JsonElement DeserializeToJsonElement(in RedisValue value)
        {
            var bytes = ((ReadOnlyMemory<byte>)value).Span;
            var utf8JsonReader = new Utf8JsonReader(bytes);
            return JsonElement.ParseValue(ref utf8JsonReader);
        }

        public static JsonDocument DeserializeToJsonDocument(in RedisValue value)
        {
            var bytes = (ReadOnlyMemory<byte>)value;
            return JsonDocument.Parse(bytes);
        }
    }
}