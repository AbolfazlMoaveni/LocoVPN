# LocoVPN

A Windows Forms GUI wrapper around [Xray-core](https://github.com/XTLS/Xray-core): fetches
subscription configs, parses `vmess://` / `vless://` / `trojan://` / `ss://` links (and basic
Clash YAML), TCP-pings and then real-tunnel URL-tests every server, keeps only the ones that
actually work, and lets you connect/disconnect with one big circular button.

## Project Structure

```
LocoVPN/
├── LocoVPN.csproj
├── Program.cs                  # Entry point: LoadingForm -> MainForm
├── LoadingForm.cs / .Designer.cs   # Startup splash: fetch + TCP test + URL test + filter
├── MainForm.cs / .Designer.cs      # Exit button, server combo box, connect/disconnect circle
├── Core/
│   ├── AppSettings.cs           # Paths, ports, constants, subscription URL list
│   ├── ServerProfile.cs         # Parsed-server data model (TCP + URL latency)
│   ├── CountryFlags.cs          # Country name -> flag emoji lookup for the combo box
│   ├── ConfigFetcher.cs         # Downloads + base64-decodes subscription bodies
│   ├── ConfigParser.cs          # URI/YAML parsing -> Xray outbound JSON
│   ├── XrayManager.cs           # Resolve bundled/cached/downloaded xray.exe, run/kill process
│   ├── LatencyTester.cs         # TcpPingTester (fast) + UrlTester (real tunnel test)
│   ├── SystemProxyManager.cs    # Save/restore Windows proxy via registry (HTTP + SOCKS)
│   └── AppController.cs         # All app logic, independent of any specific Form
├── Assets/
│   ├── background.png           # optional
│   └── logo.png                 # optional
├── xray.exe                     # <- OPTIONAL: drop your own copy here, see below
├── .gitignore
└── README.md
```

## Setup

1. .NET 8 SDK + Visual Studio 2022 (17.8+) with the ".NET desktop development" workload, or
   just the `dotnet` CLI.
2. `dotnet restore` — pulls in `YamlDotNet`, `Microsoft.Win32.Registry`, and `CuoreUI.Winforms`.
3. Optional: drop `background.png`/`logo.png` into `Assets\`, and/or your own `xray.exe` into
   the project root (see "Using your own xray.exe" below).
4. `dotnet build -c Release` then run `LocoVPN.exe`, or `dotnet run`.

## Startup Flow: LoadingForm → MainForm

`Program.cs` shows **`LoadingForm`** first. On load it runs
`AppController.FetchAndFilterAsync()`, which does, in order:

1. Fetch all subscription URLs, parse into `ServerProfile`s.
2. **TCP pre-filter** (fast) — drops anything whose host:port doesn't even accept a connection.
3. **Real URL test** — for each TCP survivor, spins up an isolated `xray.exe` on an ephemeral
   port and routes an actual HTTP request through the tunnel to `generate_204`, timing it.
4. Keeps only servers that passed **both** tests.

The loading screen's `cuiCircleProgressBar` is driven by `AppController.ProgressChanged` (0-100,
staged roughly 0-5% fetch, 5-50% TCP filter, 50-55% installing xray-core, 55-100% URL test), and
the status label shows the live log line. Closing the loading screen's × button cancels the
in-flight work and exits without opening `MainForm`. Once filtering finishes, `LoadingForm`
closes and `Program.cs` opens `MainForm` with the same `AppController` instance (so the
already-fetched server list carries over — `MainForm` doesn't redo any of this work on open,
only on an explicit "Refresh Server List" click).

## MainForm

- **× exit button**, top right.
- **`cuiComboBox`** listing every surviving server as `"🇩🇪  Germany   —   42 ms"` — flag, the
  country name detected in the config's own name (via `CountryFlags.FindCountryName`), and its
  URL-test latency. `SortAlphabetically` is turned off specifically so the combo box's item
  order stays aligned with `MainForm`'s internal `_orderedServers` list (index-based lookup).
- **Big circular `cuiButton`** that toggles Connect ⇄ Disconnect and recolors itself based on
  state: orange/red when disconnected, green when connected, gray while (dis)connecting
  (`SetConnectButtonColors` in `MainForm.cs` — tweak the four `Color` constants at the top of
  the file to restyle).
- **Status strip** (`pnlStatusBar`) showing a colored connection dot and the connected server's
  flag+name, mirroring the reference design's bottom bar.
- A plain `TextBox` log panel (`txtLog`) for diagnostics — delete it in the designer if you don't
  want it visible; nothing else depends on it.

### Since you'll be redesigning the UI yourself: everything routes through `AppController`

All real logic (fetch/parse/test/connect/disconnect/crash-recovery) lives in
`Core/AppController.cs`, which has zero dependency on any specific Form or control. Both
`LoadingForm` and `MainForm` are thin consumers of it — you can freely rearrange, restyle, or
completely replace either `.Designer.cs` in the Visual Studio designer (both are ordinary
hand-written `InitializeComponent()` methods, so they open fine for drag-and-drop editing) as
long as your new controls' event handlers call the same `AppController` members:

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

## Using CuoreUI.Winforms

The UI is built with **CuoreUI.Winforms** (`cuiButton`, `cuiComboBox`, `cuiLabel`, `cuiPanel`,
`cuiCircleProgressBar`) for the rounded/circular look. A couple of notes:

- **Circular buttons**: set a square `Size` and `Rounding` to half that size (e.g. `100×100` with
  `Rounding = new Padding(50)`) — that's how `btnConnectToggle` and the × close buttons are made
  circular. `OutlineThickness = 0` removes the border ring if you don't want it.
- **`cuiComboBox` only holds plain strings** (`Items` is `string[]`, no per-item icons or bound
  objects), which is why the flag is baked into the display text itself rather than drawn as a
  separate icon, and why `MainForm` keeps a parallel `_orderedServers` list indexed the same way
  as `comboServers.Items`.
- **One property name I couldn't verify against the exact installed package version**:
  `cuiCircleProgressBar.Value` (assumed 0-100, the standard WinForms progress-control
  convention) in `LoadingForm.OnProgress()`. If your installed CuoreUI version names this
  differently, IntelliSense/the Properties panel will show the right name immediately — it's a
  one-line fix isolated to that method.
- If you'd rather not use CuoreUI at all, nothing else in the project depends on it — swap the
  two `.Designer.cs` files for standard WinForms controls and wire them to the same
  `AppController` calls listed above.

## Using Your Own `xray.exe`

`XrayManager.EnsureXrayInstalledAsync()` now checks, **in this order, before ever touching the
network**:

1. `xray.exe` next to the built `.exe` (i.e. drop it in the **project root**, next to
   `LocoVPN.csproj` — it's wired into the `.csproj` as a `CopyToOutputDirectory` item, so it
   lands next to `LocoVPN.exe` on build).
2. `core\xray.exe` or `Assets\xray.exe` relative to the built `.exe`, if you'd rather organize it
   that way (`AppSettings.BundledXrayCandidatePaths`).
3. A previously-downloaded copy cached at `%AppData%\LocoVPN\core\xray.exe`.
4. Only if none of the above exist: downloads the latest Windows-64 release from
   `XTLS/Xray-core` on GitHub, verifying its SHA-256 against the published `.dgst` file when
   available.

The log panel tells you which of these it used ("Using bundled xray.exe found at ... - skipping
download." vs. the download path). Both the live connection (`XrayManager`) and the per-server
URL test (`UrlTester`) share this same resolution logic via `XrayManager.ResolveExistingXrayPath()`.

## SOCKS Proxy Support

The Windows system proxy is set via the same WinINet `ProxyServer` registry format that
**Internet Options → LAN Settings → Advanced** writes, which supports **per-protocol** proxies:

```
http=127.0.0.1:10809;https=127.0.0.1:10809;socks=127.0.0.1:10808
```

This means SOCKS-aware applications (most browsers, many dev tools) actually get routed through
Xray's SOCKS inbound now, not just plain HTTP/HTTPS traffic through the HTTP inbound like before.
This is still WinINet-level proxying, though — not a full system-wide TUN/packet-capture
solution. Apps with their own network stack (some UWP apps, some games, some VPN-detecting
software) can still bypass it entirely. A genuinely all-traffic solution would mean adding a TUN
adapter (e.g. via `wintun.dll`) and routing through Xray's `tun` inbound instead of a registry
proxy toggle — a much bigger change, and out of scope here, but flagging it in case "SOCKS" was
really shorthand for "everything, no exceptions."

## Testing Checklist

1. Launch the app — `LoadingForm` should show live status text and the circular progress bar
   advancing through fetch → TCP filter → URL test.
2. `MainForm` opens with the combo box pre-populated; pick a server, hit the circular button —
   it should turn gray ("Connecting…"), then green ("Disconnect") on success, with the status
   strip showing the connected flag+name.
3. Check Windows Settings → Network & Internet → Proxy (or `reg query
   "HKCU\Software\Microsoft\Windows\CurrentVersion\Internet Settings" /v ProxyServer`) — you
   should see the `http=...;https=...;socks=...` string.
4. Click the circular button again to disconnect — it should turn back to orange/red, and the
   registry value should revert to whatever it was before.
5. **Crash-recovery test**: while connected, kill the process from Task Manager instead of using
   the button. Relaunch — `RecoverFromCrash()` (called at the top of `LoadingForm_Load`) should
   restore your original proxy settings before the fetch even starts; check the log panel.

## Error Handling Notes

| Scenario | Handling |
|---|---|
| Subscription fetch fails | Logged per-URL; other URLs still attempted. |
| A config line fails to parse | Skipped silently, doesn't abort the batch. |
| xray.exe download fails / checksum mismatch | Surfaces as an exception; `LoadingForm` shows a message box then still opens `MainForm` (possibly with an empty list) rather than stranding the user on the splash screen. |
| xray.exe exits immediately after Connect | Detected via a short delay + `IsRunning` check before the proxy is applied. |
| Proxy restore fails | Exception surfaces to a message box asking the user to check Windows proxy settings manually. |
| App killed without clean disconnect | `connected.flag` under `%AppData%\LocoVPN\` triggers auto-restore on next launch, before any UI is shown. |

## Security Notes

- No API keys are used (GitHub raw/releases URLs don't need auth at this volume).
- `.gitignore` excludes `bin/`, `obj/`, any downloaded/bundled `xray.exe`, and the generated
  `config.json` (which embeds server UUIDs/passwords in plaintext) — don't remove those entries.
- Registry writes are scoped to `HKEY_CURRENT_USER`, so the app doesn't need admin rights and
  only affects the current Windows user's proxy settings.
- SHA-256 verification of any *downloaded* `xray.exe` is enforced when GitHub publishes a
  `.dgst` file. A bundled xray.exe you supply yourself is trusted as-is (you already vetted it),
  and is not re-hashed against anything.
