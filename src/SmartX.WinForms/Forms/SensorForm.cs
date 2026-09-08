using SmartX.Shared.Models;
using SmartX.WinForms.Services;

namespace SmartX.WinForms.Forms;

public sealed class SensorForm : Form
{
    private readonly ApiClient _api;
    private readonly ComboBox _device = new();
    private readonly TextBox _name = new();
    private readonly ComboBox _metric = new();
    private readonly TextBox _unit = new();
    private readonly NumericUpDown _min = new();
    private readonly NumericUpDown _max = new();
    private readonly Label _message = new();

    public SensorForm(ApiClient api)
    {
        _api = api;
        Text = "Register SmartX Sensor";
        Width = 540;
        Height = 500;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        _device.DropDownStyle = ComboBoxStyle.DropDownList;
        _metric.DropDownStyle = ComboBoxStyle.DropDownList;
        _metric.Items.AddRange(["Temperature", "Humidity", "Power Usage", "Pressure", "Flow", "Other"]);
        _metric.SelectedIndex = 0;
        _metric.SelectedIndexChanged += (_, _) => ApplyMetricDefaults();

        _min.DecimalPlaces = 2;
        _min.Minimum = -100000;
        _min.Maximum = 1000000;
        _max.DecimalPlaces = 2;
        _max.Minimum = -100000;
        _max.Maximum = 1000000;

        Controls.Add(BuildFormPanel());
        Shown += async (_, _) => await LoadDevicesAsync();
    }

    private Control BuildFormPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 8
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        AddRow(layout, 0, "Device", _device);
        AddRow(layout, 1, "Sensor name", _name);
        AddRow(layout, 2, "Metric", _metric);
        AddRow(layout, 3, "Unit", _unit);
        AddRow(layout, 4, "Warning minimum", _min);
        AddRow(layout, 5, "Warning maximum", _max);

        _message.AutoSize = true;
        _message.ForeColor = Color.Firebrick;
        layout.Controls.Add(_message, 1, 6);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "Register Sensor", AutoSize = true, Padding = new Padding(8) };
        var cancel = new Button { Text = "Cancel", AutoSize = true, Padding = new Padding(8) };
        save.Click += Save_Click;
        cancel.Click += (_, _) => Close();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 1, 7);

        return layout;
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var devices = await _api.GetDevicesAsync();
            _device.DisplayMember = nameof(Device.Name);
            _device.ValueMember = nameof(Device.Id);
            _device.DataSource = devices;
            ApplyMetricDefaults();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }

    private void ApplyMetricDefaults()
    {
        switch (_metric.Text)
        {
            case "Temperature": _unit.Text = "°C"; _min.Value = 10; _max.Value = 32; break;
            case "Humidity": _unit.Text = "%"; _min.Value = 30; _max.Value = 80; break;
            case "Power Usage": _unit.Text = "W"; _min.Value = 0; _max.Value = 450; break;
            case "Pressure": _unit.Text = "kPa"; _min.Value = 80; _max.Value = 120; break;
            case "Flow": _unit.Text = "L/min"; _min.Value = 0; _max.Value = 100; break;
        }
    }

    // Client-side validation checks required sensor fields and threshold ordering before submission.
    private async void Save_Click(object? sender, EventArgs e)
    {
        _message.Text = string.Empty;
        if (_device.SelectedItem is not Device device)
        {
            _message.Text = "Choose a device.";
            return;
        }
        if (string.IsNullOrWhiteSpace(_name.Text) || string.IsNullOrWhiteSpace(_unit.Text))
        {
            _message.Text = "Sensor name and unit are required.";
            return;
        }
        if (_min.Value >= _max.Value)
        {
            _message.Text = "Minimum threshold must be less than maximum threshold.";
            return;
        }

        try
        {
            await _api.CreateSensorAsync(device.Id, _name.Text.Trim(), _metric.Text, _unit.Text.Trim(),
                (double)_min.Value, (double)_max.Value);
            MessageBox.Show("Sensor registered successfully.", "SmartX", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        layout.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        control.Dock = DockStyle.Fill;
        control.Margin = new Padding(3, 8, 3, 8);
        layout.Controls.Add(control, 1, row);
    }
}
