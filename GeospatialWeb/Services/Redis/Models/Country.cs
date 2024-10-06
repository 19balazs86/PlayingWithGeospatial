using GeospatialWeb.Geography;
using System.Text.Json.Serialization;

namespace GeospatialWeb.Services.Redis.Models;

public sealed class Country
{
    public const string CollectionName = "Countries";

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public GeoPolygon GeoFence { get; set; }

    public string GetIdKey()
    {
        return $"{CollectionName}:{Id}";
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(Country))]
public partial class CountrySerializationContext : JsonSerializerContext { }