using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using R8.RedisHashMap.Tests.Models;
using StackExchange.Redis;
using static R8.RedisHashMap.Tests.StressModelGenerator;

namespace R8.RedisHashMap.Tests;

/// <summary>
///     High-volume stress and load coverage for the generated write/read paths.
///     <para>
///         These tests exist because <c>GetHashEntries</c> writes JSON through hand-written UTF-8 emitters that
///         bypass <see cref="Utf8JsonWriter" /> for known value shapes. Every assertion here therefore checks one of
///         two invariants: the emitted bytes are identical to what System.Text.Json would produce, and a written
///         model reads back equal to itself.
///     </para>
/// </summary>
public class StressTests
{
    private const int Iterations = 5_000;

    private static readonly string[] JsonPropertyNames = { "tags", "notes", "levels", "scores", "ticks", "attributes" };

    private readonly ITestOutputHelper _output;

    public StressTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Theory]
    [InlineData(Flavour.Clean)]
    [InlineData(Flavour.Escapable)]
    [InlineData(Flavour.NonAscii)]
    [InlineData(Flavour.Mixed)]
    public void RoundTrip_HighVolume_PreservesEveryValue(Flavour flavour)
    {
        var random = new Random(20260726 + (int)flavour);
        var helper = TestCacheContext.Default.StressModel;

        for (var i = 0; i < Iterations; i++)
        {
            var model = Create(random, flavour);

            var entries = helper.GetHashEntries(model);
            var restored = helper.FromHashEntries(entries);

            AssertRoundTripped(model, restored, $"iteration {i} ({flavour})");
        }
    }

    [Theory]
    [InlineData(Flavour.Clean)]
    [InlineData(Flavour.Escapable)]
    [InlineData(Flavour.NonAscii)]
    [InlineData(Flavour.Mixed)]
    public void Write_HighVolume_IsByteIdenticalToSystemTextJson(Flavour flavour)
    {
        var random = new Random(777 + (int)flavour);
        var helper = TestCacheContext.Default.StressModel;

        for (var i = 0; i < Iterations; i++)
        {
            var model = Create(random, flavour);
            AssertJsonMatchesSystemTextJson(helper.GetHashEntries(model), model, null, $"iteration {i} ({flavour})");
        }
    }

    /// <summary>
    ///     The hand-written emitters may only be used when they reproduce System.Text.Json exactly. Options that
    ///     change the encoding (relaxed escaping, dictionary key policy, number handling, indentation, string enums)
    ///     must therefore transparently fall back, including when the same helper instance alternates between them.
    /// </summary>
    [Fact]
    public void Write_UnderDivergentOptions_AlwaysMatchesSystemTextJson()
    {
        var optionSets = new (string Name, JsonSerializerOptions? Options)[]
        {
            ("null (defaults)", null),
            ("default instance", new JsonSerializerOptions()),
            ("relaxed encoder", new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }),
            ("camel dictionary keys", new JsonSerializerOptions { DictionaryKeyPolicy = JsonNamingPolicy.CamelCase }),
            ("numbers as strings", new JsonSerializerOptions { NumberHandling = JsonNumberHandling.WriteAsString }),
            ("indented", new JsonSerializerOptions { WriteIndented = true }),
            ("string enums", new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } })
        };

        var random = new Random(31337);
        var helper = TestCacheContext.Default.StressModel;

        // Interleaved so that each call re-validates against a different options instance than the cached one.
        for (var i = 0; i < 400; i++)
        {
            var model = Create(random, Flavour.Mixed);
            foreach (var (name, options) in optionSets)
                AssertJsonMatchesSystemTextJson(helper.GetHashEntries(model, options), model, options, $"iteration {i} with {name}");
        }
    }

    [Fact]
    public void RoundTrip_UnderDivergentOptions_PreservesEveryValue()
    {
        var optionSets = new[]
        {
            new JsonSerializerOptions(),
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping },
            new JsonSerializerOptions { DictionaryKeyPolicy = JsonNamingPolicy.CamelCase },
            new JsonSerializerOptions { WriteIndented = true }
        };

        var random = new Random(4242);
        var helper = TestCacheContext.Default.StressModel;

        for (var i = 0; i < 400; i++)
        {
            // Dictionary keys are lower-cased by the camel-case policy, so keys start lower-case here to keep the
            // round trip lossless; every other property still exercises the full alphabet.
            var model = Create(random, Flavour.Mixed);
            model.Attributes = model.Attributes.ToDictionary(p => p.Key.ToLowerInvariant(), p => p.Value, StringComparer.Ordinal);

            foreach (var options in optionSets)
            {
                var restored = helper.FromHashEntries(helper.GetHashEntries(model, options), options);
                AssertRoundTripped(model, restored, $"iteration {i}");
            }
        }
    }

    /// <summary>
    ///     Payloads far larger than the 4 KB pooled buffer must grow it correctly, and the next (small) payload on the
    ///     same thread must not observe any residue from the grown buffer.
    /// </summary>
    [Fact]
    public void LargePayloads_GrowPooledBuffer_WithoutCorruptingNeighbours()
    {
        var random = new Random(99);
        var helper = TestCacheContext.Default.StressModel;

        for (var i = 0; i < 12; i++)
        {
            var large = CreateLarge(random);
            AssertJsonMatchesSystemTextJson(helper.GetHashEntries(large), large, null, $"large iteration {i}");
            AssertRoundTripped(large, helper.FromHashEntries(helper.GetHashEntries(large)), $"large iteration {i}");

            var small = Create(random, Flavour.Clean, 2);
            AssertJsonMatchesSystemTextJson(helper.GetHashEntries(small), small, null, $"small iteration {i} after large");
            AssertRoundTripped(small, helper.FromHashEntries(helper.GetHashEntries(small)), $"small iteration {i} after large");
        }
    }

    /// <summary>
    ///     All six JSON properties share a single buffer and a single output array, each slice addressed by an
    ///     offset/length pair. Any combination of fast-path and bailed-out properties must keep those offsets aligned,
    ///     so every one of the 64 fast/slow combinations is exercised explicitly.
    /// </summary>
    [Fact]
    public void MixedFastAndFallbackProperties_KeepSliceOffsetsAligned()
    {
        var random = new Random(1234);
        var helper = TestCacheContext.Default.StressModel;

        for (var mask = 0; mask < 64; mask++)
        {
            var model = Create(random, Flavour.Clean);

            // Bit set => that property contains non-ASCII and must bail out into System.Text.Json.
            if ((mask & 1) != 0) model.Tags = new[] { "ok", "béd", "ok2" };
            if ((mask & 2) != 0) model.Notes = new List<string> { "n中te", "plain" };
            if ((mask & 4) != 0) model.Attributes = new Dictionary<string, string>(StringComparer.Ordinal) { ["k€"] = "v", ["p"] = "❤" };

            // Bit set => that property is present and clean, otherwise empty (skipped entirely).
            model.Levels = (mask & 8) != 0 ? new[] { StressLevel.Critical, StressLevel.Low } : Array.Empty<StressLevel>();
            model.Scores = (mask & 16) != 0 ? new[] { 0, -1, int.MaxValue } : Array.Empty<int>();
            model.Ticks = (mask & 32) != 0 ? new[] { long.MinValue, 7L } : Array.Empty<long>();

            var entries = helper.GetHashEntries(model);
            AssertJsonMatchesSystemTextJson(entries, model, null, $"mask {mask}");
            AssertRoundTripped(model, helper.FromHashEntries(entries), $"mask {mask}");
        }
    }

    /// <summary>
    ///     The generated helper is a shared singleton holding a thread-static scratch buffer and a lazily built
    ///     metadata cache. Concurrent readers and writers must never observe each other's partial state.
    /// </summary>
    [Fact]
    public void ConcurrentReadWrite_AcrossThreads_ProducesNoCorruption()
    {
        const int threads = 8;
        const int perThread = 3_000;

        var helper = TestCacheContext.Default.StressModel;
        var failures = new ConcurrentQueue<string>();

        Parallel.For(0, threads, thread =>
        {
            var random = new Random(500 + thread);
            for (var i = 0; i < perThread; i++)
            {
                var model = Create(random, Flavour.Mixed);
                try
                {
                    var restored = helper.FromHashEntries(helper.GetHashEntries(model));
                    AssertRoundTripped(model, restored, $"thread {thread} iteration {i}");
                    AssertJsonMatchesSystemTextJson(helper.GetHashEntries(model), model, null, $"thread {thread} iteration {i}");
                }
                catch (Exception ex)
                {
                    failures.Enqueue(ex.Message);
                }
            }
        });

        failures.Should().BeEmpty();
    }

    /// <summary>
    ///     Concurrent callers passing different options instances continually invalidate the shared metadata cache;
    ///     a torn read of that cache must degrade to the System.Text.Json path rather than emit wrong bytes.
    /// </summary>
    [Fact]
    public void ConcurrentWrites_WithAlternatingOptions_ProducesNoCorruption()
    {
        const int threads = 8;
        const int perThread = 1_500;

        var optionSets = new[]
        {
            new JsonSerializerOptions(),
            new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping },
            new JsonSerializerOptions { DictionaryKeyPolicy = JsonNamingPolicy.CamelCase },
            new JsonSerializerOptions { WriteIndented = true }
        };

        var helper = TestCacheContext.Default.StressModel;
        var failures = new ConcurrentQueue<string>();

        Parallel.For(0, threads, thread =>
        {
            var random = new Random(900 + thread);
            for (var i = 0; i < perThread; i++)
            {
                var model = Create(random, Flavour.Mixed);
                var options = optionSets[(thread + i) % optionSets.Length];
                try
                {
                    AssertJsonMatchesSystemTextJson(helper.GetHashEntries(model, options), model, options, $"thread {thread} iteration {i}");
                }
                catch (Exception ex)
                {
                    failures.Enqueue(ex.Message);
                }
            }
        });

        failures.Should().BeEmpty();
    }

    /// <summary>
    ///     Load check: a sustained write/read loop must stay far below the per-operation budget and must not leak
    ///     buffers. The bound is deliberately loose so it catches regressions of an order of magnitude, not noise.
    /// </summary>
    [Fact]
    public void SustainedLoad_StaysWithinTimeAndAllocationBudget()
    {
        const int operations = 50_000;

        var random = new Random(2026);
        var helper = TestCacheContext.Default.StressModel;
        var models = Enumerable.Range(0, 256).Select(_ => Create(random, Flavour.Clean)).ToArray();

        // Warm up the metadata cache, the thread-static buffer and the JIT before measuring.
        for (var i = 0; i < 1_000; i++)
            _ = helper.FromHashEntries(helper.GetHashEntries(models[i % models.Length]));

        var before = GC.GetTotalAllocatedBytes(true);
        var stopwatch = Stopwatch.StartNew();

        var checksum = 0;
        for (var i = 0; i < operations; i++)
        {
            var entries = helper.GetHashEntries(models[i % models.Length]);
            // Absent collections read back as null, so this also asserts the loop keeps observing real results.
            checksum += helper.FromHashEntries(entries)!.Tags?.Length ?? 0;
        }

        stopwatch.Stop();
        var allocated = GC.GetTotalAllocatedBytes(true) - before;

        var nsPerOperation = stopwatch.Elapsed.TotalMilliseconds * 1_000_000 / operations;
        var bytesPerOperation = (double)allocated / operations;
        _output.WriteLine($"{operations:N0} write+read ops in {stopwatch.ElapsedMilliseconds} ms ({nsPerOperation:N0} ns/op, {bytesPerOperation:N0} B/op, checksum {checksum})");

        // Two orders of magnitude of headroom over the ~1 us this actually takes: the point is to catch a
        // catastrophic regression, not to measure, so a machine busy with other work must not fail the build.
        nsPerOperation.Should().BeLessThan(200_000, "a write+read round trip should stay far below 200 us");
        bytesPerOperation.Should().BeLessThan(20_000, "a round trip of this model should allocate on the order of a few kilobytes");
    }

    private static void AssertRoundTripped(StressModel expected, StressModel? actual, string because)
    {
        actual.Should().NotBeNull(because);

        actual!.Id.Should().Be(expected.Id, because);

        // Empty strings are indistinguishable from an absent field in a Redis hash, so they read back as null.
        (actual.Name ?? string.Empty).Should().Be(expected.Name, because);
        (actual.Nickname ?? string.Empty).Should().Be(expected.Nickname ?? string.Empty, because);

        (actual.Tags ?? Array.Empty<string>()).Should().Equal(expected.Tags, because);
        (actual.Notes ?? new List<string>()).Should().Equal(expected.Notes, because);
        (actual.Levels ?? Array.Empty<StressLevel>()).Should().Equal(expected.Levels, because);
        (actual.Scores ?? Array.Empty<int>()).Should().Equal(expected.Scores, because);
        (actual.Ticks ?? Array.Empty<long>()).Should().Equal(expected.Ticks, because);

        var actualAttributes = actual.Attributes ?? new Dictionary<string, string>(StringComparer.Ordinal);
        actualAttributes.Count.Should().Be(expected.Attributes.Count, because);
        foreach (var pair in expected.Attributes)
        {
            actualAttributes.TryGetValue(pair.Key, out var value).Should().BeTrue($"{because}: key '{pair.Key}' should round trip");
            value.Should().Be(pair.Value, because);
        }
    }

    /// <summary>
    ///     Compares every JSON-serialised hash entry against the bytes System.Text.Json produces for the same value
    ///     and the same options - the exact contract the hand-written emitters must honour.
    /// </summary>
    private static void AssertJsonMatchesSystemTextJson(HashEntry[] entries, StressModel model, JsonSerializerOptions? options, string because)
    {
        foreach (var name in JsonPropertyNames)
        {
            var entry = entries.FirstOrDefault(e => e.Name == name);
            var expectedValue = ExpectedJson(model, name, options);

            if (expectedValue == null)
            {
                entry.Name.IsNull.Should().BeTrue($"{because}: '{name}' is empty and should be omitted");
                continue;
            }

            entry.Name.IsNull.Should().BeFalse($"{because}: '{name}' should be present");
            ((byte[])entry.Value!).Should().Equal(expectedValue, $"{because}: '{name}' must be byte-identical to System.Text.Json");
        }
    }

    private static byte[]? ExpectedJson(StressModel model, string name, JsonSerializerOptions? options)
    {
        switch (name)
        {
            case "tags":
                return model.Tags is { Length: > 0 } ? JsonSerializer.SerializeToUtf8Bytes(model.Tags, options) : null;
            case "notes":
                return model.Notes is { Count: > 0 } ? JsonSerializer.SerializeToUtf8Bytes(model.Notes, options) : null;
            case "levels":
                return model.Levels is { Length: > 0 } ? JsonSerializer.SerializeToUtf8Bytes(model.Levels, options) : null;
            case "scores":
                return model.Scores is { Length: > 0 } ? JsonSerializer.SerializeToUtf8Bytes(model.Scores, options) : null;
            case "ticks":
                return model.Ticks is { Length: > 0 } ? JsonSerializer.SerializeToUtf8Bytes(model.Ticks, options) : null;
            case "attributes":
                return model.Attributes is { Count: > 0 } ? JsonSerializer.SerializeToUtf8Bytes(model.Attributes, options) : null;
            default:
                throw new ArgumentOutOfRangeException(nameof(name), name, "Unknown JSON property");
        }
    }
}
