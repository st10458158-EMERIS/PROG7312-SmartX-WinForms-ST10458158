using SmartX.Shared.Models;
using SmartX.WinForms.Services;

namespace SmartX.WinForms.Forms;

public sealed class TelemetryForm : Form
{
    private readonly ApiClient _api;
    private readonly ComboBox _sensor = new();
    private readonly NumericUpDown _value = new();
    private readonly Label _message = new();
    private readonly Label _threshold = new();

    public TelemetryForm(ApiClient api)
    {
        _api = api;
        Text = "Manual Telemetry Ingestion";
        Width = 520;
        Height = 360;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        _sensor.DropDownStyle = ComboBoxStyle.DropDownList;
        _sensor.SelectedIndexChanged += (_, _) => UpdateThresholdLabel();
        _value.DecimalPlaces = 2;
        _value.Minimum = -100000;
        _value.Maximum = 1000000;
        _value.Increment = 0.1m;

        Controls.Add(BuildPanel());
        Shown += async (_, _) => await LoadSensorsAsync();
    }

    private Control BuildPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 6
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "Sensor", _sensor);
        AddRow(layout, 1, "Reading value", _value);
        _threshold.AutoSize = true;
        _threshold.ForeColor = Color.DimGray;
        layout.Controls.Add(_threshold, 1, 2);

        var hint = new Label
        {
            AutoSize = true,
            Text = "Try a value outside the threshold to demonstrate a Warning state.",
            ForeColor = Color.DimGray
        };
        layout.Controls.Add(hint, 1, 3);

        _message.AutoSize = true;
        _message.ForeColor = Color.Firebrick;
        layout.Controls.Add(_message, 1, 4);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var submit = new Button { Text = "Submit Telemetry", AutoSize = true, Padding = new Padding(8) };
        var cancel = new Button { Text = "Cancel", AutoSize = true, Padding = new Padding(8) };
        submit.Click += Submit_Click;
        cancel.Click += (_, _) => Close();
        buttons.Controls.Add(submit);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 1, 5);
        return layout;
    }

    private async Task LoadSensorsAsync()
    {
        try
        {
            var sensors = await _api.GetSensorsAsync();
            _sensor.DisplayMember = nameof(Sensor.Name);
            _sensor.DataSource = sensors;
            UpdateThresholdLabel();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }

    private void UpdateThresholdLabel()
    {
        if (_sensor.SelectedItem is not Sensor sensor) return;
        _threshold.Text = $"Expected range: {sensor.WarningMin:0.##} to {sensor.WarningMax:0.##} {sensor.Unit}";
    }

    private async void Submit_Click(object? sender, EventArgs e)
    {
        _message.Text = string.Empty;
        if (_sensor.SelectedItem is not Sensor sensor)
        {
            _message.Text = "Choose a sensor.";
            return;
        }

        try
        {
            // The asynchronous API call prevents network I/O from blocking the Windows Forms UI thread (Microsoft, 2026d).
            var reading = await _api.SubmitTelemetryAsync(sensor.Id, (double)_value.Value);
            MessageBox.Show(
                $"Reading accepted: {reading.Value:0.##} {reading.Unit}\nStatus: {reading.Status}",
                "SmartX telemetry",
                MessageBoxButtons.OK,
                reading.Status == "Warning" ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 8, 3, 8);
        layout.Controls.Add(control, 1, row);
    }
}
