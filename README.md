<div align="center">

# LocoVPN

**Language:** English &nbsp;|&nbsp; [فارسی](README-farsi.md)

</div>

A Windows Forms GUI wrapper around [Xray-core](https://github.com/XTLS/Xray-core), built with
**C#/.NET 8** and **[CuoreUI.Winforms](https://www.nuget.org/packages/CuoreUI.Winforms)**.
LocoVPN fetches subscription configs from GitHub, parses `vmess://` / `vless://` / `trojan://` /
`ss://` links (plus basic Clash YAML), TCP-pings and then real-tunnel URL-tests every server,
keeps only the ones that actually work, and lets you connect/disconnect with one big circular
button.

## Features

- Fetches and parses multiple subscription sources at once.
- Two-stage server testing: a fast TCP pre-filter, then a real tunnel test (an isolated
  `xray.exe` instance actually routes a request through each server) — only servers that pass
  both stay in the list.
- Country flag emoji + name + live latency shown per server in the selection combo box.
- One circular button toggles Connect/Disconnect and recolors itself by state.
- Sets the Windows system proxy (HTTP **and** SOCKS) on connect, and restores your original
  settings on disconnect — including automatic recovery if the app was killed mid-connection.
- Looks for your own `xray.exe` before ever downloading one.

## Project Structure

```
LocoVPN/
├── LocoVPN.csproj
├── Program.cs                    # Entry point: LoadingForm -> MainForm
├── LoadingForm.cs / .Designer.cs # Startup splash: fetch + TCP test + URL test + filter
├── MainForm.cs / .Designer.cs    # Exit button, server combo box, connect/disconnect circle
├── Core/
│   ├── AppSettings.cs            # Paths, ports, constants, subscription URL list
│   ├── ServerProfile.cs          # Parsed-server data model (TCP + URL latency)
│   ├── CountryFlags.cs           # Country name -> flag emoji lookup for the combo box
│   ├── ConfigFetcher.cs          # Downloads + base64-decodes subscription bodies
│   ├── ConfigParser.cs           # URI/YAML parsing -> Xray outbound JSON
│   ├── XrayManager.cs            # Resolve bundled/cached/downloaded xray.exe, run/kill process
│   ├── LatencyTester.cs          # TcpPingTester (fast) + UrlTester (real tunnel test)
│   ├── SystemProxyManager.cs     # Save/restore Windows proxy via registry (HTTP + SOCKS)
│   └── AppController.cs          # All app logic, independent of any specific Form
├── Properties/
│   ├── Resources.resx            # Embedded resources (currently just the logo)
│   └── Resources.Designer.cs     # Auto-generated accessor for the above
├── Resources/
│   └── logo2.png                 # Embedded as Properties.Resources.logo2
├── LICENSE.txt                   # Apache-2.0
└── README.md / README-farsi.md
```

Two optional, not-yet-present folders the code already looks for:

- `Assets\background.png` / `Assets\logo.png` — if you add these, `MainForm.LoadAssets()` picks
  `logo.png` up automatically (overriding the embedded `logo2` resource) at runtime.
- `xray.exe` in the project root — see "Using Your Own xray.exe" below.

## Setup

1. **.NET 8 SDK** + Visual Studio 2022 (17.8+) with the ".NET desktop development" workload, or
   just the `dotnet` CLI.
2. `dotnet restore` — pulls in `YamlDotNet`, `Microsoft.Win32.Registry`, and `CuoreUI.Winforms`.
3. Optional: drop `background.png`/`logo.png` into an `Assets\` folder you create, and/or your
   own `xray.exe` into the project root.
4. `dotnet build -c Release`, then run `LocoVPN.exe` (or `dotnet run`).

## Startup Flow: LoadingForm → MainForm

`Program.cs` shows **`LoadingForm`** first (borderless, centered). On load it runs
`AppController.FetchAndFilterAsync()`, which does, in order:

1. Fetches every subscription URL and parses the results into `ServerProfile`s.
2. **TCP pre-filter** (fast) — drops anything whose host:port doesn't even accept a connection.
3. **Real URL test** — for each TCP survivor, spins up an isolated `xray.exe` on an ephemeral
   port and routes an actual HTTP request through the tunnel to `generate_204`, timing it.
4. Keeps only servers that passed **both** tests.

The loading screen's `cuiCircleProgressBar` is driven by `AppController.ProgressChanged`
(0–100%, staged roughly 0–5% fetch, 5–50% TCP filter, 50–55% installing xray-core, 55–100% URL
test), and the status label shows the live log line. Clicking the × button cancels the in-flight
work and exits without opening `MainForm`. Once filtering finishes, `LoadingForm` closes and
`Program.cs` opens `MainForm` with the same `AppController` instance — the already-fetched
server list carries over, so `MainForm` doesn't redo any of that work on open, only on an
explicit "Refresh Server List" click.

## MainForm

- **× exit button**, top right.
- **`cuiComboBox`** listing every surviving server as `"🇩🇪  Germany   —   42 ms"` — a flag
  emoji, the country name detected inside the config's own name (via
  `CountryFlags.FindCountryName`), and its URL-test latency (falling back to the TCP latency if
  the URL test hasn't run for that entry). The flag is baked into the text itself; there's no
  separate flag icon control in the current build (see "Known Gaps" below).
- **Big circular `cuiButton`** that toggles Connect ⇄ Disconnect and recolors itself based on
  state: orange/red when disconnected, green when connected, gray while (dis)connecting
  (`SetConnectButtonColors` in `MainForm.cs` — the four `Color` constants at the top of the file
  control this).
- **Status strip** (`pnlStatusBar`) showing a colored connection dot and the connected server's
  flag + name.
- A `TextBox` log panel (`txtLog`) for diagnostics.
- Unlike `LoadingForm`, `MainForm` currently uses the default window chrome (title bar/border) —
  `FormBorderStyle` isn't overridden here.

### Everything routes through `AppController`

All real logic (fetch/parse/test/connect/disconnect/crash-recovery) lives in
`Core/AppController.cs`, independent of any specific Form. Both `LoadingForm` and `MainForm` are
thin consumers of it, so you can freely rearrange or restyle either `.Designer.cs` in the Visual
Studio designer as long as your controls' event handlers call the same members:

```
_controller.RecoverFromCrash();                 // once, before showing any UI
await _controller.FetchAndFilterAsync(ct);       // fetch + TCP filter + URL test + keep-good
await _controller.RetestAsync();                 // re-test what's already in Servers, no filtering
await _controller.ConnectAsync(selectedServer);
_controller.Disconnect();
_controller.ShutdownIfConnected();               // call from FormClosing
_controller.Servers            // BindingList<ServerProfile> - bind to any list/combo/grid
_controller.Log                // event Action<string>
_controller.StateChanged       // event Action
_controller.ProgressChanged    // event Action<int>  (0-100, useful for a loading screen)
```

## Using Your Own `xray.exe`

`XrayManager.EnsureXrayInstalledAsync()` checks, **in this order, before ever touching the
network**:

1. `xray.exe` next to the built `.exe` (drop it in the **project root**, next to
   `LocoVPN.csproj` — it's wired into the `.csproj` as a `CopyToOutputDirectory` item).
2. `core\xray.exe` or `Assets\xray.exe` relative to the built `.exe`
   (`AppSettings.BundledXrayCandidatePaths`).
3. A previously-downloaded copy cached at `%AppData%\LocoVPN\core\xray.exe`.
4. Only if none of the above exist: downloads the latest Windows-64 release from
   `XTLS/Xray-core` on GitHub, verifying its SHA-256 against the published `.dgst` file when
   available.

The log panel reports which of these it used. Both the live connection (`XrayManager`) and the
per-server URL test (`UrlTester`) share this resolution logic via
`XrayManager.ResolveExistingXrayPath()`.

## SOCKS Proxy Support

The Windows system proxy is set via the WinINet `ProxyServer` registry format that
**Internet Options → LAN Settings → Advanced** writes, which supports per-protocol proxies:

```
http=127.0.0.1:10809;https=127.0.0.1:10809;socks=127.0.0.1:10808
```

So SOCKS-aware applications get routed through Xray's SOCKS inbound, not just plain HTTP/HTTPS
traffic. This is still WinINet-level proxying, not a full system-wide TUN/packet-capture
solution — apps with their own network stack can still bypass it. A genuinely all-traffic
solution would mean adding a TUN adapter and routing through Xray's `tun` inbound instead, which
is a bigger change and out of scope here.

## Testing Checklist

1. Launch the app — `LoadingForm` should show live status text and the progress bar advancing
   through fetch → TCP filter → URL test.
2. `MainForm` opens with the combo box pre-populated; pick a server, hit the circular button —
   it turns gray ("Connecting…"), then green ("Disconnect") on success, with the status strip
   showing the connected flag + name.
3. Check Windows Settings → Network & Internet → Proxy (or `reg query
   "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" /v ProxyServer`) for the
   `http=...;https=...;socks=...` string.
4. Click the circular button again to disconnect — colors revert, and the registry value should
   revert to whatever it was before.
5. **Crash-recovery test**: while connected, kill the process from Task Manager instead of using
   the button. Relaunch — `RecoverFromCrash()` (called at the top of `LoadingForm_Load`) should
   restore your original proxy settings before the fetch even starts.

## Error Handling Notes

| Scenario | Handling |
|---|---|
| Subscription fetch fails | Logged per-URL; other URLs still attempted. |
| A config line fails to parse | Skipped silently, doesn't abort the batch. |
| xray.exe download fails / checksum mismatch | Surfaces as an exception; `LoadingForm` shows a message box then still opens `MainForm` rather than stranding the user on the splash screen. |
| xray.exe exits immediately after Connect | Detected via a short delay + `IsRunning` check before the proxy is applied. |
| Proxy restore fails | Exception surfaces to a message box asking the user to check Windows proxy settings manually. |
| App killed without clean disconnect | `connected.flag` under `%AppData%\LocoVPN\` triggers auto-restore on next launch, before any UI is shown. |

## Security Notes

- No API keys are used (GitHub raw/releases URLs don't need auth at this volume).
- `.gitignore` excludes `bin/`, `obj/`, any downloaded/bundled `xray.exe`, and the generated
  `config.json` (which embeds server UUIDs/passwords in plaintext).
- Registry writes are scoped to `HKEY_CURRENT_USER`, so the app doesn't need admin rights and
  only affects the current Windows user's proxy settings.
- SHA-256 verification of any *downloaded* `xray.exe` is enforced when GitHub publishes a
  `.dgst` file. A bundled xray.exe you supply yourself is trusted as-is and not re-hashed.

## Known Gaps

- **Per-country flag images**: the combo box currently shows a Unicode flag *emoji* baked into
  the text (`CountryFlags.cs`), not a real flag icon in a dedicated `PictureBox`. If you've built
  a `Flags\` folder of PNGs and a picture box for this, it isn't in this snapshot of the repo —
  worth double-checking it was actually pushed/merged before relying on this README to describe
  it.
- `MainForm` currently has standard window chrome (title bar + border), while `LoadingForm` is
  borderless — this is likely worth making consistent depending on which look you're going for.
- `cuiCircleProgressBar.Value` in `LoadingForm.cs` is the one property name that was an
  educated guess at the standard WinForms progress-control convention rather than a confirmed
  API — check it in the Properties panel/IntelliSense if it's not driving the bar correctly.

## License

Apache-2.0 — see `LICENSE.txt`.
