
# SmartX - Windows Forms Part 1 Implementation

SmartX is an IoT monitoring and management system built for the PROG7312 Part 1 scenario. This version uses a **Windows Forms desktop frontend**, a **.NET 10 ASP.NET Core Minimal API**, a shared class library, and **SQL Server LocalDB**.

## Solution structure

```text
SmartX_WinForms
|
+-- SmartX.Shared
|   +-- shared models
|   +-- TelemetryPacket<T> generic class
|   +-- TelemetryValue operator overloading
|   +-- TelemetryRingBuffer<T> custom collection
|   +-- jagged-array and recursive analytics
|
+-- SmartX.Api
|   +-- ASP.NET Core Minimal API
|   +-- EF Core / SQL Server
|   +-- device and sensor registration
|   +-- telemetry validation and ingestion
|   +-- diagnostic upload + Data Protection
|   +-- telemetry simulator
|
+-- SmartX.WinForms
    +-- dashboard
    +-- device registration form
    +-- sensor registration form
    +-- manual telemetry form
    +-- diagnostic file upload form
    +-- async HttpClient API integration
```

## Prerequisites

- Windows 10/11
- Visual Studio with **.NET desktop development** and **ASP.NET and web development** workloads
- .NET 10 SDK
- SQL Server LocalDB (`MSSQLLocalDB`)

Check LocalDB in PowerShell:

```powershell
sqllocaldb info
```

You should see `MSSQLLocalDB`.

## Important note about Docker

**Docker Desktop is not required for this project.** If Visual Studio shows a Containers panel saying Docker Desktop is not running, you can ignore or close that panel. SmartX runs directly with .NET, Windows Forms and SQL Server LocalDB.

## Quick verification before running

From the solution folder, you can run:

```powershell
.\Verify-SmartX.ps1
```

This checks the .NET SDK, checks for SQL Server LocalDB, restores NuGet packages and builds the complete solution.

## Recommended way to run in Visual Studio

1. Extract the project ZIP.
2. Open `SmartX_WinForms.sln` in Visual Studio.
3. Right-click the **solution** > **Configure Startup Projects**.
4. Choose **Multiple startup projects**.
5. Set:
   - `SmartX.Api` = **Start**
   - `SmartX.WinForms` = **Start**
   - `SmartX.Shared` = **None**
6. Make sure `SmartX.Api` is above `SmartX.WinForms`.
7. Build > **Build Solution**.
8. Press **F5**.

The API listens on:

```text
http://localhost:7000
```

The Windows Forms dashboard checks that address automatically.

### Fixed MainForm startup issue

The original package could throw `System.InvalidOperationException` while constructing the dashboard because a `SplitContainer.SplitterDistance` was assigned before the control had a valid run-time width. The revised project uses a proportional `TableLayoutPanel` for the lower dashboard area. See `FIX_NOTES.md`.

## Alternative PowerShell startup

From the solution folder:

```powershell
.\Start-SmartX.ps1
```

Or run each project manually:

```powershell
cd src\SmartX.Api
dotnet restore
dotnet run
```

In a second terminal:

```powershell
cd src\SmartX.WinForms
dotnet restore
dotnet run
```

## Database

The default connection string is in `src/SmartX.Api/appsettings.json`:

```text
Server=(localdb)\MSSQLLocalDB;Database=SmartXWinForms;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True
```

On the first run, the API uses `Database.EnsureCreatedAsync()` and seeds example devices and sensors. The telemetry simulator generates readings every five seconds.

## Main functionality

### Dashboard

- API connection state
- Device, sensor and warning counts
- Live telemetry cards
- Automatic three-second refresh
- Warning-row highlighting
- Custom sparkline trend panel

### Device registration

Captures:

- Device name
- MAC address
- Zone/location
- Category

Validation occurs in both Windows Forms and the API.

### Sensor registration

Captures:

- Device
- Sensor name
- Metric
- Unit
- Minimum warning threshold
- Maximum warning threshold

### Telemetry ingestion

The client submits a sensor and numeric reading to the API. The API:

1. validates the sensor;
2. validates the telemetry value;
3. wraps the value in `TelemetryPacket<double>`;
4. determines Normal/Warning status;
5. persists the reading to SQL Server; and
6. adds it to the fixed-capacity `TelemetryRingBuffer<T>`.

### Advanced OOP

`SmartX.Shared` contains:

- `TelemetryPacket<T>` - generics
- `TelemetryValue` - `+` and `/` operator overloading
- `TelemetryRingBuffer<T>` - a custom bounded collection

### Arrays and recursion

`TelemetryAnalytics` demonstrates:

- `BuildJaggedWindows()` - creates a jagged `double[][]` structure;
- `FindMaximumRecursive()` - recursively finds the maximum telemetry value;
- `AverageWithOperators()` - uses the overloaded `TelemetryValue` operators.

The API exposes this through:

```text
GET /api/analytics/{sensorId}
```

### Diagnostic attachment upload

The Windows Forms client sends supported files through multipart upload. The API validates the extension and size, protects the content using ASP.NET Core Data Protection, and writes a `.protected` file under:

```text
src/SmartX.Api/App_Data/ProtectedUploads
```

## Useful API endpoints

```text
GET  /api/health
GET  /api/devices
POST /api/devices
GET  /api/sensors
POST /api/sensors
GET  /api/telemetry/latest
GET  /api/telemetry/recent?take=50
GET  /api/telemetry/history/{sensorId}?take=30
POST /api/telemetry
GET  /api/analytics/{sensorId}
POST /api/attachments
```

## Assessment evidence

Use `docs/screenshot-checklist.md` and the supplied Word implementation report to capture evidence. Do not fabricate GitHub history: make meaningful commits as you work through and test the project.

## Suggested genuine Git commit sequence

```text
Initial SmartX solution structure
Add shared IoT domain models
Add SQL Server EF Core data context
Add device and sensor API endpoints
Add telemetry ingestion and validation
Add generic telemetry packet and operators
Add recursive and jagged-array analytics
Add bounded telemetry ring buffer
Add telemetry simulator
Add Windows Forms API client
Add SmartX dashboard UI
Add device registration validation
Add sensor registration form
Add manual telemetry ingestion form
Add dynamic dashboard refresh and warnings
Add diagnostic file upload and protection
Add documentation and assessment evidence checklist
```

## Code references 

In-text references are included in relevant `.cs` comments. The complete reference list is also available in `CODE_REFERENCES.md`. The 2026 suffixes continue the lettering already used in the accompanying evidence report.

Microsoft. 2025a. *TableLayoutPanel Control Overview - Windows Forms*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/desktop/winforms/controls/tablelayoutpanel-control-overview [Accessed 8 September 2026].

Microsoft. 2025b. *Worker services in .NET*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/core/extensions/workers [Accessed 8 September 2026].

Microsoft. 2026c. *ASP.NET Core data protection overview*. [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction?view=aspnetcore-10.0 [Accessed 8 September 2026].

Microsoft. 2026d. *Guidelines for using HttpClient*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines [Accessed 8 September 2026].

Microsoft. 2026e. *Generic types and methods - C#*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/generics [Accessed 8 September 2026].

Microsoft. 2026f. *DbContext Lifetime, Configuration, and Initialization - EF Core*. [Online]. Available at: https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/ [Accessed 8 September 2026].

Microsoft. 2026g. *Minimal APIs quick reference*. [Online]. Available at: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis?view=aspnetcore-10.0 [Accessed 8 September 2026].

Microsoft. n.d.b. *Operator overloading - C# reference*. [Online]. Available at: https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/operator-overloading [Accessed 8 September 2026].
=======
# PROG7312-SmartX-WinForms-ST10458158
SmartX IoT Monitoring and Telemetry Management System - PROG7312 Part 1

