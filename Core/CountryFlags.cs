namespace LocoVPN.Core;

/// <summary>
/// cuiComboBox only displays plain strings (no per-item icons), so "showing the country
/// icon before the name" is done by prefixing a Unicode flag emoji onto the display text
/// itself, e.g. "🇩🇪 Germany - 42 ms". This scans the server's Name for a known country
/// name (matching how the subscription configs name their servers, e.g. literally
/// containing "Germany") and returns the corresponding flag, or a globe as a fallback.
/// </summary>
public static class CountryFlags
{
    private static readonly Dictionary<string, string> Flags = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Germany"] = "🇩🇪",
        ["United States"] = "🇺🇸",
        ["USA"] = "🇺🇸",
        ["United Kingdom"] = "🇬🇧",
        ["UK"] = "🇬🇧",
        ["Canada"] = "🇨🇦",
        ["Netherlands"] = "🇳🇱",
        ["France"] = "🇫🇷",
        ["Japan"] = "🇯🇵",
        ["Singapore"] = "🇸🇬",
        ["South Korea"] = "🇰🇷",
        ["Korea"] = "🇰🇷",
        ["Australia"] = "🇦🇺",
        ["India"] = "🇮🇳",
        ["Brazil"] = "🇧🇷",
        ["Russia"] = "🇷🇺",
        ["Turkey"] = "🇹🇷",
        ["Poland"] = "🇵🇱",
        ["Sweden"] = "🇸🇪",
        ["Switzerland"] = "🇨🇭",
        ["Finland"] = "🇫🇮",
        ["Italy"] = "🇮🇹",
        ["Spain"] = "🇪🇸",
        ["Ireland"] = "🇮🇪",
        ["Hong Kong"] = "🇭🇰",
        ["Taiwan"] = "🇹🇼",
        ["UAE"] = "🇦🇪",
        ["United Arab Emirates"] = "🇦🇪",
        ["Ukraine"] = "🇺🇦",
        ["Austria"] = "🇦🇹",
        ["Belgium"] = "🇧🇪",
        ["Denmark"] = "🇩🇰",
        ["Norway"] = "🇳🇴",
        ["Vietnam"] = "🇻🇳",
        ["Indonesia"] = "🇮🇩",
        ["Malaysia"] = "🇲🇾",
        ["Mexico"] = "🇲🇽",
        ["Argentina"] = "🇦🇷",
        ["Iran"] = "🇮🇷",
        ["Israel"] = "🇮🇱",
        ["South Africa"] = "🇿🇦",
        ["Romania"] = "🇷🇴",
        ["Czechia"] = "🇨🇿",
        ["Czech Republic"] = "🇨🇿",
        ["Hungary"] = "🇭🇺",
        ["Greece"] = "🇬🇷",
        ["Portugal"] = "🇵🇹",
        ["Bulgaria"] = "🇧🇬",
        ["Lithuania"] = "🇱🇹",
        ["Latvia"] = "🇱🇻",
        ["Estonia"] = "🇪🇪",
        ["Iceland"] = "🇮🇸",
        ["Luxembourg"] = "🇱🇺",
        ["Croatia"] = "🇭🇷",
        ["Serbia"] = "🇷🇸",
        ["China"] = "🇨🇳",
        ["New Zealand"] = "🇳🇿",
        ["Philippines"] = "🇵🇭",
        ["Thailand"] = "🇹🇭",
        ["Egypt"] = "🇪🇬",
        ["Saudi Arabia"] = "🇸🇦",
        ["Qatar"] = "🇶🇦",
        ["Kazakhstan"] = "🇰🇿",
        ["Moldova"] = "🇲🇩",
        ["Cyprus"] = "🇨🇾",
        ["Bosnia"] = "🇧🇦",
        ["Slovenia"] = "🇸🇮",
        ["Slovakia"] = "🇸🇰",
    };

    private const string Fallback = "🌐";

    /// <summary>Returns the flag for the first known country name found inside <paramref name="serverName"/>,
    /// or a globe emoji if none match.</summary>
    public static string FindFlag(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return Fallback;

        foreach (var (country, flag) in Flags)
        {
            if (serverName.Contains(country, StringComparison.OrdinalIgnoreCase))
                return flag;
        }

        return Fallback;
    }

    /// <summary>Returns the matched country name itself (for grouping/sorting), or null if none matched.</summary>
    public static string? FindCountryName(string serverName)
    {
        if (string.IsNullOrWhiteSpace(serverName))
            return null;

        foreach (var country in Flags.Keys)
        {
            if (serverName.Contains(country, StringComparison.OrdinalIgnoreCase))
                return country;
        }

        return null;
    }
}
