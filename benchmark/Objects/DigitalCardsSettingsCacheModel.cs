using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace R8.RedisHashMap.Test.Objects;

internal record PhysicalCardCacheModel
{
    public int Id { get; set; }
    public string Name { get; set; }

    public string Code { get; set; }
    public PhysicalCardType Type { get; set; }
    public int PublishYear { get; set; }
    public int SubType { get; set; }
    public string CardIdCode { get; set; }

    public LocalizedValueCollection VideoLink { get; set; }
    public LocalizedValueCollection Description { get; set; }
    public bool IsDeleted { get; set; }

    public int CollectionScore { get; set; }

    public PlayerCard Player { get; set; }
    public CoachCard Coach { get; set; }
    public ClubCard Club { get; set; }
    public VariationCard Variation { get; set; }
    public JournalCard Journal { get; set; }

    public class PlayingCard
    {
        public int LeagueId { get; set; }
        public int FlagId { get; set; }
        public int ClubId { get; set; }
    }

    public class PlayerCard : PlayingCard
    {
        public PhysicalCardRarityVariation Variation { get; set; }

        public int ShirtNumber { get; set; }
        public int Defence { get; set; }
        public int Control { get; set; }
        public int Attack { get; set; }
        public int Overall { get; set; }

        public int PostId { get; set; }
    }

    public class CoachCard : PlayingCard
    {
        public PhysicalCardRarityVariation Variation { get; set; }

        public int Motivation { get; set; }
        public int Strategy { get; set; }
        public int Leadership { get; set; }
        public int Overall { get; set; }

        public int TacticId { get; set; }
        public int SpecialAbilityId { get; set; }
        public int FormationId { get; set; }
    }

    public class ClubCard
    {
        public int PresetId { get; set; }
        public int NumberOfCards { get; set; }
        public List<ClubDefinition> Definitions { get; set; }

        public class ClubDefinition
        {
            public PhysicalClubCardDefinitionType Type { get; set; }
            public string Value { get; set; }
            public int NumberOfCards { get; set; }
        }
    }

    public class VariationCard
    {
        public PhysicalCardRarityVariation Variation { get; set; }
        public int NumberOfCards { get; set; }
        public int PublishYear { get; set; }
    }

    public class JournalCard
    {
    }
}

internal enum PhysicalCardRarityVariation
{
    Common, // Common
    Uncommon, // Uncommon
    Rare, // Rare
    Epic, // Epic
    Legendary, // Legendary
    Mythical, // Mythical
    Special, // Special (e.g., limited edition)
    Unique // Unique (one-of-a-kind cards)
}

internal enum PhysicalClubCardDefinitionType
{
    Name, // Club name
    Flag, // Club flag
    League, // League name
    Stadium, // Stadium name
    Manager, // Manager name
    Sponsor, // Sponsor name
    YearFounded, // Year founded
    Capacity, // Stadium capacity
    City, // City name
    Country, // Country name
}

internal enum PhysicalCardType
{
    P, // Player
    C, // Coach
    M, // Multi (Club or Variation)
    J // Journal
}

public class JsonCultureToStringConverter : JsonConverter<CultureInfo>
{
    public override CultureInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var twoIso = reader.GetString();
        if (string.IsNullOrEmpty(twoIso))
            return null;

        return new CultureInfo(twoIso);
    }

    public override void Write(Utf8JsonWriter writer, CultureInfo value, JsonSerializerOptions options)
    {
        if (value == null)
            return;

        var culture = value.Name;
        writer.WriteStringValue(culture);
    }
}

public class JsonRegionInfoToStringConverter : JsonConverter<RegionInfo>
{
    public override RegionInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var twoIso = reader.GetString();
        if (string.IsNullOrEmpty(twoIso))
            return null;

        return new RegionInfo(twoIso);
    }

    public override void Write(Utf8JsonWriter writer, RegionInfo value, JsonSerializerOptions options)
    {
        if (value == null)
            return;

        var culture = value.TwoLetterISORegionName;
        writer.WriteStringValue(culture);
    }
}

[JsonSerializable(typeof(LocalizedValueCollection))]
internal partial class LocalizedValueCollectionJsonContext : JsonSerializerContext
{
}