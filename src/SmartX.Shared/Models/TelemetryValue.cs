namespace SmartX.Shared.Models;

/// <summary>
/// Value object with operator overloading used by analytics calculations.
/// C# permits user-defined types to provide custom implementations of predefined
/// operators such as + and / (Microsoft, n.d.b).
/// </summary>
public readonly record struct TelemetryValue(double Value, string Unit)
{
    public static TelemetryValue operator +(TelemetryValue left, TelemetryValue right)
    {
        if (!string.Equals(left.Unit, right.Unit, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Telemetry values must use the same unit.");

        return new TelemetryValue(left.Value + right.Value, left.Unit);
    }

    public static TelemetryValue operator /(TelemetryValue value, double divisor)
    {
        if (divisor == 0) throw new DivideByZeroException();
        return new TelemetryValue(value.Value / divisor, value.Unit);
    }

    public override string ToString() => $"{Value:0.##} {Unit}";
}
