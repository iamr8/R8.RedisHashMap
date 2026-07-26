using System.Globalization;
using R8.RedisHashMap.Tests.Models;

namespace R8.RedisHashMap.Tests;

/// <summary>
///     Deterministic generator of adversarial <see cref="StressModel" /> payloads.
///     <para>
///         The alphabets are chosen so that the generated data straddles the boundary of the hand-written UTF-8
///         writers: pure ASCII values take the vectorised fast path, values containing escapable characters take the
///         scalar escape path, and values containing non-ASCII force a bail-out into System.Text.Json. Lengths
///         deliberately cluster around the 8-character vector block size so the tail handling is covered.
///     </para>
/// </summary>
public static class StressModelGenerator
{
    private const string AsciiSafe = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 -_./=:@!#$%^*()[]{}|~?,;";
    private const string AsciiEscapable = "\"&'+<>\\`\b\t\n\f\r\u0000\u001F\u007F ";
    // BMP-only: lone surrogates are replaced with U+FFFD by System.Text.Json itself, which is JSON-encoder
    // behaviour rather than anything this library controls, so they are out of scope for round-trip equality.
    private const string NonAscii = "\u00E9\u00FC\u00F1\u00C6\u00DF\u0100\u0416\u05D0\u0623\u4E2D\u65E5\u672C\u20AC\u2764\uFF21";

    /// <summary>Content classes a generated string can be drawn from.</summary>
    public enum Flavour
    {
        /// <summary>Pure ASCII with no escapes: always takes the vectorised fast path.</summary>
        Clean,

        /// <summary>ASCII containing escapable characters: takes the scalar escape path.</summary>
        Escapable,

        /// <summary>Contains non-ASCII: forces the writer to bail out into System.Text.Json.</summary>
        NonAscii,

        /// <summary>Any of the above, plus empty strings.</summary>
        Mixed
    }

    public static StressModel Create(Random random, Flavour flavour = Flavour.Mixed, int maxCollectionSize = 8)
    {
        return new StressModel
        {
            Id = random.Next(int.MinValue, int.MaxValue),
            Name = String(random, flavour),
            Nickname = random.Next(4) == 0 ? null : String(random, flavour),
            Tags = Strings(random, flavour, maxCollectionSize),
            Notes = new List<string>(Strings(random, flavour, maxCollectionSize)),
            Levels = Enums(random, maxCollectionSize),
            Scores = Ints(random, maxCollectionSize),
            Ticks = Longs(random, maxCollectionSize),
            Attributes = Attributes(random, flavour, maxCollectionSize)
        };
    }

    /// <summary>A model whose collections are large enough to force the pooled buffer to grow repeatedly.</summary>
    public static StressModel CreateLarge(Random random, Flavour flavour = Flavour.Mixed)
    {
        var model = Create(random, flavour, 4);
        model.Tags = Enumerable.Range(0, 2_000).Select(_ => String(random, flavour, 1, 120)).ToArray();
        model.Scores = Enumerable.Range(0, 4_000).Select(_ => random.Next(int.MinValue, int.MaxValue)).ToArray();
        model.Attributes = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < 500; i++)
            model.Attributes[i.ToString(CultureInfo.InvariantCulture) + String(random, flavour, 1, 30)] = String(random, flavour, 0, 200);

        return model;
    }

    public static string String(Random random, Flavour flavour, int minLength = 0, int maxLength = 20)
    {
        var length = random.Next(minLength, maxLength + 1);
        if (length == 0)
            return string.Empty;

        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = Char(random, flavour);

        return new string(chars);
    }

    private static char Char(Random random, Flavour flavour)
    {
        var effective = flavour;
        if (flavour == Flavour.Mixed)
            effective = (Flavour)random.Next(3);

        return effective switch
        {
            Flavour.Clean => AsciiSafe[random.Next(AsciiSafe.Length)],
            // Escapable/non-ASCII characters stay rare so the surrounding fast path is still exercised.
            Flavour.Escapable => random.Next(4) == 0 ? AsciiEscapable[random.Next(AsciiEscapable.Length)] : AsciiSafe[random.Next(AsciiSafe.Length)],
            Flavour.NonAscii => random.Next(6) == 0 ? NonAscii[random.Next(NonAscii.Length)] : AsciiSafe[random.Next(AsciiSafe.Length)],
            _ => AsciiSafe[random.Next(AsciiSafe.Length)]
        };
    }

    private static string[] Strings(Random random, Flavour flavour, int maxCount)
    {
        var values = new string[random.Next(maxCount + 1)];
        for (var i = 0; i < values.Length; i++)
            values[i] = String(random, flavour);

        return values;
    }

    private static StressLevel[] Enums(Random random, int maxCount)
    {
        var values = new StressLevel[random.Next(maxCount + 1)];
        for (var i = 0; i < values.Length; i++)
            values[i] = random.Next(4) switch
            {
                0 => StressLevel.Low,
                1 => StressLevel.Medium,
                2 => StressLevel.High,
                _ => StressLevel.Critical
            };

        return values;
    }

    private static int[] Ints(Random random, int maxCount)
    {
        var values = new int[random.Next(maxCount + 1)];
        for (var i = 0; i < values.Length; i++)
            values[i] = random.Next(3) == 0 ? random.Next(0, 10) : random.Next(int.MinValue, int.MaxValue);

        return values;
    }

    private static long[] Longs(Random random, int maxCount)
    {
        var values = new long[random.Next(maxCount + 1)];
        for (var i = 0; i < values.Length; i++)
            values[i] = random.Next(4) switch
            {
                0 => 0,
                1 => long.MinValue,
                2 => long.MaxValue,
                _ => ((long)random.Next() << 32) | (uint)random.Next()
            };

        return values;
    }

    private static Dictionary<string, string> Attributes(Random random, Flavour flavour, int maxCount)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        var count = random.Next(maxCount + 1);
        for (var i = 0; i < count; i++)
        {
            // The index prefix guarantees distinct keys without perturbing the generated content.
            var key = i.ToString(CultureInfo.InvariantCulture) + String(random, flavour, 1, 18);
            values[key] = String(random, flavour);
        }

        return values;
    }
}
