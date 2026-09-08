using SmartX.Shared.Models;
using SmartX.WinForms.Controls;
using SmartX.WinForms.Services;

namespace SmartX.WinForms.Forms;

public sealed class MainForm : Form
{
    private readonly ApiClient _api = new();
    private readonly System.Windows.Forms.Timer _refreshTimer = new() { Interval = 3000 };

    private readonly Label _connection = new();
    private readonly Label _deviceCount = new();
    private readonly Label _sensorCount = new();
    private readonly Label _warningCount = new();
    private readonly FlowLayoutPanel _metricCards = new();
    private readonly DataGridView _recentGrid = new();
    private readonly SparklinePanel _sparkline = new();
    private readonly Label _lastUpdated = new();

    private List<Sensor> _sensors = new();
    private bool _refreshInProgress;

    public MainForm()
    {
        Text = "SmartX - IoT Monitoring and Management";
        Width = 1280;
        Height = 820;
        MinimumSize = new Size(1050, 700);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 247, 250);

        Controls.Add(BuildLayout());

        Shown += async (_, _) =>
        {
            await RefreshDashboardAsync();
            _refreshTimer.Start();
        };
        _refreshTimer.Tick += async (_, _) => await RefreshDashboardAsync();
    }

    private Control BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 215));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildSidebar(), 0, 0);
        root.Controls.Add(BuildContent(), 1, 0);
        return root;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(25, 32, 48),
            Padding = new Padding(16)
        };

        var title = new Label
        {
            Text = "SmartX",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(18, 20)
        };
        var subtitle = new Label
        {
            Text = "IoT Control Panel",
            ForeColor = Color.LightGray,
            Font = new Font("Segoe UI", 9),
            AutoSize = true,
            Location = new Point(20, 62)
        };

        sidebar.Controls.Add(title);
        sidebar.Controls.Add(subtitle);

        var buttonPanel = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Location = new Point(12, 115),
            Size = new Size(190, 430),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        buttonPanel.Controls.Add(CreateNavButton("Refresh Dashboard", async () => await RefreshDashboardAsync()));
        buttonPanel.Controls.Add(CreateNavButton("Register Device", OpenDeviceForm));
        buttonPanel.Controls.Add(CreateNavButton("Register Sensor", OpenSensorForm));
        buttonPanel.Controls.Add(CreateNavButton("Submit Telemetry", OpenTelemetryForm));
        buttonPanel.Controls.Add(CreateNavButton("Upload Diagnostic", OpenUploadForm));

        sidebar.Controls.Add(buttonPanel);

        _connection.Text = "API: checking...";
        _connection.ForeColor = Color.Gold;
        _connection.AutoSize = true;
        _connection.Location = new Point(20, 650);
        _connection.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
        sidebar.Controls.Add(_connection);

        var endpoint = new Label
        {
            Text = ApiClient.BaseUrl,
            ForeColor = Color.Gray,
            AutoSize = true,
            Location = new Point(20, 676),
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Font = new Font("Segoe UI", 8)
        };
        sidebar.Controls.Add(endpoint);

        return sidebar;
    }

    private Control BuildContent()
    {
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Color.FromArgb(245, 247, 250)
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 74));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 145));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var header = new Panel { Dock = DockStyle.Fill };
        header.Controls.Add(new Label
        {
            Text = "SmartX Operations Dashboard",
            AutoSize = true,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.FromArgb(17, 24, 39),
            Location = new Point(0, 4)
        });
        header.Controls.Add(new Label
        {
            Text = "Live device, sensor and telemetry monitoring",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(2, 44)
        });
        content.Controls.Add(header, 0, 0);

        var summary = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1 };
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
        summary.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
        summary.Controls.Add(CreateSummaryCard("Devices", _deviceCount), 0, 0);
        summary.Controls.Add(CreateSummaryCard("Sensors", _sensorCount), 1, 0);
        summary.Controls.Add(CreateSummaryCard("Warnings", _warningCount), 2, 0);
        content.Controls.Add(summary, 0, 1);

        _metricCards.Dock = DockStyle.Fill;
        _metricCards.AutoScroll = true;
        _metricCards.WrapContents = false;
        _metricCards.FlowDirection = FlowDirection.LeftToRight;
        _metricCards.Padding = new Padding(0, 5, 0, 5);
        content.Controls.Add(_metricCards, 0, 2);

        // A TableLayoutPanel is used here because it resizes proportionally at run time
        // without requiring a SplitterDistance before the control has a valid width
        // (Microsoft, 2025a). This fixes the InvalidOperationException that previously
        // occurred while MainForm.BuildContent() was constructing the SplitContainer.
        var lowerDashboard = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        lowerDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56f));
        lowerDashboard.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44f));

        var recentPanel = BuildRecentGridPanel();
        recentPanel.Margin = new Padding(0, 8, 8, 0);

        var trendPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 8, 0, 0),
            Margin = Padding.Empty
        };
        trendPanel.Controls.Add(_sparkline);

        lowerDashboard.Controls.Add(recentPanel, 0, 0);
        lowerDashboard.Controls.Add(trendPanel, 1, 0);
        content.Controls.Add(lowerDashboard, 0, 3);

        _lastUpdated.AutoSize = true;
        _lastUpdated.ForeColor = Color.DimGray;
        _lastUpdated.Text = "Waiting for first refresh...";
        content.Controls.Add(_lastUpdated, 0, 4);

        return content;
    }

    private Control BuildRecentGridPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
        var title = new Label
        {
            Text = "Recent telemetry",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(2, 0, 0, 8)
        };

        _recentGrid.Dock = DockStyle.Fill;
        _recentGrid.ReadOnly = true;
        _recentGrid.AllowUserToAddRows = false;
        _recentGrid.AllowUserToDeleteRows = false;
        _recentGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _recentGrid.BackgroundColor = Color.White;
        _recentGrid.BorderStyle = BorderStyle.None;
        _recentGrid.RowHeadersVisible = false;
        _recentGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

        panel.Controls.Add(_recentGrid);
        panel.Controls.Add(title);
        return panel;
    }

    private async Task RefreshDashboardAsync()
    {
        if (_refreshInProgress) return;
        _refreshInProgress = true;
        try
        {
            var healthy = await _api.IsHealthyAsync();
            SetConnectionState(healthy);
            if (!healthy)
            {
                _lastUpdated.Text = $"API unavailable at {ApiClient.BaseUrl}. Start SmartX.Api and click Refresh Dashboard.";
                return;
            }

            // Independent API requests are started together and awaited as a group so the
            // desktop UI remains responsive while network I/O completes (Microsoft, 2026d).
            var devicesTask = _api.GetDevicesAsync();
            var sensorsTask = _api.GetSensorsAsync();
            var latestTask = _api.GetLatestTelemetryAsync();
            var recentTask = _api.GetRecentTelemetryAsync(30);
            await Task.WhenAll(devicesTask, sensorsTask, latestTask, recentTask);

            var devices = await devicesTask;
            _sensors = await sensorsTask;
            var latest = await latestTask;
            var recent = await recentTask;

            _deviceCount.Text = devices.Count.ToString();
            _sensorCount.Text = _sensors.Count.ToString();
            _warningCount.Text = latest.Count(r => r.Status == "Warning").ToString();

            RenderMetricCards(latest);
            RenderRecentGrid(recent);
            RenderSparkline(recent);
            _lastUpdated.Text = $"Last updated: {DateTime.Now:HH:mm:ss} | Auto-refresh every {_refreshTimer.Interval / 1000} seconds";
        }
        catch (Exception ex)
        {
            SetConnectionState(false);
            _lastUpdated.Text = $"Refresh failed: {ex.Message}";
        }
        finally
        {
            _refreshInProgress = false;
        }
    }

    private void RenderMetricCards(List<TelemetryReading> latest)
    {
        _metricCards.SuspendLayout();
        _metricCards.Controls.Clear();

        foreach (var reading in latest.Take(4))
        {
            var sensor = _sensors.FirstOrDefault(s => s.Id == reading.SensorId);
            var card = new MetricCard();
            card.SetMetric(sensor?.Metric ?? sensor?.Name ?? "Telemetry", $"{reading.Value:0.##} {reading.Unit}", reading.Status);
            _metricCards.Controls.Add(card);
        }

        if (_metricCards.Controls.Count == 0)
        {
            _metricCards.Controls.Add(new Label
            {
                Text = "No telemetry has been received yet.",
                AutoSize = true,
                ForeColor = Color.DimGray,
                Padding = new Padding(8, 30, 0, 0)
            });
        }

        _metricCards.ResumeLayout();
    }

    private void RenderRecentGrid(List<TelemetryReading> recent)
    {
        var rows = recent.Select(r => new
        {
            Sensor = _sensors.FirstOrDefault(s => s.Id == r.SensorId)?.Name ?? r.SensorId.ToString()[..8],
            Value = $"{r.Value:0.##} {r.Unit}",
            r.Status,
            Time = r.Timestamp.LocalDateTime.ToString("HH:mm:ss")
        }).ToList();

        _recentGrid.DataSource = rows;
        foreach (DataGridViewRow row in _recentGrid.Rows)
        {
            if (string.Equals(row.Cells["Status"].Value?.ToString(), "Warning", StringComparison.OrdinalIgnoreCase))
                row.DefaultCellStyle.BackColor = Color.MistyRose;
        }
    }

    private void RenderSparkline(List<TelemetryReading> recent)
    {
        if (_sensors.Count == 0)
        {
            _sparkline.SetValues(Array.Empty<double>());
            return;
        }

        var sensorId = _sensors[0].Id;
        var values = recent
            .Where(r => r.SensorId == sensorId)
            .OrderBy(r => r.Timestamp)
            .Select(r => r.Value)
            .ToList();
        _sparkline.SetValues(values);
    }

    private void SetConnectionState(bool connected)
    {
        _connection.Text = connected ? "API: Connected" : "API: Offline";
        _connection.ForeColor = connected ? Color.LightGreen : Color.Salmon;
    }

    private Button CreateNavButton(string text, Func<Task> action)
    {
        var button = CreateNavButtonBase(text);
        button.Click += async (_, _) => await action();
        return button;
    }

    private Button CreateNavButton(string text, Action action)
    {
        var button = CreateNavButtonBase(text);
        button.Click += (_, _) => action();
        return button;
    }

    private static Button CreateNavButtonBase(string text) => new()
    {
        Text = text,
        Width = 175,
        Height = 48,
        FlatStyle = FlatStyle.Flat,
        FlatAppearance = { BorderSize = 0 },
        BackColor = Color.FromArgb(37, 47, 69),
        ForeColor = Color.White,
        TextAlign = ContentAlignment.MiddleLeft,
        Padding = new Padding(12, 0, 0, 0),
        Margin = new Padding(0, 0, 0, 8),
        Cursor = Cursors.Hand
    };

    private static Control CreateSummaryCard(string title, Label value)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 12, 8)
        };

        var label = new Label
        {
            Text = title,
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(14, 10)
        };

        value.AutoSize = true;
        value.Text = "0";
        value.Font = new Font("Segoe UI", 22, FontStyle.Bold);
        value.ForeColor = Color.FromArgb(17, 24, 39);
        value.Location = new Point(12, 34);
        panel.Controls.Add(label);
        panel.Controls.Add(value);
        return panel;
    }

    private void OpenDeviceForm()
    {
        using var form = new DeviceForm(_api);
        if (form.ShowDialog(this) == DialogResult.OK) _ = RefreshDashboardAsync();
    }

    private void OpenSensorForm()
    {
        using var form = new SensorForm(_api);
        if (form.ShowDialog(this) == DialogResult.OK) _ = RefreshDashboardAsync();
    }

    private void OpenTelemetryForm()
    {
        using var form = new TelemetryForm(_api);
        if (form.ShowDialog(this) == DialogResult.OK) _ = RefreshDashboardAsync();
    }

    private void OpenUploadForm()
    {
        using var form = new UploadForm(_api);
        form.ShowDialog(this);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _refreshTimer.Stop();
        _refreshTimer.Dispose();
        _api.Dispose();
        base.OnFormClosed(e);
    }
}
