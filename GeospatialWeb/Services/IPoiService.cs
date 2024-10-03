namespace GeospatialWeb.Services;

public interface IPoiService
{
    IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, CancellationToken ct = default);

    Task<string?> FindCountryName(double latitude, double longitude, CancellationToken ct = default);

    Task DatabaseSeed(CancellationToken ct = default);
}
