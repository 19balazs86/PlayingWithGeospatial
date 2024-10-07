namespace GeospatialWeb.Geography;

public readonly record struct GeoLocation
{
    public double Lng { get; init; }
    public double Lat { get; init; }

    public GeoLocation(double lng, double lat)
    {
        if (!IsValidLongitude(lng))
        {
            throw new ArgumentOutOfRangeException(nameof(lng), "Longitude must be between -180 and 180 degrees.");
        }

        if (!IsValidLatitude(lat))
        {
            throw new ArgumentOutOfRangeException(nameof(lat), "Latitude must be between -90 and 90 degrees.");
        }

        Lng = lng;
        Lat = lat;
    }

    public static bool IsValidLatitude(double lat)
    {
        return lat is >= -90 and <= 90;
    }

    public static bool IsValidLongitude(double lng)
    {
        return lng is >= -180 and <= 180;
    }

    public void Deconstruct(out double lng, out double lat)
    {
        lng = Lng;
        lat = Lat;
    }
}
