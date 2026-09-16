using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32;

namespace LocoVPN.Core;

/// <summary>
/// Snapshot of the values we touch under
/// HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings.
/// </summary>
public class ProxyState
{
    public bool ProxyEnable { get; set; }
    public string? ProxyServer { get; set; }
    public string? ProxyOverride { get; set; }
}

/// <summary>
/// Reads/writes the current user's system proxy settings via the registry
/// (the same keys Internet Options / Windows Settings uses), and notifies
/// the system so apps like browsers pick up the change immediately.
/// </summary>
public class SystemProxyManager
{
    private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";

    [DllImport("wininet.dll", SetLastError = true)]
    private static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);

    private const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
    private const int INTERNET_OPTION_REFRESH = 37;

    /// <summary>Reads the current proxy settings without changing anything.</summary>
    public ProxyState ReadCurrentState()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: false);
        return new ProxyState
        {
            ProxyEnable = (int?)key?.GetValue("ProxyEnable") == 1,
            ProxyServer = key?.GetValue("ProxyServer") as string,
            ProxyOverride = key?.GetValue("ProxyOverride") as string
        };
    }

    /// <summary>Persists the current proxy state to disk so it can survive an app crash.</summary>
    public void BackupCurrentState()
    {
        var state = ReadCurrentState();
        var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        AppSettings.EnsureDirectories();
        File.WriteAllText(AppSettings.SavedProxyStatePath, json);
    }

    /// <summary>
    /// Points the system proxy at the local Xray listeners. Uses WinINet's per-protocol
    /// "ProxyServer" format (the same format Internet Options → LAN Settings → Advanced
    /// writes) so both HTTP(S) traffic AND SOCKS-aware applications get routed through
    /// Xray - not just plain HTTP. Equivalent to manually entering:
    ///   http=127.0.0.1:{HttpPort};https=127.0.0.1:{HttpPort};socks=127.0.0.1:{SocksPort}
    /// in the Advanced Proxy Settings dialog.
    /// </summary>
    /// <remarks>
    /// This is still WinINet-level proxying, not a true system-wide TUN/packet-capture
    /// solution: it covers apps and browsers that read the standard Windows/WinINet proxy
    /// settings (which is most of them, including the "socks=" entry for apps that support
    /// SOCKS), but apps with their own network stack (some UWP apps, some games) can bypass
    /// it entirely. A genuinely all-traffic solution would require a TUN adapter (e.g. via
    /// WinTun/wintun.dll) routing through Xray's tun inbound - a much bigger change than a
    /// registry proxy toggle, and out of scope here.
    /// </remarks>
    public void ApplyXrayProxy()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true)
            ?? throw new InvalidOperationException("Could not open Internet Settings registry key.");

        var proxyServerValue =
            $"http=127.0.0.1:{AppSettings.HttpPort};" +
            $"https=127.0.0.1:{AppSettings.HttpPort};" +
            $"socks=127.0.0.1:{AppSettings.SocksPort}";

        key.SetValue("ProxyEnable", 1, RegistryValueKind.DWord);
        key.SetValue("ProxyServer", proxyServerValue, RegistryValueKind.String);
        key.SetValue("ProxyOverride", "<local>", RegistryValueKind.String);

        NotifySystemOfChange();
    }

    /// <summary>Restores whatever proxy state was captured by BackupCurrentState().</summary>
    public void RestoreSavedState()
    {
        if (!File.Exists(AppSettings.SavedProxyStatePath))
        {
            // Nothing was ever backed up (e.g. first run after a crash before any
            // connect happened) - safest fallback is simply disabling the proxy.
            DisableProxy();
            return;
        }

        var json = File.ReadAllText(AppSettings.SavedProxyStatePath);
        var state = JsonSerializer.Deserialize<ProxyState>(json) ?? new ProxyState();

        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true)
            ?? throw new InvalidOperationException("Could not open Internet Settings registry key.");

        key.SetValue("ProxyEnable", state.ProxyEnable ? 1 : 0, RegistryValueKind.DWord);
        if (state.ProxyServer != null)
            key.SetValue("ProxyServer", state.ProxyServer, RegistryValueKind.String);
        if (state.ProxyOverride != null)
            key.SetValue("ProxyOverride", state.ProxyOverride, RegistryValueKind.String);

        NotifySystemOfChange();
        File.Delete(AppSettings.SavedProxyStatePath);
    }

    public void DisableProxy()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegPath, writable: true);
        key?.SetValue("ProxyEnable", 0, RegistryValueKind.DWord);
        NotifySystemOfChange();
    }

    private static void NotifySystemOfChange()
    {
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
        InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
    }
}
