using GeospatialWeb.Geography;
using System.Text.Json.Serialization;

namespace GeospatialWeb.Services.Redis.Models;

public sealed class PoiData
{
    private const string _geoPoisKey = "GeoPois";

    private const string _collectionName = "POIs";

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public GeoLocation Location { get; set; }

    public string GetIdKey(string countryName)
    {
        return $"{_collectionName}:{countryName}:{Id}";
    }

    public static string GetIdKey(string countryName, string idGuidString)
    {
        return $"{_collectionName}:{countryName}:{idGuidString}";
    }

    public static string GetGeoIdKey(string countryName)
    {
        return $"{_geoPoisKey}:{countryName}";
    }
}

[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(PoiData))]
public sealed partial class PoiDataSerializationContext : JsonSerializerContext;
