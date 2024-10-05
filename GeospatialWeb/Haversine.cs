namespace GeospatialWeb;

public static class Haversine
{
    public static double Distance(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6371000; // Radius of the Earth in meters

        double d_Lat = toRadians(lat2 - lat1);
        double d_Lng = toRadians(lng2 - lng1);

        lat1 = toRadians(lat1);
        lat2 = toRadians(lat2);

        double a = Math.Sin(d_Lat / 2) * Math.Sin(d_Lat / 2) +
                   Math.Cos(lat1) * Math.Cos(lat2) *
                   Math.Sin(d_Lng / 2) * Math.Sin(d_Lng / 2);

        double c = 2 * Math.Asin(Math.Sqrt(a));

        return R * c;
    }

    private static double toRadians(double angleInDegrees)
    {
        return angleInDegrees * (Math.PI / 180.0);
    }
}