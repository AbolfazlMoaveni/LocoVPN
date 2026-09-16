using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text.Json.Nodes;

namespace LocoVPN.Core;

/// <summary>
/// Fast reachability test: just times a raw TCP connect to the server's host:port.
/// Cheap enough to run against hundreds of servers at once - use this as a first pass
/// to weed out obviously-dead entries before running the heavier <see cref="UrlTester"/>.
/// </summary>
public class TcpPingTester
{
    public async Task TestAllAsync(
        IEnumerable<ServerProfile> servers,
        IProgress<ServerProfile>? progress = null,
        CancellationToken ct = default)
    {
        using var gate = new SemaphoreSlim(AppSettings.LatencyMaxConcurrency);
        var tasks = servers.Select(async server =>
        {
            await gate.WaitAsync(ct);
            try
            {
                server.TcpLatencyMs = await TestOneAsync(server, ct);
                progress?.Report(server);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task<long> TestOneAsync(ServerProfile server, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(server.Address) || server.Port <= 0)
            return long.MaxValue;

        using var client = new TcpClient();
        var sw = Stopwatch.StartNew();

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(AppSettings.LatencyTimeoutMs);

            await client.ConnectAsync(server.Address, server.Port, timeoutCts.Token);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
        catch
        {
            return long.MaxValue;
        }
    }
}

/// <summary>
/// Real "URL-test" latency: launches a short-lived, isolated xray.exe instance per
/// server (its own temp config, its own ephemeral SOCKS port), routes an HTTP GET to
/// <see cref="AppSettings.LatencyTestUrl"/> through it via .NET's built-in SOCKS5 proxy
/// support, times the round trip, then tears the process down. This is the same
/// technique GUI clients like v2rayN mean by "URL test" - it proves the whole tunnel
/// (handshake, TLS, routing) actually works, not just that the TCP port is open.
///
/// Requires xray.exe to already be installed (call XrayManager.EnsureXrayInstalledAsync
/// once before running a batch of these). Keep concurrency modest - each test is a full
/// child process, unlike the lightweight TcpPingTester above.
/// </summary>
public class UrlTester
{
    public async Task TestAllAsync(
        IEnumerable<ServerProfile> servers,
        IProgress<ServerProfile>? progress = null,
        CancellationToken ct = default)
    {
        using var gate = new SemaphoreSlim(AppSettings.UrlTestMaxConcurrency);
        var tasks = servers.Select(async server =>
        {
            await gate.WaitAsync(ct);
            try
            {
                server.UrlLatencyMs = await TestOneAsync(server, ct);
                progress?.Report(server);
            }
            finally
            {
                gate.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task<long> TestOneAsync(ServerProfile server, CancellationToken ct = default)
    {
        var exePath = XrayManager.ResolveExistingXrayPath();
        if (exePath == null)
            return long.MaxValue; // xray-core not installed/bundled yet - caller should Ensure it first.

        int port;
        try
        {
            port = GetFreeTcpPort();
        }
        catch
        {
            return long.MaxValue;
        }

        var configPath = Path.Combine(Path.GetTempPath(), $"xray-urltest-{Guid.NewGuid():N}.json");
        Process? process = null;

        try
        {
            var config = new JsonObject
            {
                ["log"] = new JsonObject { ["loglevel"] = "none" },
                ["inbounds"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["listen"] = "127.0.0.1",
                        ["port"] = port,
                        ["protocol"] = "socks",
                        ["settings"] = new JsonObject { ["udp"] = false },
                        ["tag"] = "socks-in"
                    }
                },
                ["outbounds"] = ConfigParser.BuildOutbounds(server)
            };
            File.WriteAllText(configPath, config.ToJsonString());

            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = $"-config \"{configPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            process = new Process { StartInfo = psi };
            process.Start();

            // Give xray a moment to bind its listener; bail early if it died on startup
            // (bad outbound config, etc.) instead of waiting out the full HTTP timeout.
            await Task.Delay(400, ct);
            if (process.HasExited)
                return long.MaxValue;

            using var handler = new SocketsHttpHandler
            {
                Proxy = new WebProxy(new Uri($"socks5://127.0.0.1:{port}")),
                UseProxy = true
            };
            using var client = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(AppSettings.UrlTestTimeoutMs) };

            var sw = Stopwatch.StartNew();
            using var response = await client.GetAsync(AppSettings.LatencyTestUrl, ct);
            sw.Stop();

            // generate_204 returns HTTP 204 with an empty body on success.
            return response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.OK
                ? sw.ElapsedMilliseconds
                : long.MaxValue;
        }
        catch
        {
            return long.MaxValue;
        }
        finally
        {
            try
            {
                if (process is { HasExited: false })
                    process.Kill(entireProcessTree: true);
            }
            catch { /* best-effort cleanup */ }
            process?.Dispose();

            try { File.Delete(configPath); } catch { /* best-effort cleanup */ }
        }
    }

    private static int GetFreeTcpPort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
