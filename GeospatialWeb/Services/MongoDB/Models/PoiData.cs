using MongoDB.Driver.GeoJsonObjectModel;

namespace GeospatialWeb.Services.MongoDB.Models;

public sealed class PoiData
{
    public const string CollectionName = "POIs";

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; } = default!;
}
