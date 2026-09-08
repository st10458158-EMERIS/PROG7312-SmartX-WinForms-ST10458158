namespace SmartX.Api.Dtos;

public sealed record TelemetryRequest(Guid SensorId, double Value, DateTimeOffset? Timestamp = null);
