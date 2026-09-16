using System.ComponentModel;

namespace LocoVPN.Core;

public enum ConnectionState { Disconnected, Connecting, Connected, Disconnecting }

/// <summary>
/// All app logic lives here, deliberately independent of any specific Form or control
/// layout. Build whatever UI you like in the Visual Studio designer (or a UI toolkit
/// like Guna.UI2.WinForms - see README) and wire its buttons/grid to this class:
///
///   controller.Servers            -> DataSource for your grid/list control
///   controller.Log                -> event, append to a textbox/listbox
///   controller.StateChanged       -> event, update a status label/indicator
///   controller.FetchAndFilterAsync()   -> "Fetch" button
///   controller.RetestAsync()          -> "Test Latency" / "Refresh" button
///   controller.ConnectAsync(profile)  -> "Connect" button (pass the selected row's ServerProfile)
///   controller.Disconnect()           -> "Disconnect" button
///   controller.RecoverFromCrash()     -> call once at startup, before showing the form
/// </summary>
public class AppController : IDisposable
{
    public BindingList<ServerProfile> Servers { get; } = new();

    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public ServerProfile? ConnectedServer { get; private set; }

    /// <summary>Fired for every log-worthy event. Subscribers should marshal to the UI thread themselves
    /// if needed (WinForms Control.Invoke) - this event may fire from background tasks.</summary>
    public event Action<string>? Log;

    /// <summary>Fired whenever State or ConnectedServer changes.</summary>
    public event Action? StateChanged;

    /// <summary>Fired with an overall 0-100 completion percentage during FetchAndFilterAsync,
    /// for driving a loading screen's progress indicator. Roughly: 0-5% fetch, 5-50% TCP
    /// pre-filter, 50-55% ensuring xray-core, 55-100% real-tunnel URL test.</summary>
    public event Action<int>? ProgressChanged;

    private readonly ConfigFetcher _fetcher = new();
    private readonly TcpPingTester _tcpTester = new();
    private readonly UrlTester _urlTester = new();
    private readonly XrayManager _xray = new();
    private readonly SystemProxyManager _proxyManager = new();

    // ---------- Startup ----------

    /// <summary>Call once before showing the UI. If the app crashed last time while
    /// connected, restores the user's original proxy settings.</summary>
    public void RecoverFromCrash()
    {
        if (!XrayManager.WasUncleanShutdown()) return;

        try
        {
            _proxyManager.RestoreSavedState();
            Log?.Invoke("Detected an unclean previous shutdown - restored your original proxy settings.");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Could not auto-restore proxy after crash: {ex.Message}. Please check Windows proxy settings manually.");
        }
        finally
        {
            if (File.Exists(AppSettings.CrashFlagPath))
                File.Delete(AppSettings.CrashFlagPath);
        }
    }

    // ---------- Fetch + auto-filter ----------

    /// <summary>
    /// Fetches subscriptions, parses them, then (per AppSettings.AutoFilterOnFetch) runs
    /// a TCP pre-filter followed by a real-tunnel URL test, keeping only servers that
    /// passed both. Populates <see cref="Servers"/> with the final surviving set.
    /// </summary>
    public async Task FetchAndFilterAsync(CancellationToken ct = default)
    {
        ProgressChanged?.Invoke(0);
        Log?.Invoke("Fetching subscriptions...");
        var rawLines = await _fetcher.FetchAllAsync(progress: new Progress<string>(m => Log?.Invoke(m)), ct: ct);
        var parsed = ConfigParser.ParseAll(rawLines);
        Log?.Invoke($"Parsed {parsed.Count} usable server configs out of {rawLines.Count} raw entries.");
        ProgressChanged?.Invoke(5);

        if (!AppSettings.AutoFilterOnFetch)
        {
            ReplaceServers(parsed);
            ProgressChanged?.Invoke(100);
            return;
        }

        Log?.Invoke($"Running TCP pre-filter on {parsed.Count} servers...");
        var tcpDone = 0;
        await _tcpTester.TestAllAsync(parsed, new Progress<ServerProfile>(_ =>
        {
            var done = System.Threading.Interlocked.Increment(ref tcpDone);
            // TCP filter spans 5% -> 50% of the overall bar.
            var pct = parsed.Count == 0 ? 50 : 5 + (int)(45.0 * done / parsed.Count);
            ProgressChanged?.Invoke(pct);
        }), ct);
        var tcpSurvivors = parsed.Where(p => p.TcpLatencyMs != long.MaxValue).ToList();
        Log?.Invoke($"TCP filter: {tcpSurvivors.Count}/{parsed.Count} servers reachable.");
        ProgressChanged?.Invoke(50);

        Log?.Invoke("Ensuring xray-core is installed for the URL test...");
        try
        {
            await _xray.EnsureXrayInstalledAsync(new Progress<string>(m => Log?.Invoke(m)), ct);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Could not install xray-core, skipping URL test and keeping TCP-only results: {ex.Message}");
            ReplaceServers(tcpSurvivors);
            ProgressChanged?.Invoke(100);
            return;
        }
        ProgressChanged?.Invoke(55);

        Log?.Invoke($"Running real-tunnel URL test on {tcpSurvivors.Count} servers (this takes longer)...");
        var urlDone = 0;
        await _urlTester.TestAllAsync(tcpSurvivors, new Progress<ServerProfile>(_ =>
        {
            var done = System.Threading.Interlocked.Increment(ref urlDone);
            // URL test spans 55% -> 100% of the overall bar.
            var pct = tcpSurvivors.Count == 0 ? 100 : 55 + (int)(45.0 * done / tcpSurvivors.Count);
            ProgressChanged?.Invoke(pct);
        }), ct);
        var urlSurvivors = tcpSurvivors.Where(p => p.UrlLatencyMs != long.MaxValue).ToList();
        Log?.Invoke($"URL test: {urlSurvivors.Count}/{tcpSurvivors.Count} servers actually usable. Keeping only these.");

        ReplaceServers(urlSurvivors);
        ProgressChanged?.Invoke(100);
    }

    /// <summary>Re-runs TCP + URL tests on the currently-listed servers without discarding any
    /// (use this for a manual "Test Latency" / "Refresh" button after the initial auto-filter).</summary>
    public async Task RetestAsync(CancellationToken ct = default)
    {
        var current = Servers.ToList();
        if (current.Count == 0)
        {
            Log?.Invoke("No servers to test - fetch some first.");
            return;
        }

        Log?.Invoke($"Re-testing {current.Count} servers (TCP)...");
        await _tcpTester.TestAllAsync(current, new Progress<ServerProfile>(RefreshRow), ct);

        try
        {
            await _xray.EnsureXrayInstalledAsync(ct: ct);
            Log?.Invoke("Re-testing (URL)...");
            await _urlTester.TestAllAsync(current, new Progress<ServerProfile>(RefreshRow), ct);
        }
        catch (Exception ex)
        {
            Log?.Invoke($"URL re-test skipped: {ex.Message}");
        }

        Log?.Invoke("Re-test complete.");
    }

    private void ReplaceServers(List<ServerProfile> servers)
    {
        Servers.Clear();
        foreach (var s in servers)
            Servers.Add(s);
    }

    private void RefreshRow(ServerProfile p)
    {
        var index = Servers.IndexOf(p);
        if (index >= 0) Servers.ResetItem(index);
    }

    // ---------- Connect / disconnect ----------

    public async Task ConnectAsync(ServerProfile server, CancellationToken ct = default)
    {
        SetState(ConnectionState.Connecting, null);
        Log?.Invoke($"Connecting to {server.Name}...");
        try
        {
            await _xray.EnsureXrayInstalledAsync(new Progress<string>(m => Log?.Invoke(m)), ct);
            _xray.GenerateConfig(server);

            _proxyManager.BackupCurrentState();
            _xray.Start();

            await Task.Delay(700, ct);
            if (!_xray.IsRunning)
                throw new InvalidOperationException("xray.exe exited immediately after starting - check the generated config.json for errors.");

            _proxyManager.ApplyXrayProxy();

            SetState(ConnectionState.Connected, server);
            Log?.Invoke($"Connected via {server.Protocol} {server.Address}:{server.Port}. " +
                        $"System HTTP proxy -> 127.0.0.1:{AppSettings.HttpPort}, SOCKS on 127.0.0.1:{AppSettings.SocksPort}.");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Connect failed: {ex.Message}");
            try { _xray.Stop(); } catch { /* root cause already logged */ }
            SetState(ConnectionState.Disconnected, null);
            throw;
        }
    }

    public void Disconnect()
    {
        SetState(ConnectionState.Disconnecting, ConnectedServer);
        Log?.Invoke("Disconnecting...");
        try
        {
            _xray.Stop();
            _proxyManager.RestoreSavedState();
            Log?.Invoke("Disconnected and restored original proxy settings.");
        }
        catch (Exception ex)
        {
            Log?.Invoke($"Disconnect encountered an error: {ex.Message}");
            throw;
        }
        finally
        {
            SetState(ConnectionState.Disconnected, null);
        }
    }

    /// <summary>Call from your Form's FormClosing handler so a live connection is
    /// always torn down cleanly when the app exits normally.</summary>
    public void ShutdownIfConnected()
    {
        if (State != ConnectionState.Connected) return;
        try
        {
            _xray.Stop();
            _proxyManager.RestoreSavedState();
        }
        catch { /* crash-flag recovery on next launch is the safety net here */ }
    }

    private void SetState(ConnectionState state, ServerProfile? server)
    {
        State = state;
        ConnectedServer = server;
        StateChanged?.Invoke();
    }

    public void Dispose() => _xray.Dispose();
}
