using System.Text;
using System.Text.Json;
using FluentAssertions;
using R8.RedisHashMap.Tests.Models;
using StackExchange.Redis;

namespace R8.RedisHashMap.Tests;

/// <summary>
///     Pins how System.Text.Json converter attributes interact with the hand-written UTF-8 writers.
/// </summary>
public class ConverterAttributeTests
{
    private static byte[] Entry(HashEntry[] entries, string name)
    {
        var entry = entries.First(e => e.Name == name);
        return (byte[])entry.Value!;
    }

    /// <summary>
    ///     A converter declared on the enum <em>type</em> is visible through the resolved JsonTypeInfo, so the
    ///     validation probe rejects the numeric fast writer and the value serializes as strings.
    /// </summary>
    [Fact]
    public void ConverterOnEnumType_DisablesTheNumericFastPath()
    {
        var model = new ConverterAnnotatedModel
        {
            Id = 1,
            Levels = new[] { AnnotatedLevel.Alpha, AnnotatedLevel.Beta },
            Tags = new[] { "plain" }
        };

        var levels = Entry(TestCacheContext.Default.ConverterAnnotatedModel.GetHashEntries(model), "levels");

        Encoding.UTF8.GetString(levels).Should().Be("[\"Alpha\",\"Beta\"]");
        levels.Should().Equal(JsonSerializer.SerializeToUtf8Bytes(model.Levels));
    }

    /// <summary>
    ///     A converter declared on the <em>property</em> is not part of the property type's JsonTypeInfo, so neither
    ///     the hand-written writer nor the System.Text.Json fallback applies it - the generator serializes each value
    ///     standalone against its type metadata. What matters is that both paths agree: an all-ASCII value (fast
    ///     writer) and a value containing non-ASCII (fallback) must be encoded by the same rules.
    /// </summary>
    [Fact]
    public void ConverterOnProperty_ProducesTheSameResultOnFastAndFallbackPaths()
    {
        var helper = TestCacheContext.Default.ConverterAnnotatedModel;

        var ascii = new ConverterAnnotatedModel { Id = 1, Tags = new[] { "alpha", "beta", "gamma" } };
        var nonAscii = new ConverterAnnotatedModel { Id = 2, Tags = new[] { "alpha", "bétä", "gamma" } };

        // Both are compared against plain type-level serialization: the property attribute is ignored uniformly.
        Entry(helper.GetHashEntries(ascii), "tags")
            .Should().Equal(JsonSerializer.SerializeToUtf8Bytes(ascii.Tags), "the fast writer must match type-level System.Text.Json");

        Entry(helper.GetHashEntries(nonAscii), "tags")
            .Should().Equal(JsonSerializer.SerializeToUtf8Bytes(nonAscii.Tags), "the fallback must match type-level System.Text.Json");

        // Neither path reversed the array, i.e. neither applied ReversedStringArrayConverter.
        Encoding.UTF8.GetString(Entry(helper.GetHashEntries(ascii), "tags")).Should().Be("[\"alpha\",\"beta\",\"gamma\"]");
    }

    [Fact]
    public void ConverterAnnotatedModel_RoundTrips()
    {
        var model = new ConverterAnnotatedModel
        {
            Id = 42,
            Levels = new[] { AnnotatedLevel.Beta, AnnotatedLevel.Alpha },
            Tags = new[] { "one", "two" }
        };

        var helper = TestCacheContext.Default.ConverterAnnotatedModel;
        var restored = helper.FromHashEntries(helper.GetHashEntries(model));

        restored.Should().NotBeNull();
        restored!.Id.Should().Be(model.Id);
        restored.Levels.Should().Equal(model.Levels);
    }
}
