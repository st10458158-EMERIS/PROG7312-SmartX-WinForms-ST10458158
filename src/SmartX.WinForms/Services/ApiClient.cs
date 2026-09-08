using System.Net.Http.Json;
using System.Text.Json;
using SmartX.Shared.Models;

namespace SmartX.WinForms.Services;

public sealed class ApiClient : IDisposable
{
    public const string BaseUrl = "http://localhost:7000";

    // The same HttpClient instance is reused for the lifetime of this API client rather
    // than creating a new client for every request (Microsoft, 2026d).
    private readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(10)
    };

    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("/api/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public Task<List<Device>> GetDevicesAsync(CancellationToken ct = default) =>
        GetListAsync<Device>("/api/devices", ct);

    public Task<List<Sensor>> GetSensorsAsync(CancellationToken ct = default) =>
        GetListAsync<Sensor>("/api/sensors", ct);

    public Task<List<TelemetryReading>> GetLatestTelemetryAsync(CancellationToken ct = default) =>
        GetListAsync<TelemetryReading>("/api/telemetry/latest", ct);

    public Task<List<TelemetryReading>> GetRecentTelemetryAsync(int take = 50, CancellationToken ct = default) =>
        GetListAsync<TelemetryReading>($"/api/telemetry/recent?take={take}", ct);

    public Task<List<TelemetryReading>> GetHistoryAsync(Guid sensorId, int take = 30, CancellationToken ct = default) =>
        GetListAsync<TelemetryReading>($"/api/telemetry/history/{sensorId}?take={take}", ct);

    public async Task<Device> CreateDeviceAsync(string name, string macAddress, string zone, string category, CancellationToken ct = default)
    {
        var payload = new { name, macAddress, zone, category };
        using var response = await _httpClient.PostAsJsonAsync("/api/devices", payload, _jsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<Device>(_jsonOptions, ct))!;
    }

    public async Task<Sensor> CreateSensorAsync(
        Guid deviceId,
        string name,
        string metric,
        string unit,
        double warningMin,
        double warningMax,
        CancellationToken ct = default)
    {
        var payload = new { deviceId, name, metric, unit, warningMin, warningMax };
        using var response = await _httpClient.PostAsJsonAsync("/api/sensors", payload, _jsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<Sensor>(_jsonOptions, ct))!;
    }

    public async Task<TelemetryReading> SubmitTelemetryAsync(Guid sensorId, double value, CancellationToken ct = default)
    {
        var payload = new { sensorId, value };
        using var response = await _httpClient.PostAsJsonAsync("/api/telemetry", payload, _jsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<TelemetryReading>(_jsonOptions, ct))!;
    }

    public async Task<AttachmentRecord> UploadAsync(string filePath, CancellationToken ct = default)
    {
        await using var stream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(GetContentType(filePath));
        content.Add(fileContent, "file", Path.GetFileName(filePath));

        using var response = await _httpClient.PostAsync("/api/attachments", content, ct);
        await EnsureSuccessAsync(response, ct);
        return (await response.Content.ReadFromJsonAsync<AttachmentRecord>(_jsonOptions, ct))!;
    }

    private async Task<List<T>> GetListAsync<T>(string url, CancellationToken ct)
    {
        try
        {
            // GetFromJsonAsync performs asynchronous HTTP + JSON deserialization, keeping
            // the Windows Forms UI thread free while the request is in progress (Microsoft, 2026d).
            return await _httpClient.GetFromJsonAsync<List<T>>(url, _jsonOptions, ct) ?? new List<T>();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Could not connect to SmartX.Api at {BaseUrl}. Start the API project first.", ex);
        }
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;

        var body = await response.Content.ReadAsStringAsync(ct);
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("message", out var message))
                throw new InvalidOperationException(message.GetString() ?? body);

            if (document.RootElement.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                foreach (var property in errors.EnumerateObject())
                    messages.AddRange(property.Value.EnumerateArray()
                        .Select(v => v.GetString())
                        .Where(v => !string.IsNullOrWhiteSpace(v))
                        .Select(v => v!));
                throw new InvalidOperationException(string.Join(Environment.NewLine, messages));
            }
        }
        catch (JsonException)
        {
            // Fall through to the raw response below.
        }

        throw new InvalidOperationException(string.IsNullOrWhiteSpace(body)
            ? $"API request failed ({(int)response.StatusCode})."
            : body);
    }

    private static string GetContentType(string filePath) => Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".json" => "application/json",
        ".csv" => "text/csv",
        _ => "text/plain"
    };

    public void Dispose() => _httpClient.Dispose();
}
