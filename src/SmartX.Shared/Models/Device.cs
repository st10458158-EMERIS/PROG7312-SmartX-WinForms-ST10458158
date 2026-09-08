using System.Text.Json.Serialization;

namespace SmartX.Shared.Models;

public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public string Zone { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [JsonIgnore]
    public List<Sensor> Sensors { get; set; } = new();
}
