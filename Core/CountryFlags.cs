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
        //["Germany"] = "🇩🇪",
        //["United States"] = "🇺🇸",
        //["USA"] = "🇺🇸",
        //["United Kingdom"] = "🇬🇧",
        //["UK"] = "🇬🇧",
        //["Canada"] = "🇨🇦",
        //["Netherlands"] = "🇳🇱",
        //["France"] = "🇫🇷",
        //["Japan"] = "🇯🇵",
        //["Singapore"] = "🇸🇬",
        //["South Korea"] = "🇰🇷",
        //["Korea"] = "🇰🇷",
        //["Australia"] = "🇦🇺",
        //["India"] = "🇮🇳",
        //["Brazil"] = "🇧🇷",
        //["Russia"] = "🇷🇺",
        //["Turkey"] = "🇹🇷",
        //["Poland"] = "🇵🇱",
        //["Sweden"] = "🇸🇪",
        //["Switzerland"] = "🇨🇭",
        //["Finland"] = "🇫🇮",
        //["Italy"] = "🇮🇹",
        //["Spain"] = "🇪🇸",
        //["Ireland"] = "🇮🇪",
        //["Hong Kong"] = "🇭🇰",
        //["Taiwan"] = "🇹🇼",
        //["UAE"] = "🇦🇪",
        //["United Arab Emirates"] = "🇦🇪",
        //["Ukraine"] = "🇺🇦",
        //["Austria"] = "🇦🇹",
        //["Belgium"] = "🇧🇪",
        //["Denmark"] = "🇩🇰",
        //["Norway"] = "🇳🇴",
        //["Vietnam"] = "🇻🇳",
        //["Indonesia"] = "🇮🇩",
        //["Malaysia"] = "🇲🇾",
        //["Mexico"] = "🇲🇽",
        //["Argentina"] = "🇦🇷",
        //["Iran"] = "🇮🇷",
        //["Israel"] = "🇮🇱",
        //["South Africa"] = "🇿🇦",
        //["Romania"] = "🇷🇴",
        //["Czechia"] = "🇨🇿",
        //["Czech Republic"] = "🇨🇿",
        //["Hungary"] = "🇭🇺",
        //["Greece"] = "🇬🇷",
        //["Portugal"] = "🇵🇹",
        //["Bulgaria"] = "🇧🇬",
        //["Lithuania"] = "🇱🇹",
        //["Latvia"] = "🇱🇻",
        //["Estonia"] = "🇪🇪",
        //["Iceland"] = "🇮🇸",
        //["Luxembourg"] = "🇱🇺",
        //["Croatia"] = "🇭🇷",
        //["Serbia"] = "🇷🇸",
        //["China"] = "🇨🇳",
        //["New Zealand"] = "🇳🇿",
        //["Philippines"] = "🇵🇭",
        //["Thailand"] = "🇹🇭",
        //["Egypt"] = "🇪🇬",
        //["Saudi Arabia"] = "🇸🇦",
        //["Qatar"] = "🇶🇦",
        //["Kazakhstan"] = "🇰🇿",
        //["Moldova"] = "🇲🇩",
        //["Cyprus"] = "🇨🇾",
        //["Bosnia"] = "🇧🇦",
        //["Slovenia"] = "🇸🇮",
        //["Slovakia"] = "🇸🇰",

        ["Germany"] = "DE",
        ["United States"] = "US",
        ["USA"] = "US",
        ["United Kingdom"] = "GB",
        ["UK"] = "GB",
        ["Canada"] = "CA",
        ["Netherlands"] = "NL",
        ["France"] = "FR",
        ["Japan"] = "JP",
        ["Singapore"] = "SG",
        ["South Korea"] = "KR",
        ["Korea"] = "KR",
        ["Australia"] = "AU",
        ["India"] = "IN",
        ["Brazil"] = "BR",
        ["Russia"] = "RU",
        ["Turkey"] = "TR",
        ["Poland"] = "PL",
        ["Sweden"] = "SE",
        ["Switzerland"] = "CH",
        ["Finland"] = "FI",
        ["Italy"] = "IT",
        ["Spain"] = "ES",
        ["Ireland"] = "IE",
        ["Hong Kong"] = "HK",
        ["Taiwan"] = "TW",
        ["UAE"] = "AE",
        ["United Arab Emirates"] = "AE",
        ["Ukraine"] = "UA",
        ["Austria"] = "AT",
        ["Belgium"] = "BE",
        ["Denmark"] = "DK",
        ["Norway"] = "NO",
        ["Vietnam"] = "VN",
        ["Indonesia"] = "ID",
        ["Malaysia"] = "MY",
        ["Mexico"] = "MX",
        ["Argentina"] = "AR",
        ["Iran"] = "IR",
        ["Israel"] = "IL",
        ["South Africa"] = "ZA",
        ["Romania"] = "RO",
        ["Czechia"] = "CZ",
        ["Czech Republic"] = "CZ",
        ["Hungary"] = "HU",
        ["Greece"] = "GR",
        ["Portugal"] = "PT",
        ["Bulgaria"] = "BG",
        ["Lithuania"] = "LT",
        ["Latvia"] = "LV",
        ["Estonia"] = "EE",
        ["Iceland"] = "IS",
        ["Luxembourg"] = "LU",
        ["Croatia"] = "HR",
        ["Serbia"] = "RS",
        ["China"] = "CN",
        ["New Zealand"] = "NZ",
        ["Philippines"] = "PH",
        ["Thailand"] = "TH",
        ["Egypt"] = "EG",
        ["Saudi Arabia"] = "SA",
        ["Qatar"] = "QA",
        ["Kazakhstan"] = "KZ",
        ["Moldova"] = "MD",
        ["Cyprus"] = "CY",
        ["Bosnia"] = "BA",
        ["Slovenia"] = "SI",
        ["Slovakia"] = "SK",
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
