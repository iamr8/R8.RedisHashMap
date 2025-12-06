using R8.RedisHashMap.Tests.Converters;

namespace R8.RedisHashMap.Tests.Models;

public class TestSession
{
    public string SessionId { get; set; } = string.Empty;
    public int UserId { get; set; }
    
    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime StartTime { get; set; }
    
    [CacheConverter(typeof(DateTimeConverter))]
    public DateTime? EndTime { get; set; }

    [CacheConverter(typeof(TimeSpanConverter))]
    public TimeSpan Duration { get; set; }

    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
}