using GeospatialWeb.Services.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services.EntityFramework;

public sealed class PoiServiceEF(ApplicationDbContext _dbContext) : IPoiService
{
    public async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var point = new Point(poiRequest.Lng, poiRequest.Lat);

        var asyncEnum = _dbContext.POIs
            .Where(poi => poi.Country.GeoFence.Contains(point) &&
                          poi.Location.Distance(point) <= poiRequest.Distance)
            .AsAsyncEnumerable();

        await foreach (PoiData? poi in asyncEnum)
        {
            // Console.WriteLine(poi.Location.Distance(point));

            yield return new PoiResponse(poi.Id, poi.Name, poi.Category, Lat: poi.Location.Y, Lng: poi.Location.X);
        }
    }

    public async Task<string?> FindCountryName(double latitude, double longitude, CancellationToken ct = default)
    {
        // SRID: 4326 is a global standard, which corresponds to the WGS 84 coordinate system
        // NetTopologySuite ignores SRID values during operations. It assumes a planar coordinate system
        var point = new Point(longitude, latitude); // { SRID = 4326 };

        return await _dbContext.Countries
            .Where(c => c.GeoFence.Contains(point))
            .Select(c => c.Name)
            .FirstOrDefaultAsync(ct);
    }

    public async Task DatabaseSeed(CancellationToken ct = default)
    {
        await _dbContext.Database.EnsureCreatedAsync(ct);

        bool isDatabaseExists = await _dbContext.Countries.AnyAsync(ct);

        if (isDatabaseExists)
        {
            return;
        }

        Guid countryFrance  = await databaseSeed_Country("France",  ct);
        Guid countrySpain   = await databaseSeed_Country("Spain",   ct);
        Guid countryIreland = await databaseSeed_Country("Ireland", ct);

        await databaseSeed_POIs(countryFrance,  "France",  ct);
        await databaseSeed_POIs(countrySpain,   "Spain",   ct);
        await databaseSeed_POIs(countryIreland, "Ireland", ct);
    }

    private async Task<Guid> databaseSeed_Country(string countryName, CancellationToken ct = default)
    {
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Country.json"));

        CountrySeedRecord[]? seedRecords = await JsonSerializer.DeserializeAsync<CountrySeedRecord[]?>(fileStream, cancellationToken: ct);

        Coordinate[] coordinates = seedRecords!.Select(c => new Coordinate(c.Lng, c.Lat)).ToArray();

        var country = new Country
        {
            Id       = Guid.NewGuid(),
            Name     = countryName,
            GeoFence = new Polygon(new LinearRing(coordinates))
        };

        await _dbContext.Countries.AddAsync(country, ct);

        await _dbContext.SaveChangesAsync(ct);

        return country.Id;
    }

    private async Task databaseSeed_POIs(Guid countryId, string countryName, CancellationToken ct = default)
    {
        using FileStream fileStream = File.OpenRead(Path.Combine("SeedData", $"Seed_{countryName}_Poi.json"));

        var asyncEnum = JsonSerializer.DeserializeAsyncEnumerable<PoiSeedRecord?>(fileStream, cancellationToken: ct);

        await foreach (PoiSeedRecord? poiSeedRecord in asyncEnum)
        {
            var poiData = new PoiData
            {
                Id        = Guid.NewGuid(),
                CountryId = countryId,
                Category  = poiSeedRecord!.Category,
                Name      = poiSeedRecord.Name,
                Location  = new Point(poiSeedRecord.Lng, poiSeedRecord.Lat)
            };

            await _dbContext.POIs.AddAsync(poiData, ct);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}