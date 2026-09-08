using System.Collections;

namespace SmartX.Shared.Utilities;

/// <summary>
/// Fixed-capacity custom generic collection. Add is O(1) and old values are overwritten.
/// This prevents an unbounded in-memory telemetry list. Generic collections provide
/// reusable type-safe storage patterns in C# (Microsoft, 2026e).
/// </summary>
public sealed class TelemetryRingBuffer<T> : IEnumerable<T>
{
    private readonly T[] _items;
    private int _next;
    private int _count;

    public TelemetryRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = new T[capacity];
    }

    public int Capacity => _items.Length;
    public int Count => _count;

    public void Add(T item)
    {
        _items[_next] = item;
        _next = (_next + 1) % Capacity;
        if (_count < Capacity) _count++;
    }

    public IReadOnlyList<T> Snapshot()
    {
        var result = new List<T>(_count);
        var start = _count == Capacity ? _next : 0;

        for (var i = 0; i < _count; i++)
            result.Add(_items[(start + i) % Capacity]);

        return result;
    }

    public IEnumerator<T> GetEnumerator() => Snapshot().GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
