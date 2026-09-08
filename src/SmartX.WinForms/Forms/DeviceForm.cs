using System.Text.RegularExpressions;
using SmartX.WinForms.Services;

namespace SmartX.WinForms.Forms;

public sealed class DeviceForm : Form
{
    private readonly ApiClient _api;
    private readonly TextBox _name = new();
    private readonly TextBox _mac = new();
    private readonly TextBox _zone = new();
    private readonly ComboBox _category = new();
    private readonly Label _message = new();

    public DeviceForm(ApiClient api)
    {
        _api = api;
        Text = "Register SmartX Device";
        Width = 520;
        Height = 420;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;

        var panel = BuildFormPanel();
        Controls.Add(panel);
    }

    private Control BuildFormPanel()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 7
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        _category.DropDownStyle = ComboBoxStyle.DropDownList;
        _category.Items.AddRange(["Agriculture", "Utilities", "Healthcare", "Estate", "Industrial", "Other"]);
        _category.SelectedIndex = 0;

        AddRow(layout, 0, "Device name", _name);
        AddRow(layout, 1, "MAC address", _mac);
        AddRow(layout, 2, "Zone/location", _zone);
        AddRow(layout, 3, "Category", _category);

        var hint = new Label
        {
            Text = "Example MAC: AA:BB:CC:11:22:33",
            AutoSize = true,
            ForeColor = Color.DimGray
        };
        layout.Controls.Add(hint, 1, 4);

        _message.AutoSize = true;
        _message.ForeColor = Color.Firebrick;
        layout.Controls.Add(_message, 1, 5);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var save = new Button { Text = "Register Device", AutoSize = true, Padding = new Padding(8) };
        var cancel = new Button { Text = "Cancel", AutoSize = true, Padding = new Padding(8) };
        save.Click += Save_Click;
        cancel.Click += (_, _) => Close();
        buttons.Controls.Add(save);
        buttons.Controls.Add(cancel);
        layout.Controls.Add(buttons, 1, 6);

        return layout;
    }

    private async void Save_Click(object? sender, EventArgs e)
    {
        _message.Text = string.Empty;
        var errors = ValidateForm();
        if (errors.Count > 0)
        {
            _message.Text = string.Join("  ", errors);
            return;
        }

        try
        {
            await _api.CreateDeviceAsync(_name.Text.Trim(), _mac.Text.Trim(), _zone.Text.Trim(), _category.Text);
            MessageBox.Show("Device registered successfully.", "SmartX", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }

    // Client-side validation gives immediate feedback before the API performs its own validation.
    private List<string> ValidateForm()
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(_name.Text)) errors.Add("Device name is required.");
        if (string.IsNullOrWhiteSpace(_zone.Text)) errors.Add("Zone is required.");
        if (!Regex.IsMatch(_mac.Text.Trim(), "^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$"))
            errors.Add("Enter a valid MAC address.");
        return errors;
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
