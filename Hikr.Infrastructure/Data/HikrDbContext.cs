using Hikr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hikr.Infrastructure.Data;

public class HikrDbContext : DbContext
{
    public HikrDbContext(DbContextOptions<HikrDbContext> options) : base(options)
    {
    }

    public DbSet<Routes> Routes { get; set; } = null!;
    public DbSet<Waypoints> Waypoints { get; set; } = null!;
    public DbSet<RouteWaypoint> RouteWaypoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PostGIS extension
        modelBuilder.HasPostgresExtension("postgis");

        modelBuilder.Entity<Routes>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Transportation).HasMaxLength(100);
            
            // PostGIS Geometry column
            entity.Property(e => e.Geometry).HasColumnType("geometry");
        });

        modelBuilder.Entity<Waypoints>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255);
            entity.Property(e => e.Type).HasMaxLength(50);
            
            // PostGIS Geometry column for POI
            entity.Property(e => e.Geometry).HasColumnType("geometry");
        });

        modelBuilder.Entity<RouteWaypoint>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(rw => rw.Route)
                .WithMany(r => r.RouteWaypoints)
                .HasForeignKey(rw => rw.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rw => rw.Waypoint)
                .WithMany(w => w.RouteWaypoints)
                .HasForeignKey(rw => rw.WaypointId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
