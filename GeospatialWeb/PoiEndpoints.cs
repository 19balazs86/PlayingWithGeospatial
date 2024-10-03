using GeospatialWeb.Services;

namespace GeospatialWeb;

public readonly record struct PoiRequest(double Lat, double Lng, double Distance);

public sealed record PoiResponse(Guid Id, string Name, string Category, double Lat, double Lng);

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
