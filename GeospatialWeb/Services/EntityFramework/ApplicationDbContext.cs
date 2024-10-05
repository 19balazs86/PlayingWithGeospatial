using GeospatialWeb.Services.EntityFramework.Models;
using Microsoft.EntityFrameworkCore;

namespace GeospatialWeb.Services.EntityFramework;

public sealed class ApplicationDbContext : DbContext
{
    public DbSet<Country> Countries { get; set; }

    public DbSet<PoiData> POIs { get; set; }

    public ApplicationDbContext(DbContextOptions options) : base(options)
    {

    }

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
    }
}
