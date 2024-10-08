using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services;

public abstract class PoiServiceBase : IPoiService
{
    public abstract Task DatabaseSeed(CancellationToken ct = default);
    public abstract Task<string?> FindCountryName(double longitude, double latitude, CancellationToken ct = default);
    public abstract IAsyncEnumerable<PoiResponse> FindPoisDistance(PoiRequest poiRequest, CancellationToken ct = default);
    public abstract IAsyncEnumerable<PoiResponse> FindPoisWithin(PoiRequestWithin poiRequest, CancellationToken ct = default);

    protected static async Task<CountrySeedRecord[]> getCountrySeedRecords(string countryName, CancellationToken ct = default)
    {
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Country.json"));

        CountrySeedRecord[]? seedRecords = await JsonSerializer.DeserializeAsync<CountrySeedRecord[]?>(fileStream, cancellationToken: ct);

        return seedRecords ?? [];
    }

    protected static async IAsyncEnumerable<PoiSeedRecord> getPoiSeedRecords(string countryName, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Poi.json"));

        IAsyncEnumerable<PoiSeedRecord?> asyncEnum = JsonSerializer.DeserializeAsyncEnumerable<PoiSeedRecord?>(fileStream, cancellationToken: ct);

        // This internal iteration is needed because simply returning also closes the file stream
        await foreach (PoiSeedRecord? poiSeedRecord in asyncEnum)
        {
            yield return poiSeedRecord!;
        }
    }

    protected static T[] toPolygonCoordinates<T>(PoiRequestWithin poiRequest, Func<(double Lng, double Lat), T> transform)
    {
        return toPolygonCoordinates(poiRequest).Select(coordinate => transform(coordinate)).ToArray();
    }

    private static (double Lng, double Lat)[] toPolygonCoordinates(PoiRequestWithin poiRequest)
    {
        return
        [
            (poiRequest.NWLng, poiRequest.NWLat),
            (poiRequest.SWLng, poiRequest.SWLat),
            (poiRequest.SELng, poiRequest.SELat),
            (poiRequest.NELng, poiRequest.NELat),
            (poiRequest.NWLng, poiRequest.NWLat)
        ];
    }
}