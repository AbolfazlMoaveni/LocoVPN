namespace LocoVPN.Core;

public enum ProxyProtocol
{
    Unknown,
    VMess,
    VLess,
    Trojan,
    Shadowsocks
}

/// <summary>
/// A single parsed proxy server entry, ready to be shown in the grid and
/// (if selected) turned into an Xray outbound.
/// </summary>
public class ServerProfile
{
    public string Name { get; set; } = "(unnamed)";
    public ProxyProtocol Protocol { get; set; } = ProxyProtocol.Unknown;
    public string Address { get; set; } = "";
    public int Port { get; set; }

    /// <summary>The original share-link (vmess://, vless://, trojan://, ss://).</summary>
    public string RawUri { get; set; } = "";

    /// <summary>Protocol-specific fields needed to build an Xray outbound.</summary>
    public Dictionary<string, string> Extra { get; set; } = new();

    /// <summary>Raw TCP-connect latency in ms, or -1 if untested, or long.MaxValue if unreachable.</summary>
    public long TcpLatencyMs { get; set; } = -1;

    /// <summary>Round-trip time in ms for an HTTP request routed through a real Xray tunnel to this
    /// server, or -1 if untested, or long.MaxValue if the tunnel failed / request timed out.</summary>
    public long UrlLatencyMs { get; set; } = -1;

    public string TcpLatencyDisplay => FormatLatency(TcpLatencyMs);
    public string UrlLatencyDisplay => FormatLatency(UrlLatencyMs);

    private static string FormatLatency(long ms) => ms switch
    {
        -1 => "untested",
        long.MaxValue => "timeout",
        _ => $"{ms} ms"
    };

    public override string ToString() => $"{Name} ({Protocol} {Address}:{Port})";
}
