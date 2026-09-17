using LocoVPN.Core;
using System.Xml.Linq;

namespace LocoVPN;

public partial class MainForm : Form
{
    private readonly AppController _controller;

    // Keeps display-string order in sync with comboServers.Items, since cuiComboBox works
    // with plain strings/indexes rather than bound objects. This installed version of
    // cuiComboBox doesn't reorder Items itself, so array order == display order.
    private readonly List<ServerProfile> _orderedServers = new();

    // Colors for the connect/disconnect circular button per connection state.
    private static readonly Color DisconnectedColor = Color.FromArgb(230, 80, 50);   // orange/red
    private static readonly Color DisconnectedHover = Color.FromArgb(240, 95, 65);
    private static readonly Color DisconnectedPressed = Color.FromArgb(200, 65, 40);
    private static readonly Color ConnectedColor = Color.FromArgb(46, 190, 120);      // green
    private static readonly Color ConnectedHover = Color.FromArgb(60, 205, 135);
    private static readonly Color ConnectedPressed = Color.FromArgb(35, 165, 100);
    private static readonly Color BusyColor = Color.FromArgb(140, 140, 150);          // gray while (dis)connecting

    /// <summary>Reuses the AppController the LoadingForm already fetched/filtered servers with,
    /// so MainForm doesn't need to redo any of that work.</summary>
    public MainForm(AppController controller)
    {
        _controller = controller;
        InitializeComponent();
        LoadAssets();

        _controller.Log += OnControllerLog;
        _controller.StateChanged += OnControllerStateChanged;

        PopulateServerCombo();
        OnControllerStateChanged();
    }

    private void LoadAssets()
    {
        var logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        if (File.Exists(logoPath))
            picLogo.Image = Image.FromFile(logoPath);
    }

    // ---------- Server list ----------

    private void PopulateServerCombo()
    {
        _orderedServers.Clear();
        _orderedServers.AddRange(_controller.Servers);

        comboServers.Items = _orderedServers.Select(BuildDisplayText).ToArray();

        if (_orderedServers.Count > 0)
            comboServers.SelectedItem = comboServers.Items[0];

        UpdateConnectButtonEnabled();
    }

    private static string BuildDisplayText(ServerProfile server)
    {
        var flag = CountryFlags.FindFlag(server.Name);
        var country = CountryFlags.FindCountryName(server.Name) ?? server.Name;
        var ping = server.UrlLatencyMs != -1 && server.UrlLatencyMs != long.MaxValue
            ? server.UrlLatencyDisplay
            : server.TcpLatencyDisplay;
        return $"{flag}  {country}   —   {ping}";
    }

    private ServerProfile? SelectedServer =>
        comboServers.SelectedIndex >= 0 && comboServers.SelectedIndex < _orderedServers.Count
            ? _orderedServers[comboServers.SelectedIndex]
            : null;

    private void ComboServers_SelectedIndexChanged(object? sender, EventArgs e)
    {
        UpdateConnectButtonEnabled();
        cntryflagsetter(comboServers.SelectedItem);
    }

    private void UpdateConnectButtonEnabled()
    {
        var state = _controller.State;
        var idle = state == ConnectionState.Disconnected;
        var busy = state is ConnectionState.Connecting or ConnectionState.Disconnecting;

        btnConnectToggle.Enabled = !busy && (state == ConnectionState.Connected || (idle && SelectedServer != null));
        comboServers.Enabled = idle;
        btnRefresh.Enabled = idle;
    }

    // ---------- Connect / disconnect ----------

    private async void BtnConnectToggle_Click(object? sender, EventArgs e)
    {
        if (_controller.State == ConnectionState.Connected)
        {
            try
            {
                _controller.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error while disconnecting:\n{ex.Message}", "LocoVPN",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return;
        }

        var server = SelectedServer;
        if (server == null)
        {
            MessageBox.Show(this, "Select a server first.", "LocoVPN", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        try
        {
            await _controller.ConnectAsync(server);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Failed to connect:\n{ex.Message}", "LocoVPN",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnRefresh_Click(object? sender, EventArgs e)
    {
        btnRefresh.Enabled = false;
        comboServers.Enabled = false;
        btnConnectToggle.Enabled = false;
        try
        {
            await _controller.FetchAndFilterAsync();
            PopulateServerCombo();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Refresh failed:\n{ex.Message}", "LocoVPN",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UpdateConnectButtonEnabled();
        }
    }

    // ---------- Controller event handlers ----------

    private void OnControllerLog(string message)
    {
        if (InvokeRequired) { Invoke(() => OnControllerLog(message)); return; }
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void OnControllerStateChanged()
    {
        if (InvokeRequired) { Invoke(OnControllerStateChanged); return; }

        switch (_controller.State)
        {
            case ConnectionState.Connected:
                SetConnectButtonColors(ConnectedColor, ConnectedHover, ConnectedPressed);
                btnConnectToggle.Content = "Disconnect";
                lblConnectCaption.Content = "Disconnect";
                // 
                var flag = _controller.ConnectedServer != null ? CountryFlags.FindFlag(_controller.ConnectedServer.Name) : "";
                var name = _controller.ConnectedServer != null
                    ? CountryFlags.FindCountryName(_controller.ConnectedServer.Name) ?? _controller.ConnectedServer.Name
                    : "";
                //cntrypic.BackgroundImage = File.Exists(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{(_controller.ConnectedServer != null ? (CountryFlags.FindFlag(_controller.ConnectedServer.Name) ?? "US") : "US")}.png")) ? Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{(_controller.ConnectedServer != null ? (CountryFlags.FindFlag(_controller.ConnectedServer.Name) ?? "US") : "US")}.png")) : Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", "US.png"));
                cntrypic.BackgroundImage = File.Exists(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{(_controller.ConnectedServer != null ? (flag ?? "US") : "US")}.png")) ? Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{(_controller.ConnectedServer != null ? (flag ?? "US") : "US")}.png")) : Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", "US.png"));
                cntrypic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
                break;

            case ConnectionState.Connecting:
            case ConnectionState.Disconnecting:
                SetConnectButtonColors(BusyColor, BusyColor, BusyColor);
                btnConnectToggle.Content = _controller.State == ConnectionState.Connecting ? "Connecting…" : "Disconnecting…";
                lblConnectCaption.Content = btnConnectToggle.Content;
                break;

            default: // Disconnected
                SetConnectButtonColors(DisconnectedColor, DisconnectedHover, DisconnectedPressed);
                btnConnectToggle.Content = "Connect";
                lblConnectCaption.Content = "Connect";
                break;
        }

        UpdateConnectButtonEnabled();
    }

    private void SetConnectButtonColors(Color normal, Color hover, Color pressed)
    {
        btnConnectToggle.NormalBackground = normal;
        btnConnectToggle.HoverBackground = hover;
        btnConnectToggle.PressedBackground = pressed;
        btnConnectToggle.Invalidate();
    }

    // ---------- Exit ----------

    private void BtnExit_Click(object? sender, EventArgs e)
    {
        Close();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        _controller.ShutdownIfConnected();
    }

    private void cuiPictureBox1_Load(object sender, EventArgs e)
    {
        cntryflagsetter(comboServers.SelectedItem);
    }

    private void cntryflagsetter(string name)
    {
        var flag = CountryFlags.FindFlag(name);
        //cntrypic.BackgroundImage = File.Exists(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{(_controller.ConnectedServer != null ? (CountryFlags.FindFlag(_controller.ConnectedServer.Name) ?? "US") : "US")}.png")) ? Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{(_controller.ConnectedServer != null ? (CountryFlags.FindFlag(_controller.ConnectedServer.Name) ?? "US") : "US")}.png")) : Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", "US.png"));
        cntrypic.BackgroundImage = File.Exists(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{flag : 'US'}.png")) ? Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", $"{flag: 'US'}.png")) : Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "flags", "png", "US.png"));
        cntrypic.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
    }
}
