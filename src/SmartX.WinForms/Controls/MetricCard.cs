namespace SmartX.WinForms.Controls;

public sealed class MetricCard : Panel
{
    private readonly Label _title = new();
    private readonly Label _value = new();
    private readonly Label _status = new();

    public MetricCard()
    {
        Width = 220;
        Height = 118;
        Padding = new Padding(16);
        Margin = new Padding(8);
        BackColor = Color.White;
        BorderStyle = BorderStyle.FixedSingle;

        _title.AutoSize = true;
        _title.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        _title.ForeColor = Color.FromArgb(55, 65, 81);
        _title.Location = new Point(14, 12);

        _value.AutoSize = true;
        _value.Font = new Font("Segoe UI", 22, FontStyle.Bold);
        _value.ForeColor = Color.FromArgb(17, 24, 39);
        _value.Location = new Point(12, 40);

        _status.AutoSize = true;
        _status.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _status.Location = new Point(14, 86);

        Controls.AddRange([_title, _value, _status]);
    }

    public void SetMetric(string title, string value, string status)
    {
        _title.Text = title;
        _value.Text = value;
        _status.Text = status;
        _status.ForeColor = string.Equals(status, "Warning", StringComparison.OrdinalIgnoreCase)
            ? Color.Firebrick
            : Color.SeaGreen;
    }
}
