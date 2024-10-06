using NetTopologySuite.Geometries;

namespace GeospatialWeb.Services.EntityFramework.Models;

public sealed class Country
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Polygon GeoFence { get; set; } = default!;
}
