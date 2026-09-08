using Microsoft.EntityFrameworkCore;
using SmartX.Shared.Models;

namespace SmartX.Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(SmartXDbContext db)
    {
        await db.Database.EnsureCreatedAsync();
        if (await db.Devices.AnyAsync()) return;

        var greenhouse = new Device
        {
            Name = "Greenhouse Gateway",
            MacAddress = "AA:BB:CC:10:20:30",
            Zone = "Greenhouse A",
            Category = "Agriculture"
        };

        var utility = new Device
        {
            Name = "Utility Monitor",
            MacAddress = "AA:BB:CC:40:50:60",
            Zone = "Plant Room",
            Category = "Utilities"
        };

        var temperature = new Sensor
        {
            DeviceId = greenhouse.Id,
            Name = "Temperature Sensor",
            Metric = "Temperature",
            Unit = "°C",
            WarningMin = 10,
            WarningMax = 32
        };

        var humidity = new Sensor
        {
            DeviceId = greenhouse.Id,
            Name = "Humidity Sensor",
            Metric = "Humidity",
            Unit = "%",
            WarningMin = 30,
            WarningMax = 80
        };

        var power = new Sensor
        {
            DeviceId = utility.Id,
            Name = "Power Meter",
            Metric = "Power Usage",
            Unit = "W",
            WarningMin = 0,
            WarningMax = 450
        };

        db.Devices.AddRange(greenhouse, utility);
        db.Sensors.AddRange(temperature, humidity, power);
        await db.SaveChangesAsync();
    }
}
