namespace GeospatialWeb.Services;

public interface IPoiService
{
    IAsyncEnumerable<PoiResponse> FindPoisDistance(PoiRequest poiRequest, CancellationToken ct = default);

    IAsyncEnumerable<PoiResponse> FindPoisWithin(PoiRequestWithin poiRequest, CancellationToken ct = default);

    /// <summary>
    /// To keep things simple, I used CountryName in PoiData instead of CountryId when seeding the database
    /// </summary>
    Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default);

    Task DatabaseSeed(CancellationToken ct = default);
}
