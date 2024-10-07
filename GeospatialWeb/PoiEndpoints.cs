using GeospatialWeb.Services;

namespace GeospatialWeb;

public readonly record struct PoiRequest(double Lng, double Lat, double Distance);

public sealed record PoiResponse(Guid Id, string Name, string Category, double Lng, double Lat, double Distance);

public static class PoiEndpoints
{
    public static void MapPoiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/poi", handle_GetPoi);
    }

    private static IAsyncEnumerable<PoiResponse> handle_GetPoi([AsParameters] PoiRequest poiRequest, IPoiService poiService, CancellationToken ct)
    {
        return poiService.FindPOIs(poiRequest, ct);
    }
}
