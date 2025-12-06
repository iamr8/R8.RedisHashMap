using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace R8.RedisHashMap.Test.Objects;

/// <summary>
///     Initializes a new instance of <see cref="LocalRegion" />.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
[JsonConverter(typeof(LocalRegionJsonConverter))]
public class LocalRegion : IRegion, IFormatProvider
{
    private static readonly object SyncRoot = new();
    private static readonly ConcurrentDictionary<string, LocalRegion> Cached = new();
    private static readonly AsyncLocal<LocalRegion?> CurrentLocal = new();

    private static LocalRegion? _current;

    public const string IranCultureName = "fa-IR";
    public const string IraqCultureName = "ar-IQ";

    public static readonly LocalRegion[] SupportedRegions =
    {
        Iran,
        Iraq
    };

    static LocalRegion()
    {
        lock (SyncRoot)
        {
            _current ??= CurrentLocal.Value ?? Iran;
        }
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="LocalRegion" />.
    /// </summary>
    private LocalRegion()
    {
    }

    public static LocalRegion Iran => GetOrCreate(IranCultureName);
    public static LocalRegion Iraq => GetOrCreate(IraqCultureName);

    /// <summary>
    ///     Gets or sets the current region.
    /// </summary>
    /// <remarks>If a scope is started using <see cref="StartScope" />, this property will return the region of the scope.</remarks>
    public static LocalRegion Current
    {
        get
        {
            lock (SyncRoot)
            {
                if (CurrentLocal.Value != null)
                    return CurrentLocal.Value;
                if (_current != null)
                    return _current;

                _current = Iran;
                return _current;
            }
        }

        set
        {
            lock (SyncRoot)
            {
                _current = value;
            }
        }
    }

    public string Name => Culture.Name;

    public CultureInfo Culture { get; private init; }

    public ApplicationCurrentRegion Id { get; private init; }

    public RegionInfo Region { get; private init; }

    public bool IsRightToLeft => Culture.TextInfo.IsRightToLeft;

    public string GetDisplayName()
    {
        var sb = new DefaultInterpolatedStringHandler();
        sb.AppendFormatted(Region.EnglishName);
        sb.AppendLiteral(" (");
        sb.AppendFormatted(Region.DisplayName);
        sb.AppendLiteral(")");
        return sb.ToString();
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="LocalRegion" />.
    /// </summary>
    /// <param name="cultureName">A valid culture iso name.</param>
    /// <param name="id">The <see cref="ApplicationCurrentRegion" /> identifier for the region.</param>
    /// <returns>A <see cref="LocalRegion" /> object</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="cultureName" /> is null.</exception>
    /// <exception cref="CultureNotFoundException">Thrown when <paramref name="cultureName" /> is not a valid culture.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cultureName" /> is a neutral culture.</exception>
    public static LocalRegion GetOrCreate(string? cultureName)
    {
        if (cultureName == null)
            throw new ArgumentNullException(nameof(cultureName));

        if (Cached.TryGetValue(cultureName, out var cachedRegion))
            return cachedRegion;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        if (culture.IsNeutralCulture)
            throw new ArgumentException("Culture is a neutral culture.", nameof(cultureName));

        var id = cultureName switch
        {
            IranCultureName => ApplicationCurrentRegion.Iran,
            IraqCultureName => ApplicationCurrentRegion.Iraq,
            _ => throw new InvalidOperationException("Unmatched region id.")
        };

        cachedRegion = new LocalRegion
        {
            Id = id,
            Culture = culture,
            Region = new RegionInfo(cultureName),
        };
        Cached.TryAdd(cultureName, cachedRegion);
        return cachedRegion;
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="LocalRegion" />.
    /// </summary>
    /// <param name="id">The <see cref="ApplicationCurrentRegion" /> identifier for the region.</param>
    /// <returns>A <see cref="LocalRegion" /> object</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="id" /> is a neutral culture.</exception>
    public static LocalRegion GetOrCreate(ApplicationCurrentRegion id)
    {
        if (id is ApplicationCurrentRegion.Global)
            throw new ArgumentException("Global region is not supported.", nameof(id));

        var cultureName = id switch
        {
            ApplicationCurrentRegion.Iran => IranCultureName,
            ApplicationCurrentRegion.Iraq => IraqCultureName,
            _ => throw new InvalidOperationException("Unmatched region id.")
        };

        if (Cached.TryGetValue(cultureName, out var cachedRegion))
            return cachedRegion;

        var culture = CultureInfo.GetCultureInfo(cultureName);
        if (culture.IsNeutralCulture)
            throw new ArgumentException("Culture is a neutral culture.", nameof(id));

        cachedRegion = new LocalRegion
        {
            Id = id,
            Culture = culture,
            Region = new RegionInfo(cultureName),
        };
        Cached.TryAdd(cultureName, cachedRegion);
        return cachedRegion;
    }

    /// <summary>
    ///     Starts a new scope for the current region. This will set the current region to the specified region. This method is useful when you want to change the region for a specific scope (ASP.NET Core).
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="region" /> is null.</exception>
    /// <remarks>Don't forget to call <see cref="EndScope" /> to end the scope, when you are done with the region. Otherwise, the region will remain the same.</remarks>
    public static void StartScope(LocalRegion region)
    {
        if (region == null)
            throw new ArgumentNullException(nameof(region));

        lock (SyncRoot)
        {
            CurrentLocal.Value = region;
        }
    }

    /// <summary>
    ///     Ends the current region scope. This will set the current region to default.
    /// </summary>
    public static void EndScope()
    {
        lock (SyncRoot)
        {
            CurrentLocal.Value = null;
        }
    }

    public object? GetFormat(Type? formatType)
    {
        if (formatType == typeof(DateTimeFormatInfo)) return Culture.DateTimeFormat;
        if (formatType == typeof(NumberFormatInfo)) return Culture.NumberFormat;
        return null;
    }

    public bool Equals(LocalRegion? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Culture.Equals(other.Culture);
    }

    public bool Equals(IRegion? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Culture.Equals(other.Culture);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LocalRegion)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Culture, Region);
    }

    public static bool operator ==(LocalRegion? left, LocalRegion? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LocalRegion? left, LocalRegion? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return GetDisplayName();
    }

    private string GetDebuggerDisplay()
    {
        return ToString();
    }

    public class LocalRegionJsonConverter : JsonConverter<LocalRegion>
    {
        public override LocalRegion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var cultureName = reader.GetString();
            return LocalRegion.GetOrCreate(cultureName);
        }

        public override void Write(Utf8JsonWriter writer, LocalRegion value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Culture.Name);
        }
    }
}

public interface IRegion : IEquatable<IRegion>, IEquatable<LocalRegion>
{
    /// <inheritdoc cref="CultureInfo.Name" />
    string Name { get; }

    /// <summary>
    ///     Gets the culture of the region.
    /// </summary>
    CultureInfo Culture { get; }

    /// <summary>
    /// Gets the identifier of the region.
    /// </summary>
    public ApplicationCurrentRegion Id { get; }

    /// <summary>
    ///     Gets the info of the region.
    /// </summary>
    RegionInfo Region { get; }

    /// <inheritdoc cref="TextInfo.IsRightToLeft" />
    bool IsRightToLeft { get; }

    /// <summary>
    ///     Returns the display name of the region.
    /// </summary>
    /// <returns>A string that represents the display name of the region.</returns>
    string GetDisplayName();
}

public enum ApplicationCurrentRegion
{
    Global, // Global region
    Iran, // Iran region
    Iraq // Iraq region
}