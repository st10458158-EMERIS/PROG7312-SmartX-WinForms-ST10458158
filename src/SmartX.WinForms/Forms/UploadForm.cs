using SmartX.WinForms.Services;

namespace SmartX.WinForms.Forms;

public sealed class UploadForm : Form
{
    private readonly ApiClient _api;
    private readonly TextBox _path = new();
    private readonly Label _message = new();

    public UploadForm(ApiClient api)
    {
        _api = api;
        Text = "Upload Diagnostic File";
        Width = 600;
        Height = 310;
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 10);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        Controls.Add(BuildPanel());
    }

    private Control BuildPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            ColumnCount = 2,
            RowCount = 5
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        var intro = new Label
        {
            Text = "Attach a diagnostic log, CSV, JSON file or screenshot. The API protects the uploaded content before storing it.",
            AutoSize = true,
            MaximumSize = new Size(510, 0)
        };
        panel.SetColumnSpan(intro, 2);
        panel.Controls.Add(intro, 0, 0);

        _path.ReadOnly = true;
        _path.Dock = DockStyle.Fill;
        var browse = new Button { Text = "Browse...", Dock = DockStyle.Fill };
        browse.Click += Browse_Click;
        panel.Controls.Add(_path, 0, 1);
        panel.Controls.Add(browse, 1, 1);

        var types = new Label { Text = "Allowed: .txt, .log, .csv, .json, .png, .jpg, .jpeg (max 5 MB)", AutoSize = true, ForeColor = Color.DimGray };
        panel.SetColumnSpan(types, 2);
        panel.Controls.Add(types, 0, 2);

        _message.AutoSize = true;
        _message.ForeColor = Color.Firebrick;
        panel.SetColumnSpan(_message, 2);
        panel.Controls.Add(_message, 0, 3);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
        var upload = new Button { Text = "Upload", AutoSize = true, Padding = new Padding(10) };
        var cancel = new Button { Text = "Cancel", AutoSize = true, Padding = new Padding(10) };
        upload.Click += Upload_Click;
        cancel.Click += (_, _) => Close();
        buttons.Controls.Add(upload);
        buttons.Controls.Add(cancel);
        panel.SetColumnSpan(buttons, 2);
        panel.Controls.Add(buttons, 0, 4);

        return panel;
    }

    private void Browse_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "Supported files|*.txt;*.log;*.csv;*.json;*.png;*.jpg;*.jpeg|All files|*.*",
            Title = "Choose a SmartX diagnostic file"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            _path.Text = dialog.FileName;
    }

    private async void Upload_Click(object? sender, EventArgs e)
    {
        _message.Text = string.Empty;
        if (!File.Exists(_path.Text))
        {
            _message.Text = "Choose a file first.";
            return;
        }

        try
        {
            // The client sends multipart content asynchronously; server-side rules still validate size and extension.
            var result = await _api.UploadAsync(_path.Text);
            MessageBox.Show(
                $"Upload successful.\nFile: {result.OriginalFileName}\nStored as protected content on the API.",
                "SmartX upload",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            _message.Text = ex.Message;
        }
    }
}
