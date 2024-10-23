using GeospatialWeb.Services.MongoDB.Models;
using MongoDB.Driver;

namespace GeospatialWeb.Services.MongoDB;

public interface IMongoDbContext
{
    IMongoDatabase DataBase { get; }
    IMongoCollection<Country> CountryCollection { get; }
    IMongoCollection<PoiData> PoiDataCollection { get; }

    Task<bool> IsDatabaseExists(CancellationToken ct);
}

public sealed class MongoDbContext(IMongoClient _mongoClient) : IMongoDbContext
{
    private const string _databaseName = "PlayingWith_Geospatial";

    public IMongoDatabase DataBase => _mongoClient.GetDatabase(_databaseName);

    public IMongoCollection<PoiData> PoiDataCollection => DataBase.GetCollection<PoiData>(PoiData.CollectionName);

    public IMongoCollection<Country> CountryCollection => DataBase.GetCollection<Country>(Country.CollectionName);

    public async Task<bool> IsDatabaseExists(CancellationToken ct)
    {
        using IAsyncCursor<string> asyncCursor = await _mongoClient.ListDatabaseNamesAsync(ct);

        List<string> databaseNames = await asyncCursor.ToListAsync(ct);

        return databaseNames.Contains(_databaseName);
    }
}
