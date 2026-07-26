namespace R8.RedisHashMap.Tests.Models;

public enum StressLevel : byte
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 200
}

/// <summary>
///     Covers every value shape that the source generator emits a hand-written UTF-8 JSON writer for
///     (string sequences, enum/integral sequences and string dictionaries) alongside plain direct-write
///     properties, so round-trip and byte-parity stress tests exercise both the fast and the fallback path.
/// </summary>
public class StressModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Nickname { get; set; }

    public string[] Tags { get; set; } = Array.Empty<string>();
    public List<string> Notes { get; set; } = new();
    public StressLevel[] Levels { get; set; } = Array.Empty<StressLevel>();
    public int[] Scores { get; set; } = Array.Empty<int>();
    public long[] Ticks { get; set; } = Array.Empty<long>();
    public Dictionary<string, string> Attributes { get; set; } = new();
}
