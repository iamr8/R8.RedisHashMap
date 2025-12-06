using System.Collections;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using R8.RedisHashMap.Test.Map;

namespace R8.RedisHashMap.Test.Objects;

/// <summary>
///     A collection of localized values.
/// </summary>
[JsonConverter(typeof(LocalizedValueCollectionJsonConverter))]
public class LocalizedValueCollection : IReadOnlyDictionary<CultureInfo, string>
{
    private readonly Dictionary<CultureInfo, string> _dictionary;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalizedValueCollection" /> class.
    /// </summary>
    public LocalizedValueCollection() : this(new Dictionary<CultureInfo, string>())
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalizedValueCollection" /> class.
    /// </summary>
    /// <param name="dictionary">A dictionary of localized values.</param>
    public LocalizedValueCollection(Dictionary<CultureInfo, string> dictionary)
    {
        _dictionary = dictionary;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalizedValueCollection" /> class with all supported regions.
    /// </summary>
    /// <param name="localizedValueCollection">A <see cref="LocalizedValueCollection" /> instance to copy from.</param>
    /// <remarks>This constructor is used to create a new instance of <see cref="LocalizedValueCollection" /> with all supported regions, by copying the values from another instance.</remarks>
    public LocalizedValueCollection(LocalizedValueCollection localizedValueCollection) : this(LocalRegion.SupportedRegions.ToDictionary(
        region => region.Culture,
        region => localizedValueCollection._dictionary.GetValueOrDefault(region.Culture)))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalizedValueCollection" /> class.
    /// </summary>
    /// <param name="dictionary">A dictionary of localized values.</param>
    public LocalizedValueCollection(Dictionary<string, string> dictionary) : this(dictionary.ToDictionary(x => new CultureInfo(x.Key), x => x.Value))
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="LocalizedValueCollection" /> class.
    /// </summary>
    /// <param name="str">A string value. If the string starts with '{', it will be deserialized, otherwise it will be used as the default value for <see cref="LocalRegion.Iran" />.</param>
    public LocalizedValueCollection(string str) : this(Deserialize(str)._dictionary)
    {
    }

    /// <summary>
    ///     Gets a value indicating whether the dictionary is empty.
    /// </summary>
    public bool IsEmpty => _dictionary.Count == 0 || _dictionary.All(x => string.IsNullOrWhiteSpace(x.Value));

    public string CustomText { get; set; }

    public string CustomText2 { get; set; }

    private readonly int _customText;

    /// <summary>
    ///     Gets or sets the localized value for the specified region.
    /// </summary>
    /// <param name="region">A region.</param>
    /// <param name="fallback">A fallback value to return if the value is not found WHEN getting the value.</param>
    /// <exception cref="ArgumentNullException">The region is null.</exception>
    public string this[IRegion region, string fallback = ""]
    {
        get
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            return this[region.Culture, fallback];
        }
        set
        {
            if (region == null)
                throw new ArgumentNullException(nameof(region));

            if (_dictionary.ContainsKey(region.Culture))
                _dictionary[region.Culture] = value;
            else if (!string.IsNullOrWhiteSpace(value))
                _dictionary.Add(region.Culture, value);
        }
    }

    public int this[int index] => index;

    public int Count => _dictionary.Count;

    string IReadOnlyDictionary<CultureInfo, string>.this[CultureInfo key] => this[key];

    /// <summary>
    ///     Gets or sets the localized value for the specified culture.
    /// </summary>
    /// <param name="culture">A culture.</param>
    /// <param name="fallback">A fallback value to return if the value is not found WHEN getting the value.</param>
    public string this[CultureInfo culture, string fallback = ""]
    {
        get => _dictionary.GetValueOrDefault(culture) ?? fallback;
        set
        {
            if (_dictionary.ContainsKey(culture))
                _dictionary[culture] = value;
            else if (!string.IsNullOrWhiteSpace(value))
                _dictionary.Add(culture, value);
        }
    }

    public IEnumerable<CultureInfo> Keys => _dictionary.Keys;
    public IEnumerable<string> Values => _dictionary.Values;

    public bool ContainsKey(CultureInfo key)
    {
        return _dictionary.ContainsKey(key);
    }

    public bool TryGetValue(CultureInfo key, out string value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    public IEnumerator<KeyValuePair<CultureInfo, string>> GetEnumerator()
    {
        return _dictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private static JsonSerializerOptions SerializerOptions
    {
        get
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JsonCultureToStringConverter());
            options.Converters.Add(new JsonRegionInfoToStringConverter());
            options.AddContext<LocalizedValueCollectionJsonContext>();

            return options;
        }
    }

    /// <summary>
    ///     Serializes the current instance to a json string.
    /// </summary>
    /// <returns>A json string.</returns>
    public string Serialize()
    {
        return JsonSerializer.Serialize(this, SerializerOptions);
    }

    /// <summary>
    ///     Deserializes the specified value to a <see cref="LocalizedValueCollection" />.
    /// </summary>
    /// <param name="str">A string value. If the string starts with '{', it will be deserialized, otherwise it will be used as the default value for <see cref="LocalRegion.Iran" />.</param>
    /// <returns>A <see cref="LocalizedValueCollection" /> instance.</returns>
    public static LocalizedValueCollection Deserialize(string str)
    {
        if (string.IsNullOrEmpty(str))
            return new LocalizedValueCollection();

        if (str.StartsWith('{'))
            return JsonSerializer.Deserialize<LocalizedValueCollection>(str, SerializerOptions);

        return new LocalizedValueCollection(new Dictionary<CultureInfo, string>
        {
            { LocalRegion.Iran.Culture, str }
        });
    }

    /// <summary>
    ///     Returns a dictionary of localized values.
    /// </summary>
    /// <returns>A dictionary of localized values.</returns>
    public Dictionary<CultureInfo, string> AsDictionary()
    {
        return _dictionary;
    }

    /// <summary>
    ///     Returns a new instance of <see cref="LocalizedValueCollection" /> with all supported region keys defined and all values set to null.
    /// </summary>
    /// <returns>An instance of <see cref="LocalizedValueCollection" /> object.</returns>
    public static LocalizedValueCollection Empty()
    {
        return new LocalizedValueCollection(LocalRegion.SupportedRegions.ToDictionary(x => x.Culture, x => (string)null));
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;

        if (obj is string str)
            return this[LocalRegion.Current] == str;

        if (obj is LocalizedValueCollection lvc)
            return this.SequenceEqual(lvc);

        return false;
    }

    public override int GetHashCode()
    {
        return _dictionary != null ? _dictionary.GetHashCode() : 0;
    }

    public static bool operator ==(LocalizedValueCollection left, LocalizedValueCollection right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LocalizedValueCollection left, LocalizedValueCollection right)
    {
        return !Equals(left, right);
    }

    public class LocalizedValueCollectionJsonConverter : JsonConverter<LocalizedValueCollection>
    {
        public override LocalizedValueCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<CultureInfo, string>();

            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException("The json must start with an object.");

            while (reader.Read())
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var key = reader.GetString();
                    var culture = CultureInfo.GetCultureInfo(key);
                    reader.Read();
                    var value = reader.GetString();
                    dictionary.Add(culture, value);
                }
                else if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }
                else
                {
                    throw new JsonException("Invalid json format.");
                }

            return new LocalizedValueCollection(dictionary);
        }

        public override void Write(Utf8JsonWriter writer, LocalizedValueCollection value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            foreach (var (culture, localizedValue) in value)
            {
                if (string.IsNullOrWhiteSpace(localizedValue) ||
                    culture.Equals(CultureInfo.InvariantCulture) ||
                    !LocalRegion.SupportedRegions.Any(c => c.Culture.Equals(culture)))
                    continue;

                value[culture] = localizedValue.Trim();
                if (localizedValue.StartsWith("http://") || localizedValue.StartsWith("https://"))
                    value[culture] = localizedValue.Urlify();

                writer.WritePropertyName(culture.Name);
                writer.WriteStringValue(localizedValue.Trim());
            }

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// A mechanism to fill the empty values with the values of the fallback culture.
    /// </summary>
    /// <param name="fallbackCulture"></param>
    public void FillByFallback(CultureInfo fallbackCulture)
    {
        foreach (var region in LocalRegion.SupportedRegions)
        {
            if (this.TryGetValue(fallbackCulture, out var currentDesc) && !string.IsNullOrWhiteSpace(currentDesc) && !this.ContainsKey(region.Culture))
                this[region.Culture] = currentDesc;
        }
    }
}