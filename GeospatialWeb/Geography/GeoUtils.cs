namespace GeospatialWeb.Geography;

public static class GeoUtils
{
    public static double HaversineDistance(double lng1, double lat1, double lng2, double lat2)
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

        return Math.Round(R * c, 4);
    }

    // Ray-casting algorithm
    public static bool IsPointInPolygon(in GeoLocation point, in GeoPolygon polygon)
    {
        int n = polygon.Points.Count;

        bool inside = false;

        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = polygon.Points[i].Lng;
            double xj = polygon.Points[j].Lng;

            double yi = polygon.Points[i].Lat;
            double yj = polygon.Points[j].Lat;

            bool intersect = ((yi > point.Lat) != (yj > point.Lat)) && (point.Lng < (xj - xi) * (point.Lat - yi) / (yj - yi) + xi);

            if (intersect)
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static double toRadians(double angleInDegrees)
    {
        return angleInDegrees * (Math.PI / 180.0);
    }
}