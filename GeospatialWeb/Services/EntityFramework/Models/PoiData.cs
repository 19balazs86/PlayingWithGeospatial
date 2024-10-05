using NetTopologySuite.Geometries;

namespace GeospatialWeb.Services.EntityFramework.Models;

public sealed class PoiData
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public Point Location { get; set; } = default!;

    public Guid CountryId { get; set; }

    public Country Country { get; set; } = default!;
}
