using GeospatialWeb.Services;

namespace GeospatialWeb;

public readonly record struct PoiRequest(double Lng, double Lat, double Distance);

public readonly record struct PoiRequestWithin(double CenterLng, double CenterLat, double NWLng, double NWLat, double SWLng, double SWLat, double SELng, double SELat, double NELng, double NELat);

public sealed record PoiResponse(Guid Id, string Name, string Category, double Lng, double Lat, double Distance);

public static class PoiEndpoints
{
    public static void MapPoiEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/pois/distance", handle_GetPoisDistance);
        app.MapGet("/api/pois/within",   handle_GetPoisWithin);
    }

    private static IAsyncEnumerable<PoiResponse> handle_GetPoisDistance([AsParameters] PoiRequest poiRequest, IPoiService poiService, CancellationToken ct)
    {
        return poiService.FindPOIs(poiRequest, ct);
    }

    private static IAsyncEnumerable<PoiResponse> handle_GetPoisWithin([AsParameters] PoiRequestWithin poiRequest, IPoiService poiService, CancellationToken ct)
    {
        return poiService.FindPoisWithin(poiRequest, ct);
    }
}
