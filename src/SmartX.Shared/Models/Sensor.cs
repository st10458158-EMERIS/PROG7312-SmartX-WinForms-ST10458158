using System.Text.Json.Serialization;

namespace SmartX.Shared.Models;

public class Sensor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DeviceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Metric { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double WarningMin { get; set; }
    public double WarningMax { get; set; }
    public bool IsEnabled { get; set; } = true;

    [JsonIgnore]
    public Device? Device { get; set; }

    [JsonIgnore]
    public List<TelemetryReading> Readings { get; set; } = new();
}
