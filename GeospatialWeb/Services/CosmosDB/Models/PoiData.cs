using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;
using Newtonsoft.Json;

namespace GeospatialWeb.Services.CosmosDB.Models;

public sealed class PoiData
{
    public const string ContainerId      = "POIs";
    public const string PartitionKeyPath = $"/{nameof(CountryName)}";
    public const string SpatialPath      = $"/{nameof(Location)}/*";

    [JsonIgnore]
    public PartitionKey PartitionKey => new PartitionKey(CountryName);

    [JsonProperty("id")]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public Point Location { get; set; } = default!;
}
