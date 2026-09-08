using SmartX.Shared.Models;

namespace SmartX.Shared.Utilities;

public static class TelemetryAnalytics
{
    /// <summary>
    /// Recursive maximum finder used to demonstrate recursion on telemetry data.
    /// The implementation is intentionally recursive to satisfy the assessment requirement.
    /// </summary>
    public static double FindMaximumRecursive(double[] values, int index = 0)
    {
        if (values.Length == 0) throw new ArgumentException("At least one value is required.", nameof(values));
        if (index == values.Length - 1) return values[index];

        var maximumOfRest = FindMaximumRecursive(values, index + 1);
        return Math.Max(values[index], maximumOfRest);
    }

    /// <summary>
    /// Builds a jagged array of sequential windows for historical analysis.
    /// Jagged arrays are represented as arrays whose elements are themselves arrays.
    /// </summary>
    public static double[][] BuildJaggedWindows(double[] values, int windowSize)
    {
        if (windowSize <= 0) throw new ArgumentOutOfRangeException(nameof(windowSize));
        if (values.Length == 0) return Array.Empty<double[]>();

        var windows = new List<double[]>();
        for (var offset = 0; offset < values.Length; offset += windowSize)
        {
            var length = Math.Min(windowSize, values.Length - offset);
            var window = new double[length];
            Array.Copy(values, offset, window, 0, length);
            windows.Add(window);
        }

        return windows.ToArray();
    }

    public static TelemetryValue AverageWithOperators(IEnumerable<TelemetryReading> readings)
    {
        var list = readings.ToList();
        if (list.Count == 0) return new TelemetryValue(0, string.Empty);

        var total = new TelemetryValue(0, list[0].Unit);
        foreach (var reading in list)
            total += new TelemetryValue(reading.Value, reading.Unit);

        return total / list.Count;
    }
}
