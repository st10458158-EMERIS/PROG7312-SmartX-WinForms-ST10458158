using Microsoft.EntityFrameworkCore;
using SmartX.Shared.Models;

namespace SmartX.Api.Data;

// DbContext represents a short-lived EF Core unit of work and is configured through
// dependency injection in SmartX.Api/Program.cs (Microsoft, 2026f).
public sealed class SmartXDbContext(DbContextOptions<SmartXDbContext> options) : DbContext(options)
{
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Sensor> Sensors => Set<Sensor>();
    public DbSet<TelemetryReading> TelemetryReadings => Set<TelemetryReading>();
    public DbSet<AttachmentRecord> Attachments => Set<AttachmentRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Device>()
            .HasIndex(d => d.MacAddress)
            .IsUnique();

        modelBuilder.Entity<Device>()
            .HasMany(d => d.Sensors)
            .WithOne(s => s.Device)
            .HasForeignKey(s => s.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Sensor>()
            .HasMany(s => s.Readings)
            .WithOne(r => r.Sensor)
            .HasForeignKey(r => r.SensorId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TelemetryReading>()
            .HasIndex(r => new { r.SensorId, r.Timestamp });
    }
}
