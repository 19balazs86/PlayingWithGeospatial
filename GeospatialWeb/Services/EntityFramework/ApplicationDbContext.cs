using GeospatialWeb.Services.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace GeospatialWeb.Services.EntityFramework;

public sealed class ApplicationDbContext(DbContextOptions options) : DbContext(options)
{
    public const int SRID = 4326;

    public DbSet<Country> Countries { get; set; }

    public DbSet<PoiData> POIs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        /* Some notes using MS SQL
         * - I could not add indexes with HasMethod("SPATIAL")
         * - But I was able to add the indexes using SSMS by selecting the field, a new button for Spatial indexes appeared
         * - Only supports the GEOGRAPHY type and does not have specialized types for Polygon or Point
         * - Instead of poi.Location.CoveredBy, you need to use Within | GeoFence.Covers -> GeoFence.Contains
         * - Example: SELECT geography::Point(47.4882, 19.1043, 4326).STDistance(geography::Point(46.2485, 20.1517, 4326))
        */

        // If you are using EF 6.0, you need to make sure that the PostGIS extension is installed in your database (later versions do this automatically)
        // modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Country>()
            .HasIndex(c => c.GeoFence)
            .HasMethod("gist"); // Use the GiST index method for spatial data

        modelBuilder.Entity<PoiData>()
            .HasIndex(p => p.Location)
            .HasMethod("gist");

        modelBuilder.Entity<Country>(entity =>
        {
            entity.Property(e => e.GeoFence)
                  .HasColumnType($"GEOGRAPHY(Polygon, {SRID})"); // Needs to be defined as GEOGRAPHY, otherwise it will be GEOMETRY
        });

        modelBuilder.Entity<PoiData>(entity =>
        {
            entity.Property(e => e.Location)
                  .HasColumnType($"GEOGRAPHY(Point, {SRID})");
        });
    }
}
