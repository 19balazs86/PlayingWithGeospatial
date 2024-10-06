namespace GeospatialWeb.Geography;

public readonly record struct GeoLocation
{
    public double Lat { get; init; }
    public double Lng { get; init; }

    public GeoLocation(double lat, double lng)
    {
        if (!IsValidLatitude(lat))
        {
            throw new ArgumentOutOfRangeException(nameof(lat), "Latitude must be between -90 and 90 degrees.");
        }

        if (!IsValidLongitude(lng))
        {
            throw new ArgumentOutOfRangeException(nameof(lng), "Longitude must be between -180 and 180 degrees.");
        }

        Lat = lat;
        Lng = lng;
    }

    public static bool IsValidLatitude(double lat)
    {
        return lat is >= -90 and <= 90;
    }

    public static bool IsValidLongitude(double lng)
    {
        return lng is >= -180 and <= 180;
    }

    public void Deconstruct(out double lat, out double lng)
    {
        lat = Lat;
        lng = Lng;
    }
}
