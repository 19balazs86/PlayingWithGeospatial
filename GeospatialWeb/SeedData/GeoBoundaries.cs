//using GeoJSON.Net.Feature;
//using GeoJSON.Net.Geometry;
//using System.Text.Json;

//namespace ConsoleAppTest;

//// Install-Package GeoJSON.Net

//public static class GeoBoundaries
//{
//    private const double _expectedCoordinates = 200.0;

//    public static async Task CreateCountryPolygon(string inputGeoJsonPath, string outputCoordinatesPath)
//    {
//        double[][] countryCoordinates = getCountryCoordinates(inputGeoJsonPath);

//        double percentage = 1 - (_expectedCoordinates / countryCoordinates.Length);

//        countryCoordinates = reduceCoordinatesByPercentage(countryCoordinates, percentage);

//        using FileStream fileStream = File.OpenWrite(outputCoordinatesPath);

//        await JsonSerializer.SerializeAsync(fileStream, countryCoordinates);
//    }

//    private static double[][] getCountryCoordinates(string inputGeoJsonPath)
//    {
//        string geoJsonText = File.ReadAllText(inputGeoJsonPath);

//        FeatureCollection? featureCollection = Newtonsoft.Json.JsonConvert.DeserializeObject<FeatureCollection>(geoJsonText);

//        if (featureCollection is null || featureCollection.Features.Count != 1)
//        {
//            throw new InvalidOperationException("Invalid FeatureCollection. Make sure the input file is correct");
//        }

//        MultiPolygon? multiPolygon = featureCollection.Features[0].Geometry as MultiPolygon;

//        if (multiPolygon is null)
//        {
//            throw new NullReferenceException("No MultiPolygon was found in the FeatureCollection");
//        }

//        // Find the biggest polygon. Leave out the small ones
//        Polygon? biggestPolygon = null;
//        int numberOfCoordinates = 0;

//        foreach (Polygon polygon in multiPolygon.Coordinates)
//        {
//            int numberOfCoords = polygon.Coordinates[0].Coordinates.Count;

//            if (numberOfCoords > numberOfCoordinates)
//            {
//                numberOfCoordinates = numberOfCoords;
//                biggestPolygon = polygon;
//            }
//        }

//        if (biggestPolygon is null)
//        {
//            throw new NullReferenceException("Biggest Polygon was not found");
//        }

//        var selectedCoords = new List<double[]>(numberOfCoordinates);

//        foreach (IPosition position in biggestPolygon.Coordinates[0].Coordinates)
//        {
//            selectedCoords.Add([position.Longitude, position.Latitude]);
//        }

//        return [..selectedCoords];
//    }

//    private static double[][] reduceCoordinatesByPercentage(in double[][] coordinates, double percentage)
//    {
//        if (coordinates == null || coordinates.Length < 2)
//        {
//            throw new ArgumentException("The coordinates array must contain at least two points.");
//        }

//        int totalPoints    = coordinates.Length;
//        int pointsToRemove = (int)(totalPoints * percentage);

//        // Ensure we do not remove more points than available
//        if (pointsToRemove >= totalPoints - 2)
//        {
//            throw new ArgumentException("The percentage is too high; not enough points to retain.");
//        }

//        int newCount = totalPoints - pointsToRemove;

//        double[][] reducedCoordinates = new double[newCount][];

//        // Preserve the first coordinate
//        reducedCoordinates[0] = [coordinates[0][0], coordinates[0][1]];

//        // Add coordinates evenly spaced out while ensuring first and last are preserved
//        int step = (totalPoints - 2) / (newCount - 2); // Calculate step to take

//        for (int i = 1; i < newCount - 1; i++)
//        {
//            reducedCoordinates[i] = new double[2];

//            reducedCoordinates[i][0] = coordinates[1 + (i - 1) * step][0];
//            reducedCoordinates[i][1] = coordinates[1 + (i - 1) * step][1];
//        }

//        // Preserve the last coordinate
//        reducedCoordinates[newCount - 1] = [coordinates[totalPoints - 1][0], coordinates[totalPoints - 1][1]];

//        return reducedCoordinates;
//    }
//}
