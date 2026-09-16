using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json.Nodes;

namespace LocoVPN.Core;

/// <summary>
/// Handles fetching Xray-core, generating the connection config, and starting/
/// stopping the xray.exe child process.
/// </summary>
public class XrayManager : IDisposable
{
    private readonly HttpClient _http;
    private Process? _process;
    private string? _resolvedPath;

    public bool IsRunning => _process is { HasExited: false };

    /// <summary>Full path to the xray.exe actually in use (bundled, cached, or downloaded),
    /// once resolved by EnsureXrayInstalledAsync. Null until then.</summary>
    public string? ResolvedPath => _resolvedPath;

    public XrayManager(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LocoVPN/1.0");
    }

    /// <summary>
    /// Checks, in order: xray.exe bundled in the project/output folder
    /// (AppSettings.BundledXrayCandidatePaths), then the cached copy under
    /// %AppData%\LocoVPN\core\xray.exe. Returns the first one found, or null if neither exists.
    /// </summary>
    public static string? ResolveExistingXrayPath()
    {
        foreach (var candidate in AppSettings.BundledXrayCandidatePaths)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return File.Exists(AppSettings.XrayExePath) ? AppSettings.XrayExePath : null;
    }

    /// <summary>
    /// Resolves an xray.exe to use, preferring one you've already supplied (in the project's
    /// output folder, or previously cached under %AppData%) over downloading anything. Only
    /// reaches out to GitHub if no local copy can be found anywhere.
    /// </summary>
    public async Task<string> EnsureXrayInstalledAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        AppSettings.EnsureDirectories();

        var existing = ResolveExistingXrayPath();
        if (existing != null)
        {
            _resolvedPath = existing;
            var isBundled = AppSettings.BundledXrayCandidatePaths.Contains(existing);
            progress?.Report(isBundled
                ? $"Using bundled xray.exe found at {existing} - skipping download."
                : "xray.exe already cached, skipping download.");
            return existing;
        }

        progress?.Report("Looking up latest Xray-core release...");
        var releaseJson = await _http.GetStringAsync(AppSettings.XrayReleaseApiUrl, ct);
        var release = JsonNode.Parse(releaseJson)!.AsObject();
        var assets = release["assets"]!.AsArray();

        string? zipUrl = null, digestUrl = null;
        foreach (var asset in assets)
        {
            var assetName = asset!["name"]!.ToString();
            if (assetName == AppSettings.XrayWindowsAssetName)
                zipUrl = asset["browser_download_url"]!.ToString();
            else if (assetName == AppSettings.XrayWindowsAssetName + ".dgst")
                digestUrl = asset["browser_download_url"]!.ToString();
        }

        if (zipUrl is null)
            throw new InvalidOperationException($"Could not find {AppSettings.XrayWindowsAssetName} in the latest release assets.");

        var tempZip = Path.Combine(Path.GetTempPath(), $"xray-{Guid.NewGuid():N}.zip");
        progress?.Report("Downloading xray-core...");

        await using (var stream = await _http.GetStreamAsync(zipUrl, ct))
        await using (var fileStream = File.Create(tempZip))
        {
            await stream.CopyToAsync(fileStream, ct);
        }

        if (digestUrl != null)
        {
            progress?.Report("Verifying SHA-256 checksum...");
            var digestText = await _http.GetStringAsync(digestUrl, ct);
            var expectedSha256 = ExtractSha256FromDigest(digestText);

            if (expectedSha256 != null)
            {
                var actualSha256 = await ComputeSha256Async(tempZip, ct);
                if (!string.Equals(expectedSha256, actualSha256, StringComparison.OrdinalIgnoreCase))
                {
                    File.Delete(tempZip);
                    throw new InvalidOperationException(
                        "SHA-256 checksum mismatch for downloaded xray-core archive. Aborting install for safety.");
                }
                progress?.Report("Checksum verified OK.");
            }
            else
            {
                progress?.Report("WARNING: could not parse checksum file - proceeding without verification.");
            }
        }
        else
        {
            progress?.Report("WARNING: no .dgst file published for this release - proceeding without checksum verification.");
        }

        progress?.Report("Extracting xray.exe...");
        using (var archive = ZipFile.OpenRead(tempZip))
        {
            var entry = archive.GetEntry("xray.exe")
                ?? throw new InvalidOperationException("xray.exe not found inside downloaded archive.");
            entry.ExtractToFile(AppSettings.XrayExePath, overwrite: true);
        }

        File.Delete(tempZip);
        progress?.Report("xray-core installed.");
        return AppSettings.XrayExePath;
    }

    private static string? ExtractSha256FromDigest(string digestText)
    {
        // The .dgst file has lines like: "SHA2-256= <hex>"
        foreach (var line in digestText.Split('\n'))
        {
            if (line.StartsWith("SHA2-256", StringComparison.OrdinalIgnoreCase))
            {
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                    return parts[1].Trim();
            }
        }
        return null;
    }

    private static async Task<string> ComputeSha256Async(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Writes an Xray config.json that binds SOCKS+HTTP inbounds on localhost and
    /// routes everything through the given server's outbound.
    /// </summary>
    public string GenerateConfig(ServerProfile server)
    {
        AppSettings.EnsureDirectories();

        var config = new JsonObject
        {
            ["log"] = new JsonObject { ["loglevel"] = "warning" },
            ["inbounds"] = new JsonArray
            {
                new JsonObject
                {
                    ["listen"] = "127.0.0.1",
                    ["port"] = AppSettings.SocksPort,
                    ["protocol"] = "socks",
                    ["settings"] = new JsonObject { ["udp"] = true },
                    ["tag"] = "socks-in"
                },
                new JsonObject
                {
                    ["listen"] = "127.0.0.1",
                    ["port"] = AppSettings.HttpPort,
                    ["protocol"] = "http",
                    ["tag"] = "http-in"
                }
            },
            ["outbounds"] = ConfigParser.BuildOutbounds(server)
        };

        var json = config.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(AppSettings.GeneratedConfigPath, json);
        return AppSettings.GeneratedConfigPath;
    }

    /// <summary>Starts xray.exe as a hidden background process using the generated config.</summary>
    public void Start()
    {
        if (IsRunning) return;

        var exePath = _resolvedPath ?? ResolveExistingXrayPath()
            ?? throw new FileNotFoundException("xray.exe not installed. Call EnsureXrayInstalledAsync first.");
        _resolvedPath = exePath;

        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"-config \"{AppSettings.GeneratedConfigPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.Start();

        // Drop a flag file so we can detect+recover from an unclean shutdown on next launch.
        File.WriteAllText(AppSettings.CrashFlagPath, DateTime.UtcNow.ToString("O"));
    }

    /// <summary>Kills the xray.exe process cleanly, if running.</summary>
    public void Stop()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        finally
        {
            _process?.Dispose();
            _process = null;
            if (File.Exists(AppSettings.CrashFlagPath))
                File.Delete(AppSettings.CrashFlagPath);
        }
    }

    /// <summary>True if a crash flag from a previous unclean shutdown exists.</summary>
    public static bool WasUncleanShutdown() => File.Exists(AppSettings.CrashFlagPath);

    public void Dispose() => Stop();
}
