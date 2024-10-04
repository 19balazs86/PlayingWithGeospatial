using MongoDB.Driver.GeoJsonObjectModel;

namespace GeospatialWeb.Services.MongoDB.Models;

public sealed class Country
{
    public const string CollectionName = "Countries";

    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public GeoJsonPolygon<GeoJson2DGeographicCoordinates> GeoFence { get; set; } = default!;
}
