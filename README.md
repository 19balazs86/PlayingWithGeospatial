# Playing with Geospatial location

- In this repository, I had some fun working with geospatial location
- The application is quite simple: just a map for Points of Interest
- I used LeafletJS with OpenStreetMap
- For database solutions, you can choose from various implementations in [Program.cs](GeospatialWeb/Program.cs)
  - Azure CosmosDB | MongoDB | Redis | EntityFramework with PostgreSQL
  - Note: PostgreSQL with EF  was somewhat cumbersome, I had to use a raw query...

## Resources

- [Leaflet.js](https://leafletjs.com) 📓
  - [LeafletForBlazor](https://github.com/ichim/LeafletForBlazor-NuGet) 👤*Laurentiu Ichim*
  - [Leaflet routing machine](https://www.liedman.net/leaflet-routing-machine) 📓
- CosmosDB
  - [Geospatial data](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/query/geospatial) 📚*MS-Learn*
  - [Index and query location data](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/how-to-geospatial-index-query) 📚*MS-Learn*
  - [Playing with Azure CosmosDB](https://github.com/19balazs86/AzureCosmosDB) 👤*My repository*
- MongoDB
  - [Search Geospatially](https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/geo) 📓
  - [Geospatial indexes](https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/indexes/#geospatial-indexes) 📓
  - [Playing with MongoDB](https://github.com/19balazs86/PlayingWithMongoDB) 👤*My repository*
- Redis
  - [Commands - Geo](https://redis.io/docs/latest/commands/?group=geo)
- NetTopologySuite with EntityFramework
  - [NetTopologySuite - API Documentation](https://nettopologysuite.github.io/NetTopologySuite/api/NetTopologySuite.html) 📓
  - [Entity Framework - Spatial mapping](https://www.npgsql.org/efcore/mapping/nts.html) 📓*Npgsql*
  - [Spatial type mapping](https://www.npgsql.org/doc/types/nts.html) 📓*Npgsql*
  - [Spatial data](https://learn.microsoft.com/en-us/ef/core/modeling/spatial) 📚*MS-learn*
- Miscellaneous
  - [OpenStreetMap](https://www.openstreetmap.org) 📓
  - [Turf.js](https://turfjs.org) 📓 - *Advanced geospatial analysis*
  - [GeoJSON.io](https://geojson.io) 📓
  - [geoBoundaries](https://www.geoboundaries.org/index.html) 📓

## Comparison summary
- **Redis**: Fast for in-memory operations but limited by memory for large datasets. Better for real-time applications with smaller datasets.
- **CosmosDB**: Highly scalable and efficient for large, distributed datasets, complex geospatial queries.
- **MongoDB**: Good balance of performance and scalability, complex geospatial queries.
- **PostgreSQL**: Handles complex geospatial queries with robust performance and scalability, but working with geo types in CosmosDB and MongoDB is more efficient than in PostgreSQL.

## Screen

![Screen](Screen.JPG)