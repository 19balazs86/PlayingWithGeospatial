# Playing with Geospatial location

- In this repository, I had some fun working with geospatial location
- The application is quite simple: just a map for Points of Interest
- I used LeafletJS with OpenStreetMap
- For database solutions, you can choose from various implementations in [Program.cs](GeospatialWeb/Program.cs)
  - Azure Cosmos | Mongo | Redis | Postgres with EF

## Resources

#### 🍃 `Leaflet`

- [Leaflet.js](https://leafletjs.com)
- [LeafletForBlazor](https://github.com/ichim/LeafletForBlazor-NuGet) 👤*Laurentiu Ichim*
- [Routing machine](https://www.liedman.net/leaflet-routing-machine)

#### 🗃️ `Cosmos`

- [Geospatial data](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/geospatial) 📚*MS-Learn*
- [Index and query location data](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-geospatial-index-query) 📚*MS-Learn*
- [Playing with Azure CosmosDB](https://github.com/19balazs86/AzureCosmosDB) 👤*My repository*

#### 🗃️ `Mongo`

- [Search Geospatially](https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/geo)
- [Geospatial indexes](https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/indexes/#geospatial-indexes)
- [Playing with MongoDB](https://github.com/19balazs86/PlayingWithMongoDB) 👤*My repository*

#### 🗃️ `Redis`

- [Commands - Geo](https://redis.io/docs/latest/commands/?group=geo)

#### 🗃️ `Postgres`

- [NetTopologySuite - API Documentation](https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.html)
- [Entity Framework - Spatial mapping](https://www.npgsql.org/efcore/mapping/nts.html) 📓*Npgsql*
- [Spatial type mapping](https://www.npgsql.org/doc/types/nts.html) 📓*Npgsql*
- [Spatial data](https://learn.microsoft.com/en-us/ef/core/modeling/spatial) 📚*MS-learn*

#### ✨ `Miscellaneous`

- [OpenStreetMap](https://www.openstreetmap.org)
- [Turf.js](https://turfjs.org) - *Advanced geospatial analysis*
- [GeoJSON.io](https://geojson.io)
- [geoBoundaries](https://www.geoboundaries.org/index.html)

## Comparison summary
- **Redis**: Fast for in-memory operations but could be limited by memory for large datasets. Complex geospatial queries and Polygon types are not supported. Better for real-time applications with smaller datasets.
- **Cosmos**: Highly scalable and efficient for large, distributed datasets, complex geospatial queries.
- **Mongo**: Good balance of performance and scalability, complex geospatial queries.
- **Postgres**: Handles complex geospatial queries with robust performance. I had to use a raw SQL query, so working with geo types in Cosmos and Mongo is more convenient.

## Screen

![Screen](Screen.JPG)