using System.Text.Json.Serialization;

namespace SmartX.Shared.Models;

public class TelemetryReading
{
    public long Id { get; set; }
    public Guid SensorId { get; set; }
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = "Normal";
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public Sensor? Sensor { get; set; }
}
