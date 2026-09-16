using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using YamlDotNet.RepresentationModel;

namespace LocoVPN.Core;

/// <summary>
/// Parses raw share-links (and basic Clash YAML) into <see cref="ServerProfile"/>
/// objects, and converts a profile into an Xray-compatible outbound JSON object.
/// </summary>
public static class ConfigParser
{
    /// <summary>Parses a list of raw lines. Lines that fail to parse are skipped.</summary>
    public static List<ServerProfile> ParseAll(IEnumerable<string> rawLines)
    {
        var results = new List<ServerProfile>();
        foreach (var line in rawLines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            try
            {
                ServerProfile? profile = trimmed switch
                {
                    _ when trimmed.StartsWith("vmess://", StringComparison.OrdinalIgnoreCase) => ParseVMess(trimmed),
                    _ when trimmed.StartsWith("vless://", StringComparison.OrdinalIgnoreCase) => ParseVLess(trimmed),
                    _ when trimmed.StartsWith("trojan://", StringComparison.OrdinalIgnoreCase) => ParseTrojan(trimmed),
                    _ when trimmed.StartsWith("ss://", StringComparison.OrdinalIgnoreCase) => ParseShadowsocks(trimmed),
                    _ => null
                };

                if (profile != null)
                    results.Add(profile);
            }
            catch
            {
                // Skip malformed entries rather than aborting the whole batch.
            }
        }
        return results;
    }

    /// <summary>
    /// Parses a Clash-style YAML document's "proxies:" list. Only the common
    /// vmess/vless/trojan/ss proxy types are supported (not full Clash rule sets).
    /// </summary>
    public static List<ServerProfile> ParseClashYaml(string yamlText)
    {
        var results = new List<ServerProfile>();
        var yaml = new YamlStream();
        yaml.Load(new StringReader(yamlText));

        if (yaml.Documents.Count == 0) return results;
        var root = (YamlMappingNode)yaml.Documents[0].RootNode;

        if (!root.Children.TryGetValue(new YamlScalarNode("proxies"), out var proxiesNode))
            return results;

        foreach (var item in (YamlSequenceNode)proxiesNode)
        {
            var map = (YamlMappingNode)item;
            string Get(string key) => map.Children.TryGetValue(new YamlScalarNode(key), out var v) ? v.ToString() : "";

            var type = Get("type").ToLowerInvariant();
            var profile = new ServerProfile
            {
                Name = Get("name"),
                Address = Get("server"),
                Port = int.TryParse(Get("port"), out var p) ? p : 0
            };

            switch (type)
            {
                case "vmess":
                    profile.Protocol = ProxyProtocol.VMess;
                    profile.Extra["uuid"] = Get("uuid");
                    profile.Extra["alterId"] = string.IsNullOrEmpty(Get("alterId")) ? "0" : Get("alterId");
                    profile.Extra["network"] = string.IsNullOrEmpty(Get("network")) ? "tcp" : Get("network");
                    break;
                case "vless":
                    profile.Protocol = ProxyProtocol.VLess;
                    profile.Extra["uuid"] = Get("uuid");
                    profile.Extra["network"] = string.IsNullOrEmpty(Get("network")) ? "tcp" : Get("network");
                    profile.Extra["security"] = Get("tls") == "true" ? "tls" : "none";
                    break;
                case "trojan":
                    profile.Protocol = ProxyProtocol.Trojan;
                    profile.Extra["password"] = Get("password");
                    break;
                case "ss":
                case "shadowsocks":
                    profile.Protocol = ProxyProtocol.Shadowsocks;
                    profile.Extra["method"] = Get("cipher");
                    profile.Extra["password"] = Get("password");
                    break;
                default:
                    continue; // Unsupported proxy type (e.g. snell, wireguard) - skip.
            }

            if (!string.IsNullOrEmpty(profile.Address) && profile.Port > 0)
                results.Add(profile);
        }

        return results;
    }

    // ---------- Individual protocol parsers ----------

    private static ServerProfile ParseVMess(string uri)
    {
        var b64 = uri["vmess://".Length..];
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(b64)));
        var node = JsonNode.Parse(json)!.AsObject();

        string S(string key) => node[key]?.ToString() ?? "";

        return new ServerProfile
        {
            Name = string.IsNullOrEmpty(S("ps")) ? S("add") : S("ps"),
            Protocol = ProxyProtocol.VMess,
            Address = S("add"),
            Port = int.Parse(S("port")),
            RawUri = uri,
            Extra =
            {
                ["uuid"] = S("id"),
                ["alterId"] = string.IsNullOrEmpty(S("aid")) ? "0" : S("aid"),
                ["network"] = string.IsNullOrEmpty(S("net")) ? "tcp" : S("net"),
                ["host"] = S("host"),
                ["path"] = S("path"),
                ["tls"] = S("tls"),
                ["sni"] = S("sni")
            }
        };
    }

    private static ServerProfile ParseVLess(string uri)
    {
        var parsed = new Uri(uri);
        var query = ParseQuery(parsed.Query);
        var name = Uri.UnescapeDataString(parsed.Fragment.TrimStart('#'));

        return new ServerProfile
        {
            Name = string.IsNullOrEmpty(name) ? parsed.Host : name,
            Protocol = ProxyProtocol.VLess,
            Address = parsed.Host,
            Port = parsed.Port,
            RawUri = uri,
            Extra =
            {
                ["uuid"] = parsed.UserInfo,
                ["network"] = query.GetValueOrDefault("type", "tcp"),
                ["security"] = query.GetValueOrDefault("security", "none"),
                ["sni"] = query.GetValueOrDefault("sni", ""),
                ["path"] = query.GetValueOrDefault("path", ""),
                ["host"] = query.GetValueOrDefault("host", ""),
                ["flow"] = query.GetValueOrDefault("flow", "")
            }
        };
    }

    private static ServerProfile ParseTrojan(string uri)
    {
        var parsed = new Uri(uri);
        var query = ParseQuery(parsed.Query);
        var name = Uri.UnescapeDataString(parsed.Fragment.TrimStart('#'));

        return new ServerProfile
        {
            Name = string.IsNullOrEmpty(name) ? parsed.Host : name,
            Protocol = ProxyProtocol.Trojan,
            Address = parsed.Host,
            Port = parsed.Port,
            RawUri = uri,
            Extra =
            {
                ["password"] = parsed.UserInfo,
                ["sni"] = query.GetValueOrDefault("sni", parsed.Host),
                ["network"] = query.GetValueOrDefault("type", "tcp")
            }
        };
    }

    /// <summary>Minimal query-string parser (avoids taking a System.Web dependency).</summary>
    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(query)) return result;

        query = query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(kv[0]);
            var value = kv.Length > 1 ? Uri.UnescapeDataString(kv[1]) : "";
            result[key] = value;
        }
        return result;
    }

    private static ServerProfile ParseShadowsocks(string uri)
    {
        // Two common forms:
        //  ss://BASE64(method:password)@host:port#name
        //  ss://BASE64(method:password@host:port)#name   (legacy, whole userinfo section encoded)
        var withoutScheme = uri["ss://".Length..];
        var hashIndex = withoutScheme.IndexOf('#');
        var name = hashIndex >= 0 ? Uri.UnescapeDataString(withoutScheme[(hashIndex + 1)..]) : "";
        var body = hashIndex >= 0 ? withoutScheme[..hashIndex] : withoutScheme;

        string method, password, host;
        int port;

        if (body.Contains('@'))
        {
            var atIndex = body.LastIndexOf('@');
            var userInfo = body[..atIndex];
            var hostPort = body[(atIndex + 1)..];

            // userInfo may itself be base64("method:password")
            string decodedUserInfo;
            try { decodedUserInfo = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(userInfo))); }
            catch { decodedUserInfo = userInfo; } // already plaintext method:password

            var parts = decodedUserInfo.Split(':', 2);
            method = parts[0];
            password = parts.Length > 1 ? parts[1] : "";

            var hp = hostPort.Split(':', 2);
            host = hp[0];
            port = int.Parse(hp[1]);
        }
        else
        {
            // Fully encoded legacy form: base64("method:password@host:port")
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(PadBase64(body)));
            var atIndex = decoded.LastIndexOf('@');
            var userInfo = decoded[..atIndex];
            var hostPort = decoded[(atIndex + 1)..];

            var parts = userInfo.Split(':', 2);
            method = parts[0];
            password = parts.Length > 1 ? parts[1] : "";

            var hp = hostPort.Split(':', 2);
            host = hp[0];
            port = int.Parse(hp[1]);
        }

        return new ServerProfile
        {
            Name = string.IsNullOrEmpty(name) ? host : name,
            Protocol = ProxyProtocol.Shadowsocks,
            Address = host,
            Port = port,
            RawUri = uri,
            Extra =
            {
                ["method"] = method,
                ["password"] = password
            }
        };
    }

    private static string PadBase64(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        var remainder = s.Length % 4;
        return remainder == 0 ? s : s + new string('=', 4 - remainder);
    }

    // ---------- Xray outbound JSON generation ----------

    /// <summary>
    /// Builds an Xray "outbounds" array containing one proxy outbound (tag "proxy")
    /// plus a "direct" and "block" outbound, matching what XrayManager expects.
    /// </summary>
    public static JsonArray BuildOutbounds(ServerProfile p)
    {
        JsonObject proxyOutbound = p.Protocol switch
        {
            ProxyProtocol.VMess => BuildVMessOutbound(p),
            ProxyProtocol.VLess => BuildVLessOutbound(p),
            ProxyProtocol.Trojan => BuildTrojanOutbound(p),
            ProxyProtocol.Shadowsocks => BuildShadowsocksOutbound(p),
            _ => throw new NotSupportedException($"Protocol {p.Protocol} not supported")
        };
        proxyOutbound["tag"] = "proxy";

        return new JsonArray
        {
            proxyOutbound,
            new JsonObject { ["protocol"] = "freedom", ["tag"] = "direct" },
            new JsonObject { ["protocol"] = "blackhole", ["tag"] = "block" }
        };
    }

    private static JsonObject StreamSettings(ServerProfile p)
    {
        var network = p.Extra.GetValueOrDefault("network", "tcp");
        var security = p.Extra.GetValueOrDefault("security", p.Extra.GetValueOrDefault("tls", "") == "tls" ? "tls" : "none");

        var stream = new JsonObject
        {
            ["network"] = network,
            ["security"] = security
        };

        if (security == "tls")
        {
            var sni = p.Extra.GetValueOrDefault("sni", p.Address);
            stream["tlsSettings"] = new JsonObject { ["serverName"] = sni, ["allowInsecure"] = false };
        }

        if (network == "ws")
        {
            stream["wsSettings"] = new JsonObject
            {
                ["path"] = p.Extra.GetValueOrDefault("path", "/"),
                ["headers"] = new JsonObject { ["Host"] = p.Extra.GetValueOrDefault("host", p.Address) }
            };
        }

        return stream;
    }

    private static JsonObject BuildVMessOutbound(ServerProfile p) => new()
    {
        ["protocol"] = "vmess",
        ["settings"] = new JsonObject
        {
            ["vnext"] = new JsonArray
            {
                new JsonObject
                {
                    ["address"] = p.Address,
                    ["port"] = p.Port,
                    ["users"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = p.Extra.GetValueOrDefault("uuid", ""),
                            ["alterId"] = int.Parse(p.Extra.GetValueOrDefault("alterId", "0")),
                            ["security"] = "auto"
                        }
                    }
                }
            }
        },
        ["streamSettings"] = StreamSettings(p)
    };

    private static JsonObject BuildVLessOutbound(ServerProfile p) => new()
    {
        ["protocol"] = "vless",
        ["settings"] = new JsonObject
        {
            ["vnext"] = new JsonArray
            {
                new JsonObject
                {
                    ["address"] = p.Address,
                    ["port"] = p.Port,
                    ["users"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["id"] = p.Extra.GetValueOrDefault("uuid", ""),
                            ["encryption"] = "none",
                            ["flow"] = p.Extra.GetValueOrDefault("flow", "")
                        }
                    }
                }
            }
        },
        ["streamSettings"] = StreamSettings(p)
    };

    private static JsonObject BuildTrojanOutbound(ServerProfile p) => new()
    {
        ["protocol"] = "trojan",
        ["settings"] = new JsonObject
        {
            ["servers"] = new JsonArray
            {
                new JsonObject
                {
                    ["address"] = p.Address,
                    ["port"] = p.Port,
                    ["password"] = p.Extra.GetValueOrDefault("password", "")
                }
            }
        },
        ["streamSettings"] = new JsonObject
        {
            ["network"] = p.Extra.GetValueOrDefault("network", "tcp"),
            ["security"] = "tls",
            ["tlsSettings"] = new JsonObject
            {
                ["serverName"] = p.Extra.GetValueOrDefault("sni", p.Address)
            }
        }
    };

    private static JsonObject BuildShadowsocksOutbound(ServerProfile p) => new()
    {
        ["protocol"] = "shadowsocks",
        ["settings"] = new JsonObject
        {
            ["servers"] = new JsonArray
            {
                new JsonObject
                {
                    ["address"] = p.Address,
                    ["port"] = p.Port,
                    ["method"] = p.Extra.GetValueOrDefault("method", "aes-256-gcm"),
                    ["password"] = p.Extra.GetValueOrDefault("password", "")
                }
            }
        }
    };
}
