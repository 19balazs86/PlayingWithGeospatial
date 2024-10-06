using GeospatialWeb.Services.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GeospatialWeb.Services.EntityFramework;

public sealed class PoiServiceEF(ApplicationDbContext _dbContext) : IPoiService
{
    private static readonly GeometryFactory _geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(ApplicationDbContext.SRID);

    public async IAsyncEnumerable<PoiResponse> FindPOIs(PoiRequest poiRequest, [EnumeratorCancellation] CancellationToken ct = default)
    {
        string? countryName = await FindCountryName(poiRequest.Lat, poiRequest.Lng, ct);

        if (string.IsNullOrEmpty(countryName))
        {
            yield break;
        }

        var point = _geometryFactory.CreatePoint(poiRequest.Lng, poiRequest.Lat);

        Guid countryId = Guid.Parse(countryName);

        var asyncEnum = _dbContext.POIs
            .Where(poi => poi.CountryId == countryId && poi.Location.Distance(point) <= poiRequest.Distance)
            .Select(poi => new { Entity = poi, Distance = poi.Location.Distance(point) })
            .AsAsyncEnumerable();

        await foreach (var item in asyncEnum)
        {
            PoiData poi = item.Entity;

            yield return new PoiResponse(poi.Id, poi.Name, poi.Category, Lat: poi.Location.Y, Lng: poi.Location.X, item.Distance);
        }
    }

    public async Task<string?> FindCountryName(double latitude, double longitude, CancellationToken ct = default)
    {
        // This does not work: ST_Contains does not work with geography type
        // return _dbContext.Countries.Where(c => c.GeoFence.Contains(point)).FirstOrDefaultAsync(ct);

        string sql =
            $$"""
            SELECT "Id"::text AS "Value"
            FROM "Countries"
            WHERE ST_Contains("GeoFence"::geometry, ST_SetSRID(ST_MakePoint({0}, {1}), {{ApplicationDbContext.SRID}}))
            """;

        FormattableString formattedSql = FormattableStringFactory.Create(sql, longitude, latitude);

        return await _dbContext.Database.SqlQuery<string?>(formattedSql).FirstOrDefaultAsync(ct);
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
            GeoFence = _geometryFactory.CreatePolygon(coordinates)
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
                Location  = _geometryFactory.CreatePoint(poiSeedRecord.Lng, poiSeedRecord.Lat)
            };

            await _dbContext.POIs.AddAsync(poiData, ct);
        }

        await _dbContext.SaveChangesAsync(ct);
    }
}

file static class GeometryFactoryExtension
{
    public static Point CreatePoint(this GeometryFactory factory, double longitude, double latitude)
    {
        return factory.CreatePoint(new Coordinate(longitude, latitude));
    }

    public static Polygon CreatePolygon(this GeometryFactory factory, Coordinate[] coordinates)
    {
        return factory.CreatePolygon(factory.CreateLinearRing(coordinates));
    }
}
