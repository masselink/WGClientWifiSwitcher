# MasselGUARD — Changes from v2.5.0 to v2.9.0

---

## Architecture — Full MVVM rewrite

The entire application was restructured from a monolithic code-behind into a clean MVVM architecture.

| Layer | Files | Responsibility |
|---|---|---|
| `Models/` | `AppConfig`, `StoredTunnel`, `TunnelRule`, `TunnelGroup`, `LogEntry` | Pure data, no UI references |
| `Services/` | `ConfigService`, `TunnelService`, `WiFiService`, `RuleEngine`, `LogService`, `ScriptService` | Business logic, no UI references |
| `Infrastructure/` | `ObservableObject`, `RelayCommand`, `AsyncRelayCommand` | MVVM base classes |
| `ViewModels/` | `MainViewModel`, `SettingsViewModel`, `WizardViewModel`, `TunnelEntryViewModel` | State + commands |
| `Views/` | All `.xaml.cs` files | UI glue only |

---

## Tunnel list

- `ListView` + `DataTemplate` with proportional columns
- Full-width hover and selection via `ListHover` / `ListSelected` resources
- 4 px colour strip per tunnel row showing group colour
- Drag-to-reorder with live drop-line indicator
- Uptime counter in Status column (`< 1 min` / `Xs` / `Xm YYs` / `Xh YYm` / `Xd YYh YYm`)
- **⚡** default action badge and **🔓** open network protection badge inline after tunnel name
- **Rules column**: shows count of WiFi rules referencing each tunnel; click to highlight matching rules; rebuilds immediately on rule add/edit/delete
- Action column centred (header and button)
- Right-click context menu removed — replaced by **Defaults button** popup

---

## Defaults button

- Located in the tunnel management toolbar before Import
- Opens a themed `Window` popup centred on the main window
- **⚡ Default action tunnel** dropdown — all tunnels + "— clear —"
- **🔓 Open network protection** dropdown — all tunnels + "— clear —"
- Save commits both values immediately; badges, footer, and WiFi rules panel update in place

---

## WiFi Rules panel (main window)

- Columns: **Name** (3\*) | **SSID** (2\*) | **Action** (1.5\*) | **Hits** (40 px) | **Tunnel** (2\*)
- **Hits** column: how many times each rule has been triggered (`TunnelRule.ExecutionCount`, persisted in config, shown in Accent when > 0)
- **Name field** in rules: auto-generates from SSID + tunnel as you type; stops generating once manually edited; falls back to auto-generated on save if empty
- Drag-to-reorder rules within the panel
- Add/Edit/Delete refresh both the WiFi Rules panel and the tunnel list Rules column
- Column header renamed from `#` to `Hits`

---

## WiFi rule dialog (RuleDialog)

- New **Name** field at top; auto-generates from SSID + tunnel
- Existing rules without a stored name show auto-generated display name without migration

---

## Settings — fully deferred save

All changes stage into `_draft = ConfigSvc.Config.DeepClone()` until Save is pressed.

- Cancel reverts all changes including live theme preview
- **WiFi rules Save button removed** — main Save commits everything
- On Save: `_draft` → `_vm` sync → `DoSave()` → `ConfigSvc.Save()`
- Extended log on Save: only **changed fields** logged as `[DBG] [Settings] FieldName: old → new`
- Rules changes, group changes, and individual field changes all logged separately

---

## Settings — layout

| Tab | Content |
|---|---|
| **General** | Language, App mode |
| **Tunnel Groups** *(new)* | Always hide count, hide empty, group list, add group |
| **Appearance** | Dark/light theme, auto theme, notifications, notification duration |
| **Default Action** | Default action + open network protection tunnel |
| **WiFi Rules** | Rules list (top) → Add/Edit/Delete → Disable rules toggle → Hide panel toggle → Show rules column toggle |
| **Advanced** | Import/Export → Log level → Installation → Start with Windows → WireGuard client → Orphaned services → DLL status → Update |
| **About** | Version, update check |

**Tunnel Groups tab** extracted from General — group management (add/edit/delete/reorder, colour, visibility, hide count, hide empty) has its own dedicated page.

**WiFi Rules page reordered** — rules list at top, automation settings below.

**Advanced tab reordered** — WireGuard client moved below Installation + Start with Windows.

---

## Tunnel groups

- **Drag tunnels into groups** — drag any tunnel row onto a group tab to reassign it; saves immediately; rebuilds tabs
- Group tab `Tag="Active"` highlight fixed (was falling through to General)
- Groups tab correctly labelled "Tunnel Groups" in all languages
- `_vm.TunnelGroups` synced from `_draft` before `DoSave()` to prevent overwrite

---

## Tray menu

- `ShowImageMargin = false` — removes blank left column
- GDI+ icons per item: 🪟 Show Window (Accent), 🛡 Tunnels (green/muted shield), ⬛→ Exit (ErrorColor)
- Shield icon updates green/muted based on active tunnel count on every `UpdateTrayStatus` call
- Font: Segoe UI

---

## Custom WPF toast notifications

`Views/ToastWindow.cs` — fully themed, no system balloon tip.

**Layout (Option C):**
```
╔══════════════════════════════════════════╗
║ 🛡 MasselGUARD  ·  WiFi Rule Matched  ✕ ║
╟──────────────────────────────────────────╢
║  1.MasselinkVPN-Split-AG                 ║
║  Rule: MasselNET → activate              ║
╚══════════════════════════════════════════╝
 ▌ 4 px coloured left strip
```

- **AppName** from `Theme.AppName` resource — custom themes override it
- **Category** label: WiFi Rule Matched / Open Network Protection / Default Action
- **Strip colour** resolves as a resource key: `Accent` (rule), `Success` (open network), `Warning` (default action) — custom themes override all three
- Slides in from bottom-right (220 ms `CubicEase EaseOut`), slides out (180 ms `EaseIn`)
- Auto-closes after configurable duration (3 / 5 / 10 / 15 / 30 s, set in Appearance)
- Click anywhere or ✕ to dismiss early
- Content-hash dedup: identical notification within 1 second is suppressed
- One toast at a time — new toast closes previous

---

## Activity log

- Column header bar: **Time** (62 px) | **Event** (\*) — consistent with Tunnels and WiFi Rules panels

---

## Build number scheme

`BUILD.bat` generates `2.9.0.YYMMDDHHMM` using PowerShell `Get-Date -Format yyMMddHHmm`, injects into `UpdateChecker.cs` via a temp `.ps1` file before compiling.

Banner:
```
  ----------------------------------------
    MasselGUARD  v2.9.0.2505181430
    Harold Masselink  |  Claude.ai
  ----------------------------------------
```

---

## Managed Portable version prompt

Prompts on **any version difference** (not just newer) — covers same base with different build number. Message adapts: "is newer than" vs "differs from". `NormaliseVersion()` strips leading `v` and whitespace before comparison.

---

## WiFi detection — double-fire prevention

Two guards prevent rules firing twice on a network switch:

1. `ApplyWifiState` entry: `if (ssid == _currentSsid) return`
2. Debounce callback: `if (live != _currentSsid) ApplyWifiState(...)` — only re-applies if the connect event hasn't already handled this SSID

---

## Bug fixes

| Bug | Fix |
|---|---|
| Groups tab not clickable | `"TabBtnGroups"` missing from `TabBtn_Click` switch |
| Tunnel groups not saving | `_vm.TunnelGroups` not synced from `_draft` before `DoSave()` |
| Group picker empty in tunnel dialog | `GetConfigStatic()` was stub returning null; now passes `groupNames` directly |
| Rules/notifications fire twice on network switch | Debounce re-fire guard + `ApplyWifiState` duplicate guard |
| Settings show all fields as changed on first save | `_draft` was cloning `new AppConfig()` instead of live config |
| Theme not applying when picking | `ActiveDarkTheme`/`ActiveLightTheme` setters call `ThemeManager.Instance.Load()` immediately |
| Theme not reverting on Settings cancel | `OnClosing` restores `_originalTheme` when `_savedSuccessfully = false` |
| `FlatBtn`/`TextMuted` resource errors on dialogs | Standalone `Window` objects use `Application.Current.Resources[...]` |
| Delete rule used native `MessageBox` | Replaced with `ShowThemedYesNo` |
| `[BtnSkip]` shown as literal in wizard | Key corrected to `[WizardBtnSkip]` |
| Popup at wrong position on HiDPI | `PointToScreen` + DPI scale from `PresentationSource` |
| Toggle clipping in dialog footers | `Height="Auto"` + `Grid Margin="16,10"` |
| `wmic` not found on Windows 11 | Replaced with `powershell Get-Date -Format yyMMddHHmm` |
| `Set-Content` not found in BUILD.bat | Moved to temp `.ps1` file run with `powershell -File` |
| Rules column not updating on rule change | `_vm.RebuildTunnelList()` added to add/edit/delete handlers |
