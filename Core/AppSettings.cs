namespace LocoVPN.Core;

/// <summary>
/// Central place for paths, ports, and constants. Change these here rather than
/// hunting through the rest of the codebase.
/// </summary>
public static class AppSettings
{
    public const string AppName = "LocoVPN";

    // %AppData%\LocoVPN\...
    public static readonly string RootDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);

    public static readonly string CoreDir = Path.Combine(RootDir, "core");
    public static readonly string ConfigDir = Path.Combine(RootDir, "config");
    public static readonly string XrayExePath = Path.Combine(CoreDir, "xray.exe");
    public static readonly string GeneratedConfigPath = Path.Combine(ConfigDir, "config.json");
    public static readonly string CrashFlagPath = Path.Combine(RootDir, "connected.flag");
    public static readonly string SavedProxyStatePath = Path.Combine(RootDir, "proxy_backup.json");

    /// <summary>
    /// Places to look for an xray.exe you've bundled with the project yourself, checked
    /// in order, BEFORE falling back to the cached %AppData% copy or a GitHub download.
    /// Drop your own xray.exe in the project's output folder (or a "core" subfolder next
    /// to the .exe) and it will be picked up automatically - see XrayManager.EnsureXrayInstalledAsync.
    /// </summary>
    public static string[] BundledXrayCandidatePaths => new[]
    {
        Path.Combine(AppContext.BaseDirectory, "xray.exe"),
        Path.Combine(AppContext.BaseDirectory, "core", "xray.exe"),
        Path.Combine(AppContext.BaseDirectory, "Assets", "xray.exe"),
    };

    // Local listener ports Xray will bind to.
    public const int SocksPort = 10808;
    public const int HttpPort = 10809;

    // Latency / URL test target.
    public const string LatencyTestUrl = "http://www.gstatic.com/generate_204";
    public const int LatencyTimeoutMs = 3000;
    public const int LatencyMaxConcurrency = 5;

    // A "real tunnel" URL test spins up a short-lived xray.exe per server, so it's
    // heavier than the plain TCP test - keep concurrency lower to avoid hammering
    // the machine with dozens of simultaneous child processes.
    public const int UrlTestTimeoutMs = 5000;
    public const int UrlTestMaxConcurrency = 3;

    // On Fetch, automatically TCP-test then URL-test the results and drop anything
    // that times out on either, leaving only servers that are actually reachable.
    public const bool AutoFilterOnFetch = true;

    // Subscription sources carried over from the original crawl.py list.
    // Edit this list to add/remove subscription sources.
    public static readonly string[] SubscriptionUrls =
    {
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no9.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no1.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no2.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no3.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no4.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no5.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no7.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no8.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no10.txt",
        "https://raw.githubusercontent.com/V2RAYCONFIGSPOOL/V2RAY_SUB/refs/heads/main/v2ray_configs_no6.txt",
    };

    // Official Xray-core release asset name for 64-bit Windows.
    public const string XrayReleaseApiUrl = "https://api.github.com/repos/XTLS/Xray-core/releases/latest";
    public const string XrayWindowsAssetName = "Xray-windows-64.zip";

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(RootDir);
        Directory.CreateDirectory(CoreDir);
        Directory.CreateDirectory(ConfigDir);
    }
}
