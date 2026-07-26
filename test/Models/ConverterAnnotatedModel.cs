using System.Text.Json;
using System.Text.Json.Serialization;

namespace R8.RedisHashMap.Tests.Models;

/// <summary>An enum whose own declaration carries a converter, so it must not serialize as a number.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AnnotatedLevel
{
    Alpha,
    Beta
}

/// <summary>Writes a string array back-to-front, so applying it is unmistakable in the output.</summary>
public class ReversedStringArrayConverter : JsonConverter<string[]>
{
    public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var values = JsonSerializer.Deserialize<string[]>(ref reader, (JsonSerializerOptions?)null) ?? Array.Empty<string>();
        Array.Reverse(values);
        return values;
    }

    public override void Write(Utf8JsonWriter writer, string[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        for (var i = value.Length - 1; i >= 0; i--)
            writer.WriteStringValue(value[i]);

        writer.WriteEndArray();
    }
}

/// <summary>
///     Pins the behaviour of System.Text.Json converter attributes against the generated writers: a converter on the
///     enum <em>type</em> must be honoured, and a converter on the <em>property</em> must behave identically whether
///     the hand-written fast path or the System.Text.Json fallback produced the bytes.
/// </summary>
public class ConverterAnnotatedModel
{
    public int Id { get; set; }

    public AnnotatedLevel[] Levels { get; set; } = Array.Empty<AnnotatedLevel>();

    [JsonConverter(typeof(ReversedStringArrayConverter))]
    public string[] Tags { get; set; } = Array.Empty<string>();
}
