using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Spatial;
using Newtonsoft.Json;

namespace GeospatialWeb.Services.CosmosDB.Models;

public sealed class Country
{
    public const string ContainerId      = "Countries";
    public const string PartitionKeyPath = "/id";
    public const string SpatialPath      = $"/{nameof(GeoFence)}/*";

    [JsonIgnore]
    public PartitionKey PartitionKey => new PartitionKey(Id.ToString());

    [JsonProperty("id")]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public Polygon GeoFence { get; set; } = default!;
}
