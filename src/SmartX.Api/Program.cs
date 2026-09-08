using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using SmartX.Api.Data;
using SmartX.Api.Dtos;
using SmartX.Api.Services;
using SmartX.Shared.Models;
using SmartX.Shared.Utilities;

var builder = WebApplication.CreateBuilder(args);

// ASP.NET Core Minimal APIs map lightweight HTTP endpoints directly on WebApplication
// and are suitable for compact service backends such as this SmartX ingestion gateway
// (Microsoft, 2026g).
builder.Services.AddDbContext<SmartXDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SmartX")));
// ASP.NET Core Data Protection supplies the IDataProtector used by AttachmentService
// to protect diagnostic content before it is written to disk (Microsoft, 2026c).
builder.Services.AddDataProtection();
builder.Services.AddSingleton(new TelemetryRingBuffer<TelemetryReading>(capacity: 500));
builder.Services.AddScoped<TelemetryService>();
builder.Services.AddScoped<AttachmentService>();
builder.Services.AddHostedService<TelemetrySimulator>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SmartXDbContext>();
    await SeedData.EnsureSeededAsync(db);
}

app.MapGet("/api/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "SmartX.Api",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapGet("/api/devices", async (SmartXDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Devices.OrderBy(d => d.Name).AsNoTracking().ToListAsync(ct)));

app.MapPost("/api/devices", async (CreateDeviceRequest request, SmartXDbContext db, CancellationToken ct) =>
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["Device name is required."];
    if (string.IsNullOrWhiteSpace(request.Zone)) errors["zone"] = ["Zone/location is required."];
    if (string.IsNullOrWhiteSpace(request.Category)) errors["category"] = ["Category is required."];

    const string macPattern = "^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
    if (string.IsNullOrWhiteSpace(request.MacAddress) || !Regex.IsMatch(request.MacAddress, macPattern))
        errors["macAddress"] = ["Use a valid MAC address such as AA:BB:CC:11:22:33."];

    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var normalizedMac = request.MacAddress.ToUpperInvariant().Replace('-', ':');
    if (await db.Devices.AnyAsync(d => d.MacAddress == normalizedMac, ct))
        return Results.Conflict(new { message = "A device with this MAC address already exists." });

    var device = new Device
    {
        Name = request.Name.Trim(),
        MacAddress = normalizedMac,
        Zone = request.Zone.Trim(),
        Category = request.Category.Trim()
    };

    db.Devices.Add(device);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/devices/{device.Id}", device);
});

app.MapGet("/api/sensors", async (SmartXDbContext db, CancellationToken ct) =>
    Results.Ok(await db.Sensors.OrderBy(s => s.Name).AsNoTracking().ToListAsync(ct)));

app.MapPost("/api/sensors", async (CreateSensorRequest request, SmartXDbContext db, CancellationToken ct) =>
{
    var errors = new Dictionary<string, string[]>();
    if (!await db.Devices.AnyAsync(d => d.Id == request.DeviceId && d.IsActive, ct))
        errors["deviceId"] = ["Choose an active device."];
    if (string.IsNullOrWhiteSpace(request.Name)) errors["name"] = ["Sensor name is required."];
    if (string.IsNullOrWhiteSpace(request.Metric)) errors["metric"] = ["Metric is required."];
    if (string.IsNullOrWhiteSpace(request.Unit)) errors["unit"] = ["Unit is required."];
    if (request.WarningMin >= request.WarningMax)
        errors["thresholds"] = ["Warning minimum must be less than warning maximum."];

    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var sensor = new Sensor
    {
        DeviceId = request.DeviceId,
        Name = request.Name.Trim(),
        Metric = request.Metric.Trim(),
        Unit = request.Unit.Trim(),
        WarningMin = request.WarningMin,
        WarningMax = request.WarningMax
    };

    db.Sensors.Add(sensor);
    await db.SaveChangesAsync(ct);
    return Results.Created($"/api/sensors/{sensor.Id}", sensor);
});

app.MapGet("/api/telemetry/latest", async (SmartXDbContext db, CancellationToken ct) =>
{
    var sensorIds = await db.Sensors.Where(s => s.IsEnabled).Select(s => s.Id).ToListAsync(ct);
    var result = new List<TelemetryReading>();
    foreach (var sensorId in sensorIds)
    {
        var latest = await db.TelemetryReadings
            .Where(r => r.SensorId == sensorId)
            .OrderByDescending(r => r.Timestamp)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
        if (latest is not null) result.Add(latest);
    }

    return Results.Ok(result.OrderBy(r => r.SensorId));
});

app.MapGet("/api/telemetry/recent", async (int? take, SmartXDbContext db, CancellationToken ct) =>
{
    var safeTake = Math.Clamp(take ?? 50, 1, 200);
    var rows = await db.TelemetryReadings
        .OrderByDescending(r => r.Timestamp)
        .Take(safeTake)
        .AsNoTracking()
        .ToListAsync(ct);
    return Results.Ok(rows);
});

app.MapGet("/api/telemetry/history/{sensorId:guid}", async (Guid sensorId, int? take, SmartXDbContext db, CancellationToken ct) =>
{
    var safeTake = Math.Clamp(take ?? 30, 1, 200);
    var rows = await db.TelemetryReadings
        .Where(r => r.SensorId == sensorId)
        .OrderByDescending(r => r.Timestamp)
        .Take(safeTake)
        .AsNoTracking()
        .ToListAsync(ct);
    rows.Reverse();
    return Results.Ok(rows);
});

app.MapPost("/api/telemetry", async (TelemetryRequest request, TelemetryService service, CancellationToken ct) =>
{
    var (reading, error) = await service.IngestAsync(request, ct);
    return error is null
        ? Results.Ok(reading)
        : Results.BadRequest(new { message = error });
});

app.MapGet("/api/analytics/{sensorId:guid}", async (Guid sensorId, SmartXDbContext db, CancellationToken ct) =>
{
    var readings = await db.TelemetryReadings
        .Where(r => r.SensorId == sensorId)
        .OrderByDescending(r => r.Timestamp)
        .Take(24)
        .AsNoTracking()
        .ToListAsync(ct);
    readings.Reverse();

    if (readings.Count == 0)
        return Results.NotFound(new { message = "No telemetry exists for this sensor yet." });

    var values = readings.Select(r => r.Value).ToArray();
    var jagged = TelemetryAnalytics.BuildJaggedWindows(values, 6);
    var recursivePeak = TelemetryAnalytics.FindMaximumRecursive(values);
    var average = TelemetryAnalytics.AverageWithOperators(readings);

    return Results.Ok(new
    {
        sensorId,
        recursivePeak,
        average = average.Value,
        unit = average.Unit,
        jaggedWindows = jagged,
        readingCount = readings.Count
    });
});

app.MapPost("/api/attachments", async (HttpRequest request, AttachmentService service, CancellationToken ct) =>
{
    if (!request.HasFormContentType)
        return Results.BadRequest(new { message = "multipart/form-data is required." });

    var form = await request.ReadFormAsync(ct);
    var file = form.Files.GetFile("file");
    if (file is null || file.Length == 0)
        return Results.BadRequest(new { message = "Choose a non-empty diagnostic file." });

    if (file.Length > 5 * 1024 * 1024)
        return Results.BadRequest(new { message = "The maximum file size is 5 MB." });

    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowed = new[] { ".txt", ".log", ".csv", ".json", ".png", ".jpg", ".jpeg" };
    if (!allowed.Contains(extension))
        return Results.BadRequest(new { message = "Allowed file types: txt, log, csv, json, png, jpg, jpeg." });

    var record = await service.SaveAsync(file, ct);
    return Results.Ok(record);
});

app.Run("http://localhost:7000");
