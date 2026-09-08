namespace SmartX.Shared.Models;

/// <summary>
/// Generic telemetry envelope used to demonstrate advanced OOP/generics.
/// The payload can be a scalar, object or other telemetry representation.
/// Generic type parameters enable reusable, type-safe code (Microsoft, 2026e).
/// </summary>
public sealed class TelemetryPacket<T>
{
    public Guid SensorId { get; init; }
    public T? Payload { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = "SmartX";
}
