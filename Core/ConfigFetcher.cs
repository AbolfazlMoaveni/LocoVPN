using System.Net.Http;
using System.Text;

namespace LocoVPN.Core;

/// <summary>
/// Reimplements the fetch half of crawl.py natively: pulls each subscription URL,
/// and if the body doesn't look like plain share-links, tries base64-decoding it
/// (subscription services commonly base64-encode the whole list).
/// </summary>
public class ConfigFetcher
{
    private readonly HttpClient _http;

    public ConfigFetcher(HttpClient? httpClient = null)
    {
        _http = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
    }

    /// <summary>
    /// Fetches all configured subscription URLs and returns a de-duplicated list
    /// of raw share-link strings (vmess://, vless://, trojan://, ss://).
    /// </summary>
    public async Task<List<string>> FetchAllAsync(
        IEnumerable<string>? urls = null,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        urls ??= AppSettings.SubscriptionUrls;
        var all = new HashSet<string>();


        foreach (var url in urls)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var text = await _http.GetStringAsync(url, ct);
                var lines = DecodeSubscriptionBody(text);
                foreach (var line in lines)
                    all.Add(line);

                //progress?.Report($"[+] Fetched {lines.Count} configs from {url}");
                progress?.Report($"[+] Fetched {lines.Count} configs...");
            }
            catch (Exception ex)
            {
                progress?.Report($"[-] Failed to fetch from {url}: {ex.Message}");
            }
        }

        return all.ToList();
    }

    /// <summary>
    /// Mirrors crawl.py's heuristic: if the first chunk of text doesn't contain "://",
    /// assume the whole body is base64 and try to decode it before splitting into lines.
    /// </summary>
    private static List<string> DecodeSubscriptionBody(string text)
    {
        text = text.Trim();
        var probe = text.Length > 50 ? text[..50] : text;

        if (!probe.Contains("://"))
        {
            try
            {
                var padded = PadBase64(text);
                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                text = decoded;
            }
            catch
            {
                // Not valid base64 - fall through and treat as plain text, same as crawl.py.
            }
        }

        return text
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim().TrimEnd('\r'))
            .Where(l => l.Length > 0)
            .ToList();
    }

    private static string PadBase64(string s)
    {
        var remainder = s.Length % 4;
        return remainder == 0 ? s : s + new string('=', 4 - remainder);
    }
}
