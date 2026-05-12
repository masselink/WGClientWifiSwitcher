# MasselGUARD — Release Notes

All notable changes are documented here. The most recent release is at the top.

---

## v2.5.0

### Features

**Settings panel redesigned — independent sections**
The Settings window is now wider (700 px) and has 7 sidebar tabs, each a dedicated full-height page:

| Tab | Contents |
|---|---|
| General | Language, app mode, tunnel groups |
| Appearance | Theme pickers, auto-switch, background notifications |
| WiFi Rules | Manual mode toggle, SSID rules, open network protection |
| Default Action | WiFi fallback action + LAN/Ethernet fallback action |
| LAN Rules | Specific LAN network rules (adapter name or DNS suffix) |
| Advanced | Install, DLLs, WireGuard, orphans, log level |
| About | Version, update checker |

Rules, Default Action and Open Network were previously tabs in the main window right panel. They now live in Settings where they belong, leaving the right panel as a clean Activity Log only.

**LAN / Ethernet detection and rules**
MasselGUARD now monitors Ethernet connections using `NetworkChange.NetworkAvailabilityChanged` and `NetworkAddressChanged`. When an Ethernet adapter comes up it reads:
- Adapter name (e.g. `"Ethernet"`, `"LAN 2"`)
- Hardware description
- DNS suffix (e.g. `"corp.example.com"`, `"office.local"`)
- Default gateway IP

Specific LAN rules match on adapter name or DNS suffix (partial, case-insensitive). If no specific rule matches, the LAN default action fires as fallback. Both WiFi and LAN default actions are configured on the Default Action tab. LAN-specific network rules are on the LAN Rules tab.

**Pre/post scripts for tunnels**
Every tunnel (local and WireGuard-linked) can now run `.bat` or `.ps1` scripts at four hook points: before connect, after connect, before disconnect, after disconnect. Scripts are configured in the Edit dialog. For local tunnels an Embed option writes the script content inline into the config (stored as `@embed:...`); for WireGuard tunnels a Browse-only path is stored. Scripts run as the current user; stdout/stderr and exit code are logged.

**Log levels simplified to Normal / Extended**
Four log levels (Normal, Info, Verbose, Debug) collapsed to two:
- **Normal** — OK and Warn messages only
- **Extended** — everything, including `[DBG]` entries

The `service-debug.log` file written to disk is removed. All diagnostic output goes to the in-app activity log only.

**Activity log improvements**
- Continuation lines (indented detail lines like `[DBG]` entries) now render with a `↳` prefix instead of raw leading spaces, keeping the log visually aligned.
- Timestamp colour is now themeable via `colorLogTimestamp` in `theme.json`. Falls back to `colorBorder` if not set.
- Log text area background set to `Transparent` so the parent `Border`'s corner radius renders correctly.

**Tray icon badge counter**
The tray icon now shows a green badge in the bottom-right corner with the number of active tunnels when one or more tunnels are connected. Shows `9+` for counts above 9.

**High-contrast themes**
Two new built-in themes: `highcontrast-dark` and `highcontrast-light`. Pure black/white backgrounds, full-opacity borders, WCAG AAA compliant colour choices. Corner radius 2 px (near-sharp). Suited for low-vision users and high-brightness environments.

**Single-instance dialog improved**
The already-running dialog now shows an informational message explaining how to close the existing instance (right-click tray icon → Exit, or close the main window) and a single Close button. Previously offered a two-button choice that could bring the running instance to front, which was confusing.

**DLL layout reorganised**
`tunnel.dll` and `wireguard.dll` moved to `wireguard-deps\` subfolder in the source tree. `BUILD.bat` copies from there automatically — no prompt, no choices.

**`BUILD.bat` simplified**
All download/build DLL options removed. Build simply compiles, copies themes, and copies DLLs from `wireguard-deps\`. If DLLs are missing it points to `tunnelbuild\tunnelbuild.bat`.

**`tunnelbuild\` subfolder**
`tunnelbuild.bat`, `get-wireguard-dlls.ps1`, and `debug-tunnel.ps1` moved to a dedicated `tunnelbuild\` subfolder. `tunnelbuild.bat` builds `tunnel.dll` from source (requires Go + gcc) and places the result in `tunnelbuild\wireguard-deps\`. Download option removed.

**Installer copies all subdirectories**
When installing over an existing version, all subdirectories (`theme\`, `lang\`, etc.) are recursively copied and overwritten.

### Bug fixes

- **Already-running dialog buttons unreadable** — dark text on dark background. Fixed: Show button gets a green-tinted background with matching border; Exit button gets a neutral background with visible border.
- **Activity log leading spaces** — indented continuation lines (`"  [DBG] ..."`) were inserted verbatim causing uneven spacing. Now stripped and replaced with a styled `↳` marker.
- **Log corner radius** — `RichTextBox` was drawing its own opaque background, overwriting the parent `Border`'s rounded corners. Fixed by setting `Background="Transparent"` on the `RichTextBox`.
- **`service-debug.log` written to disk** — `TunnelDll.WriteDebug()` wrote a persistent log file next to the exe. Replaced with no-op; all output routed through the in-app log.
- **`_loading` CS0414 warning** — `MainWindow._loading` field was set but never read after handlers moved to `SettingsWindow`. Field and both set-sites removed.
- **`GetConnectedLanAdapters` CS0120** — `LogRaw` called from `static` method. Moved debug log to instance method `OnLanChanged`.
- **`DeleteRulePublic` CS1061** — method existed for LAN rules but not for WiFi rules. Added.
- **Duplicate `EditRulePublic` body** — Python replace left trailing duplicate `SaveConfig` + `}` lines. Removed.
- **Dangling `<summary>` comment** — orphaned XML doc comment left after method deletion caused CS1001/CS1519. Removed.
- **`MC3000` double comment marker** — Python string replacement duplicated `<!-- ══ APPEARANCE PAGE` in XAML, creating `--` inside a comment. Fixed.
- **`TunnelDll.WriteDebug` no-op** — body replaced; `WriteDebug` calls remain in the code for compatibility but do nothing.

---

## v2.3.1

### Features

**Language picker redesign**
The language picker now shows a styled `[XX]` code badge (accent colour, Consolas font) next to the language name — replacing flag emoji which render as plain text pairs (e.g. "GB") in WPF on Windows.

### Bug fixes

- **Right panel empty-state background** — when Default Action or Open Network tabs were active, the button row area below the content had a different background shade than the left panel. Fixed by replacing the `Border`/`Grid` wrapper with a direct `Grid` matching the left panel's structure.
- **Right panel top gap** — 8 px `Margin` on the content grid caused a visible gap between the tab strip and content on the right side. Removed.
- **Shield not showing after logo-theme switch** — switching away from a theme with a `logo` to one without left `HasBuiltinIcon = Collapsed` permanently. Fixed: `ApplyLogo` now always sets `HasBuiltinIcon = Visible` in the no-logo path.
- **`AppIcon` scope** — `appIcon` previously also showed an extra image in the title bar. Clarified: `appIcon` affects only the tray icon and `Window.Icon` (taskbar). The title bar shows `logo` or the built-in shield only.
- **Tunnel name colours not updating on theme switch** — `NameColor` and `TypeColor` did not raise `PropertyChanged` on theme change. `RefreshLabels()` now fires these and `StatusColor`, and is called on all tunnel entries from `UpdateThemeToggleIcon()`.
- **`LogLevelInfo` / `LogLevelVerbose` missing translations** — keys were referenced in Settings but never added to any lang file. Added to all 5 languages.
- **Double code badge in language picker** — `_language` in JSON already contains the ISO code; `LangItem.Display` was prepending it again. Now uses `Name` directly.
- **`CharacterSpacing` MC3072** — WinUI-only property removed from WPF XAML.
- **Bottom button row colour mismatch** — `Border` + `Grid` wrapper was removed; replaced with a plain `Grid` matching the left side.

---

## v2.3.0

### Features

**Tunnel groups and category tabs**
Tunnels can be organised into named groups (Work, Personal, Travel, or any custom group). The tunnel list shows a tab strip: All · group tabs · Uncategorized. Uncategorized is always present and not configurable. Groups are managed in Settings → General → Tunnel Groups (add, rename, reorder ↑↓, delete). Deleting a group moves its tunnels to Uncategorized.

**WireGuard profile metadata editing**
WireGuard-linked tunnels (Companion mode) can now be edited: the Edit button opens a dedicated metadata dialog where group and notes can be set. The `.conf` file owned by WireGuard is never touched.

**Tunnel notes**
Each tunnel has a Notes field (shown as a tooltip on the tunnel name in the list). Set via Edit for both local and WireGuard-linked tunnels.

**Theme system — full customisation**
- Four built-in themes: Default Dark, Default Light, Grey Dark (sharp corners), Grey Light (sharp corners)
- Themes now hot-swap without restart
- Appearance settings moved to their own Appearance tab in Settings
- Dark and light theme pickers filtered by `type` field
- Auto-switching based on Windows system theme preference (5-second poll)
- Theme toggle button in title bar cycles Dark → Light → Auto

**Detailed activity log**
WiFi changes, theme/language/mode changes all logged. Each save logs what changed. Debug entries prefixed `[DBG]` in a distinct muted colour.

**BUILD.bat improvements**
Theme folder copied to `dist\` automatically. DLL options menu added.

### Bug fixes

- Selection lag / missed clicks after any save operation
- Edit button disabled after save
- Various CS0103, CS1061, duplicate definition, and XAML errors

---

## v2.2.1

### Bug fixes

- Orphan warning text overflowed the settings card
- OpenWifi tunnel selection cleared on every tunnel list refresh
- Double ✓ / ⚠ prefix in wizard mode status panel
- Settings window height increased for wrapped checkbox text
- Tray toast duration increased to 6 seconds; font size increased to 15 pt

---

## v2.2

### Features

- Setup wizard (first-run and re-runnable)
- Tabbed Settings window: General / Advanced / About
- Tray popup notifications with reason context
- Open network protection
- Orphaned service cleanup in Settings → Advanced
- Quick Connect in the active tunnel list
- Delete / Unlink / Remove morphing toolbar button
- German, French, Spanish language support

---

## v2.1

- DPAPI encryption for tunnel configs
- Atomic temp file creation with FileSecurity
- Tunnel storage in `<ExeDir>\tunnels\`
- GitHub tags API for version checking

---

## v2.0

Initial release. Standalone, Companion and Mixed modes. WiFi auto-switching via `WlanRegisterNotification`. WiFi rules, default action, Quick Connect, system tray, single-instance guard, English and Dutch.
