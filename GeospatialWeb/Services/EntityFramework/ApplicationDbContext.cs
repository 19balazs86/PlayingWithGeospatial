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
