namespace SmartX.Api.Dtos;

public sealed record CreateDeviceRequest(string Name, string MacAddress, string Zone, string Category);
