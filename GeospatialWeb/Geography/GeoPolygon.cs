namespace GeospatialWeb.Geography;

public readonly record struct GeoPolygon
{
    public readonly IReadOnlyList<GeoLocation> Points { get; init; }

    public GeoPolygon(IReadOnlyList<GeoLocation> points)
    {
        if (points.Count < 3)
        {
            throw new ArgumentException("A polygon must have at least 3 points.", nameof(points));
        }

        if (!points[0].Equals(points[^1]))
        {
            throw new ArgumentException("The first and last (closing) points of the polygon must be the same.", nameof(points));
        }

        Points = points;
    }

    public static GeoPolygon Create(IEnumerable<GeoLocation> points)
    {
        return new GeoPolygon(points.ToList());
    }
}