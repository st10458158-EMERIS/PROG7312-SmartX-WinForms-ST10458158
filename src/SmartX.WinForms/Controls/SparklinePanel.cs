namespace SmartX.WinForms.Controls;

public sealed class SparklinePanel : Panel
{
    private IReadOnlyList<double> _values = Array.Empty<double>();

    public SparklinePanel()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;
        Height = 180;
        Dock = DockStyle.Fill;
    }

    public void SetValues(IReadOnlyList<double> values)
    {
        _values = values;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        using var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 8);
        using var linePen = new Pen(Color.RoyalBlue, 2.5f);
        using var gridPen = new Pen(Color.Gainsboro, 1f);

        g.DrawString("Recent telemetry trend", titleFont, Brushes.DimGray, 12, 10);

        var plot = new Rectangle(18, 40, Math.Max(10, Width - 36), Math.Max(10, Height - 58));
        for (var i = 0; i < 4; i++)
        {
            var y = plot.Top + i * plot.Height / 3f;
            g.DrawLine(gridPen, plot.Left, y, plot.Right, y);
        }

        if (_values.Count < 2)
        {
            g.DrawString("Waiting for telemetry...", labelFont, Brushes.Gray, plot.Left + 10, plot.Top + 20);
            return;
        }

        var min = _values.Min();
        var max = _values.Max();
        if (Math.Abs(max - min) < 0.0001) max = min + 1;

        var points = new PointF[_values.Count];
        for (var i = 0; i < _values.Count; i++)
        {
            var x = plot.Left + i * plot.Width / (float)(_values.Count - 1);
            var normalized = (_values[i] - min) / (max - min);
            var y = plot.Bottom - (float)(normalized * plot.Height);
            points[i] = new PointF(x, y);
        }

        g.DrawLines(linePen, points);
        g.DrawString($"Min {min:0.##}   Max {max:0.##}", labelFont, Brushes.Gray, plot.Left, plot.Bottom + 3);
    }
}
