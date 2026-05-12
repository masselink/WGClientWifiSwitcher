# MasselGUARD — How it works

This document explains the internal operation of MasselGUARD in detail. For a feature overview and quick-start, see the [README](../README.md).

---

## Table of contents

1. [Operating modes](#1-operating-modes)
2. [Startup sequence](#2-startup-sequence)
3. [WiFi monitoring](#3-wifi-monitoring)
4. [LAN / Ethernet monitoring](#4-lan--ethernet-monitoring)
5. [Rule evaluation](#5-rule-evaluation)
6. [Connecting a tunnel — Standalone](#6-connecting-a-tunnel--standalone)
7. [Connecting a tunnel — Companion](#7-connecting-a-tunnel--companion)
8. [Disconnecting a tunnel](#8-disconnecting-a-tunnel)
9. [Pre/post scripts](#9-prepost-scripts)
10. [Tunnel groups and categories](#10-tunnel-groups-and-categories)
11. [Quick Connect](#11-quick-connect)
12. [Open network protection](#12-open-network-protection)
13. [Configuration and storage](#13-configuration-and-storage)
14. [Security model](#14-security-model)
15. [Theme system](#15-theme-system)
16. [Logging](#16-logging)
17. [Settings panel](#17-settings-panel)
18. [Build and deployment](#18-build-and-deployment)
19. [Troubleshooting](#19-troubleshooting)

---

## 1. Operating modes

MasselGUARD runs in one of three modes, selected in the setup wizard or Settings → General.

### Standalone

MasselGUARD owns the tunnel lifecycle entirely. No WireGuard application is required. Tunnel configs are created, encrypted, and stored inside the app. Connectivity is provided by `tunnel.dll` + `wireguard.dll` (wireguard-NT) placed next to the executable.

### Companion

MasselGUARD automates the official WireGuard for Windows application. It does not store or modify any tunnel configs — it only starts and stops the `WireGuardTunnel$<n>` Windows services that WireGuard creates. You link existing WireGuard profiles from the Import dialog.

### Mixed

Both modes active simultaneously. Local (Standalone) tunnels and linked WireGuard profiles coexist in the same tunnel list and can all be automated.

---

## 2. Startup sequence

```
Program.Main()
  └─ Mutex check (single instance — second launch shows informational dialog)
  └─ UAC elevation check (exits with prompt if not Administrator)
  └─ Application.Run(MainWindow)
        │
        ▼
MainWindow.Loaded
  ├─ Log(AppStarted)
  ├─ LoadConfig()              read %APPDATA%\MasselGUARD\config.json
  ├─ ApplyManualMode()         hide Rules/Default tabs in Settings if manual
  ├─ ApplyLocalTunnelMode()    show/hide Add/Edit based on mode
  ├─ SetupTimer()              1-second status poll
  ├─ _startupComplete = true   suppress discovery log on future refreshes
  ├─ RegisterWifiEvents()      WlanRegisterNotification
  ├─ RegisterLanEvents()       NetworkChange subscription
  ├─ UpdateThemeToggleIcon()
  ├─ SyncAutoTheme()           apply dark/light based on system if AutoTheme=true
  └─ (optional) ShowWizard()   if no config.json existed
```

---

## 3. WiFi monitoring

MasselGUARD uses the native `wlanapi.dll` rather than WMI or process spawning, so it reacts in real time without polling.

```
WlanRegisterNotification()
  └─ callback fires on ACM codes:
       9  = connected
       10 = disconnected
       21 = network roaming

OnWifiChanged()
  ├─ GetCurrentSsid()    WlanQueryInterface(WLAN_INTF_OPCODE_CURRENT_CONNECTION)
  ├─ Update status bar
  ├─ Log WiFi: <SSID> (secured / open)
  └─ ApplyRules(ssid)    (skipped in manual mode)
```

`GetCurrentSsid()` reads `WLAN_CONNECTION_ATTRIBUTES` directly from memory:

| Offset | Field | Used for |
|---|---|---|
| 520 | `uSSIDLength` | SSID byte length |
| 524 | `ucSSID[32]` | SSID bytes (UTF-8) |
| 580 | `bSecurityEnabled` | 0 = open network |

A 1-second `DispatcherTimer` also calls `UpdateStatusDisplay()` to keep the active tunnel label and tray icon in sync between WiFi events.

---

## 4. LAN / Ethernet monitoring

MasselGUARD subscribes to .NET's `NetworkChange.NetworkAvailabilityChanged` and `NetworkChange.NetworkAddressChanged` events. On each change it calls `GetConnectedLanAdapters()` which enumerates all `OperationalStatus.Up` adapters of type `Ethernet`, excluding WireGuard, loopback, and virtual adapters.

Each detected adapter provides:

| Property | Source | Example |
|---|---|---|
| `Name` | Windows adapter name | `"Ethernet"`, `"LAN 2"` |
| `Description` | Hardware description | `"Intel(R) Ethernet Connection I219-V"` |
| `DnsSuffix` | `GetIPProperties().DnsSuffix` | `"corp.example.com"` |
| `GatewayIp` | First non-link-local gateway | `"192.168.1.1"` |

The adapter list is diffed against the previous state. Newly connected adapters trigger `ApplyLanRules(adapter)`. Disconnected adapters are logged. Manual mode suppresses LAN rules the same as WiFi rules.

---

## 5. Rule evaluation

### WiFi rules

`ApplyRules(ssid)` runs every time the WiFi network changes. Priority order:

```
1. Open network protection
   └─ Is bSecurityEnabled = 0?
   └─ Is OpenWifiTunnel configured?
   └─ Yes → SwitchTo(OpenWifiTunnel)  STOP

2. SSID rules
   └─ Does any rule match the SSID exactly?
   └─ Yes, tunnel set   → SwitchTo(rule.Tunnel)   STOP
   └─ Yes, tunnel empty → DisconnectAll()          STOP

3. Default WiFi action
   └─ "none"       → do nothing
   └─ "disconnect" → DisconnectAll()
   └─ "activate"   → SwitchTo(DefaultTunnel)
```

### LAN rules

`ApplyLanRules(adapter)` runs when a new Ethernet adapter is detected:

```
1. Specific LAN rules
   └─ For each rule: does AdapterFilter match adapter Name, Description, or DnsSuffix?
      (partial, case-insensitive)
   └─ Yes, tunnel set   → SwitchTo(rule.Tunnel)   STOP
   └─ Yes, tunnel empty → DisconnectAll()          STOP

2. Default LAN action
   └─ "none"       → do nothing
   └─ "disconnect" → DisconnectAll()
   └─ "activate"   → SwitchTo(LanTunnel)
```

`SwitchTo(target)` first stops any active tunnel that is not `target`, then starts `target`. If `target` is already running it logs `LogAlreadyActive` and returns immediately.

---

## 6. Connecting a tunnel — Standalone

```
StartTunnel(name)
  ├─ RunTunnelScript(PreConnectScript)    run pre-connect script if configured
  ├─ ValidateDlls()                       check tunnel.dll and wireguard.dll exist
  ├─ DpapiDecrypt(confPath)               → plaintext WireGuard config
  ├─ WriteSecure(SvcConfPath)
  │   ├─ File.Create()                    empty file, inherits parent ACL
  │   ├─ SetAccessControl(fileSec)        lock to SYSTEM + Admins + user
  │   └─ StreamWriter.Write(plaintext)    write config bytes
  ├─ TunnelDll.Connect(name, svcConf)
  │   └─ Creates WireGuardTunnel$<n> SCM service → wireguard-NT kernel driver
  ├─ Delete SvcConfPath immediately
  └─ RunTunnelScript(PostConnectScript)   run post-connect script if configured
```

The plaintext temp file lives for under 200 ms. The kernel driver keeps the tunnel active after the service process exits.

---

## 7. Connecting a tunnel — Companion

```
StartTunnel(name) — Companion path
  ├─ RunTunnelScript(PreConnectScript)
  ├─ EnsureManagerRunning()
  ├─ ServiceController(SvcName(name)).Start()
  ├─ WaitForStatus(Running, 15 s)
  └─ RunTunnelScript(PostConnectScript)
```

---

## 8. Disconnecting a tunnel

```
StopTunnel(name)
  ├─ RunTunnelScript(PreDisconnectScript)
  ├─ [local]  TunnelDll.Disconnect(name)  → sc.Stop() + sc.Delete()
  │   or
  │   [WG]    ServiceController.Stop() + WaitForStatus(Stopped, 15 s)
  └─ RunTunnelScript(PostDisconnectScript)
```

---

## 9. Pre/post scripts

Each tunnel can run a `.bat` or `.ps1` script at four hook points.

| Hook | When |
|---|---|
| `PreConnectScript` | Before the tunnel service starts |
| `PostConnectScript` | After the tunnel is confirmed running |
| `PreDisconnectScript` | Before the tunnel service is stopped |
| `PostDisconnectScript` | After the tunnel is stopped |

Script values take two forms:

- **Path** — `C:\scripts\vpn-up.ps1` — file is called at runtime
- **Embedded** — `@embed:<content>` — content is written to a temp file and executed, then deleted

`.ps1` files run via `powershell.exe -ExecutionPolicy Bypass -File <path>`. `.bat` files run via `cmd.exe /c <path>`. Stdout, stderr, and exit code are logged. A non-zero exit is logged as a warning but does not abort the connect/disconnect operation.

---

## 10. Tunnel groups and categories

Each tunnel can be assigned to a named group. Groups are managed in Settings → General → Tunnel Groups. The tunnel list shows a tab strip: All · group tabs · Uncategorized.

Tabs are built by `RebuildTunnelGroups()`. Selection is preserved by name across rebuilds so that Edit and Delete work on the first click after any save.

---

## 11. Quick Connect

Quick Connect opens any `.conf` or `.conf.dpapi` from disk and connects immediately, without permanently importing.

```
QuickConnect_Click()
  ├─ OpenFileDialog (*.conf, *.conf.dpapi)
  ├─ ReadAllBytes + DpapiDecrypt if needed
  ├─ Store in _quickConnectConfig (in-memory only)
  ├─ StartTunnel via local path
  └─ Show "⚡ <filename>" at top of tunnel list
```

The config is never written to `%APPDATA%\MasselGUARD\tunnels\`.

---

## 12. Open network protection

Detects open (passwordless) WiFi by reading `WLAN_SECURITY_ATTRIBUTES.bSecurityEnabled` at offset 580. A value of `0` means no security. Activates the configured protection tunnel **before** any SSID rule or default action. Configure in Settings → WiFi Rules → Open Network Protection.

---

## 13. Configuration and storage

### config.json

Stored at `%APPDATA%\MasselGUARD\config.json`.

```json
{
  "Rules":    [ { "Ssid": "HomeWifi", "Tunnel": "home" } ],
  "LanRules": [ { "AdapterFilter": "corp.example.com", "Tunnel": "office", "NetworkType": "ethernet" } ],
  "TunnelGroups": [ { "Name": "Work", "IsExpanded": true } ],
  "DefaultAction":  "activate",
  "DefaultTunnel":  "home",
  "LanAction":      "activate",
  "LanTunnel":      "office",
  "OpenWifiTunnel": "home",
  "Mode":           "Standalone",
  "ManualMode":     false,
  "Language":       "en",
  "ActiveTheme":    "default-dark",
  "AutoTheme":      false,
  "LogLevelSetting": "normal"
}
```

`LanRules[].AdapterFilter` is matched case-insensitively against the adapter name, hardware description, and DNS suffix of each connected Ethernet adapter.

### Tunnel configs

| Path | Format |
|---|---|
| `<ExeDir>\tunnels\<n>.conf.dpapi` | DPAPI-encrypted WireGuard config |
| `<ExeDir>\tunnels\temp\<n>.conf` | Plaintext copy for service process (deleted within ~200 ms) |

---

## 14. Security model

### DPAPI encryption

Tunnel configs are stored as `.conf.dpapi` files encrypted with Windows DPAPI (`DataProtectionScope.CurrentUser`). Decryption key is derived from the user's Windows login credentials — MasselGUARD never stores or handles keys.

### Atomic temp file

```csharp
File.Create(path).Dispose();                    // 1. create empty — inherits parent ACL
new FileInfo(path).SetAccessControl(fileSec);   // 2. apply restrictive ACL before first byte
using var sw = new StreamWriter(new FileStream( // 3. write plaintext under correct ACL
    path, FileMode.Open, FileAccess.Write, ...));
```

ACL: `SYSTEM + Administrators + owning user` only. File deleted within ~200 ms.

---

## 15. Theme system

Themes live in `theme/<folder>/theme.json`. See `theme/THEME_INFO.md` for the full key reference.

### Built-in themes

| Folder | Type | Style |
|---|---|---|
| `default-dark` | dark | Rounded (6 px) |
| `default-light` | light | Rounded (6 px) |
| `grey-dark` | dark | Sharp (0 px) |
| `grey-light` | light | Sharp (0 px) |
| `highcontrast-dark` | dark | Near-sharp (2 px), pure black bg, WCAG AAA |
| `highcontrast-light` | light | Near-sharp (2 px), pure white bg, WCAG AAA |

### Hot-swap

`ThemeManager.Instance.Load(folder)` applies all theme values directly into `Application.Current.Resources`. Every `{DynamicResource ...}` binding updates immediately.

### Auto-switching

When `AutoTheme = true`, a 5-second polling timer reads `HKCU\...\Themes\Personalize\AppsUseLightTheme` and loads `ActiveDarkTheme` or `ActiveLightTheme` accordingly.

### Themeable log timestamp

`colorLogTimestamp` in `theme.json` sets the timestamp colour in the activity log. Defaults to `colorBorder` if not set. Allows the timestamp to be visually separated from the log message text.

---

## 16. Logging

### Log levels

| Level | Setting | What appears |
|---|---|---|
| Normal | `"normal"` | OK + Warn only |
| Extended | `"extended"` | Everything including `[DBG]` entries |

Change in Settings → Advanced → Log level.

### Info-level events (Extended only)

- WiFi network change (SSID + security status)
- LAN adapter connected or disconnected (name + DNS suffix)
- Auto-switch trigger reason
- App mode change, manual mode toggle
- Language change, theme change, log level change

### Debug entries (prefixed `[DBG]`, Extended only)

- Startup: OS, .NET runtime, platform, domain\user
- Before connect: address, DNS, endpoint, AllowedIPs, public key prefix, MTU
- Service conf path
- WireGuard service name and exe path
- Connect/disconnect timing (ms)
- Detected LAN adapters: name, DNS suffix, gateway

### Continuation lines

Lines starting with spaces (detail/sub-entries) are rendered with a `↳` prefix in the timestamp colour, visually grouping them with their parent line.

### Export

Click **Export Log** in the Activity Log area to save to UTF-8 `.txt`.

---

## 17. Settings panel

The Settings window has 7 sidebar tabs. All settings except app/tunnel mode changes apply immediately (no Save button required for most tabs).

| Tab | Key settings |
|---|---|
| **General** | Language, app mode (Standalone/Companion/Mixed), manual mode, tunnel groups |
| **Appearance** | Dark/light theme pickers, auto system theme, background tunnel notifications |
| **WiFi Rules** | Manual mode toggle, SSID→tunnel rules list, open network protection tunnel |
| **Default Action** | WiFi fallback (none/disconnect/activate + tunnel), LAN/Ethernet fallback |
| **LAN Rules** | Specific LAN network rules — match by adapter name or DNS suffix |
| **Advanced** | Install/uninstall, DLL status, WireGuard client, orphaned service cleanup, log level |
| **About** | Version, update check |

---

## 18. Build and deployment

### BUILD.bat

```bat
BUILD.bat
```

1. Verify .NET 10 SDK
2. `dotnet publish` → `dist\`
3. Copy `theme\` → `dist\theme\`
4. Copy `wireguard-deps\tunnel.dll` + `wireguard-deps\wireguard.dll` → `dist\`

If DLLs are missing in `wireguard-deps\`, a warning points to `tunnelbuild\tunnelbuild.bat`.

### tunnelbuild\tunnelbuild.bat

Builds `tunnel.dll` from source (requires Go 1.21+ and gcc/MinGW). Downloads `wireguard.dll` from download.wireguard.com/wireguard-nt/. Outputs to `tunnelbuild\wireguard-deps\`. After running, copy the DLLs to the root `wireguard-deps\` folder before running `BUILD.bat`.

### Runtime requirements

| | |
|---|---|
| OS | Windows 10 / 11 x64 |
| Runtime | .NET 10 Desktop Runtime |
| Elevation | Administrator (UAC prompt on launch) |
| Standalone / Mixed | `tunnel.dll` + `wireguard.dll` next to exe |
| Companion / Mixed | WireGuard for Windows installed |

---

## 19. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Tunnel connects but Event Viewer shows error | wireguard-NT service exits immediately after driver loads — SCM logs false positive | Ignore; check tunnel list status |
| Config file not found on connect | Migration needed from older version | Delete and re-add the tunnel |
| LAN rule not firing | Adapter name or DNS suffix doesn't match | Open Settings → LAN Rules; check the filter against the adapter name shown in Windows Network Connections, or enable Extended logging to see the detected DNS suffix |
| Edit button stays disabled | Row must be selected first (click the row, not the button in the row) | Click the tunnel name in the list |
| Pre/post script not running | Script path doesn't exist or has spaces without quotes | Use Browse button; check Extended log for `[Script]` entries |
| Theme not appearing in picker | `theme.json` missing `type` field or malformed JSON | Ensure `type` is `"dark"` or `"light"` |
| Settings page content clipped | Resize the settings window taller | Drag the bottom edge |
