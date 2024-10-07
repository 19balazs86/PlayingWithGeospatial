namespace GeospatialWeb.Services;

public interface IPoiService
{
    IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, CancellationToken ct = default);

    Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default);

    Task DatabaseSeed(CancellationToken ct = default);
}
