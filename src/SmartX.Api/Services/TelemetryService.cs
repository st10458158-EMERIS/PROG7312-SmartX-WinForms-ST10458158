using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Dtos;
using SmartX.Shared.Models;
using SmartX.Shared.Utilities;

namespace SmartX.Api.Services;

public sealed class TelemetryService(SmartXDbContext db, TelemetryRingBuffer<TelemetryReading> buffer)
{
    public async Task<(TelemetryReading? Reading, string? Error)> IngestAsync(
        TelemetryRequest request,
        CancellationToken cancellationToken = default)
    {
        var sensor = await db.Sensors
            .FirstOrDefaultAsync(s => s.Id == request.SensorId && s.IsEnabled, cancellationToken);

        if (sensor is null)
            return (null, "The selected sensor does not exist or is disabled.");

        if (double.IsNaN(request.Value) || double.IsInfinity(request.Value))
            return (null, "Telemetry value must be a finite number.");

        if (request.Value is < -100000 or > 1000000)
            return (null, "Telemetry value is outside the permitted ingestion range.");

        var packet = new TelemetryPacket<double>
        {
            SensorId = request.SensorId,
            Payload = request.Value,
            Timestamp = request.Timestamp ?? DateTimeOffset.UtcNow,
            Source = "SmartX WinForms"
        };

        var status = packet.Payload < sensor.WarningMin || packet.Payload > sensor.WarningMax
            ? "Warning"
            : "Normal";

        var reading = new TelemetryReading
        {
            SensorId = sensor.Id,
            Value = packet.Payload,
            Unit = sensor.Unit,
            Status = status,
            Message = status == "Warning"
                ? $"{sensor.Name} is outside its configured threshold."
                : "Reading accepted.",
            Timestamp = packet.Timestamp
        };

        db.TelemetryReadings.Add(reading);
        await db.SaveChangesAsync(cancellationToken);
        buffer.Add(reading);
        return (reading, null);
    }
}
