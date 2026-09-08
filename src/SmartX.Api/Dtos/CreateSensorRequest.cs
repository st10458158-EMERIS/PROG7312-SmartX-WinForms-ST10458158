namespace SmartX.Api.Dtos;

public sealed record CreateSensorRequest(
    Guid DeviceId,
    string Name,
    string Metric,
    string Unit,
    double WarningMin,
    double WarningMax);
