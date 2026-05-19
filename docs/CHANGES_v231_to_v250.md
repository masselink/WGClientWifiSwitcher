# MasselGUARD — Changes from v2.3.1 to v2.5.0

There was no intermediate release. All of the following shipped together as **v2.5.0**.

---

## Settings panel — completely redesigned

The Settings window grew from a 3-tab panel (General / Advanced / About) to a **6-tab, 700 px wide** panel with dedicated pages for every major feature area.

| Old tab | New tab(s) |
|---|---|
| General (contained rules, default action, open network) | **General** — language, mode, groups only |
| — | **Appearance** — themes, notifications |
| — | **Default Action** — WiFi fallback + open network protection |
| — | **WiFi Rules** — disable rules toggle, SSID rules |
| Advanced | **Advanced** — install, DLLs, import/export, log level |
| About | **About** |

Rules, Default Action, and Open Network Protection were previously right-panel tabs inside the main window. They now live in Settings. The right panel is a clean Activity Log only.

---

## Pre/post scripts

Every tunnel can now run a `.bat` or `.ps1` script at four points:

- **Before connect**
- **After connect**
- **Before disconnect**
- **After disconnect**

Scripts are set on the **Scripts** tab when editing a local tunnel, or in the metadata dialog for WireGuard-linked tunnels. Scripts can be stored as a file path or **embedded inline** (`@embed:...` — written to a temp file, executed, then deleted). Exit code and stdout/stderr appear in the Activity Log.

---

## Log levels simplified

Four log levels (Normal / Info / Verbose / Debug) collapsed to two:

| Level | What is shown |
|---|---|
| **Normal** | OK (green) and Warn (orange) only |
| **Extended** | Everything, including Info events and `[DBG]` diagnostic detail |

The `service-debug.log` file that was written to disk next to the exe is removed. All output goes through the in-app Activity Log only.

---

## Two new themes: High Contrast Dark and High Contrast Light

Near-sharp corners (2 px), pure black/white backgrounds, full-opacity borders, WCAG AAA colour choices. Suited for low-vision users and high-brightness environments. Brings the total number of built-in themes to six.

---

## Tray icon badge counter

The tray icon now shows a green badge in the bottom-right corner with the number of active tunnels. Shows `9+` for counts above nine.

---

## Deferred save for WiFi Rules

Add, Edit, and Delete on the WiFi Rules page update memory only — nothing is written to disk until you press **Save**. Other settings (Default Action, theme, language) continue to save immediately on change.

---

## Import / Export settings

### Export

Located in **Settings → Advanced → Export settings**.

- Shows a warning before exporting: tunnel configs are not included and future-version compatibility is not guaranteed
- Writes a `*.masselguard` JSON file containing: WiFi rules, tunnel groups, default action, open network tunnel, disable-WiFi-rules toggle, app mode, language, themes, log level, and popup setting

### Import

Located in **Settings → Advanced → Import settings**.

- Accepts `*.masselguard` or `*.json` files
- Compares the file's `AppVersion` to the running version — shows a **Yes/No** warning for any mismatch (both older-to-newer and newer-to-older)
- Uses a field-by-field `JsonDocument` parser — unknown or future fields are silently ignored, so old exports remain loadable in newer versions
- Rules and tunnel groups replace existing lists entirely; all other fields merge

---

## Setup wizard updated (5 → 6 steps)

| Step | Content |
|---|---|
| 0 | Welcome |
| 1 | Language |
| 2 | Operating mode |
| 3 | Disable WiFi rules toggle |
| 4 | Rules & Automation overview — explains WiFi Rules and Default Action |
| 5 | Done — version card with update checker |

The language picker now matches Settings: an accent-coloured `[XX]` code badge next to the language name. Previously showed `"NL Nederlands"` because `DisplayMemberPath` bypassed the `DataTemplate` and fell back to `ToString()` which includes the code prefix. Fixed.

---

## Open Network Protection moved to Default Action

Logically grouped with the other fallback behaviours (WiFi default action). Was previously at the bottom of the WiFi Rules page.

---

## "Manual mode" renamed to "Disable WiFi rules"

The toggle that disables network-based automation is now labelled **Disable WiFi rules** throughout the UI, wizard, and documentation.

---

## Single-instance mutex fix

After installing MasselGUARD to a new path and relaunching, the app would show "already running" even when nothing was actually running.

**Root cause:** the named mutex (`Global\MasselGUARD_SingleInstance`) is held briefly by the OS after process exit. After moving the exe (during install), the old process's mutex handle hadn't been released yet when the new exe started.

**Fix:** on mutex failure, the app now checks whether an actual MasselGUARD process exists (by process name, excluding self). If none is found, it retries up to 4 × 500 ms for the mutex to become available. The "already running" dialog is only shown if the mutex is held **and** a real process is confirmed running.

---

## Info blocks in Settings

Each Settings section now has a styled info card (Surface background, border, rounded corners) with a plain-text description above the interactive controls. Replaces the old inline hint text that was mixed into the cards themselves.

---

## Dialog button labels

Rule dialogs (Add/Edit WiFi rule) changed from **Cancel / Save** to **Cancel / OK**. Consistent vertical padding added to the button row. Same change applied to the Add/Edit tunnel dialogs.

---

## BUILD.bat — telemetry opt-out

`DOTNET_CLI_TELEMETRY_OPTOUT=1` and `DOTNET_NOLOGO=1` added to both `BUILD.bat` and `tunnelbuild\tunnelbuild.bat`. Suppresses the .NET welcome banner and usage telemetry during builds.

---

## Code cleanup

- `TunnelDll.WriteDebug()` — no-op method and all 11 call sites removed
- Dead right-panel tab infrastructure removed: `ShowRightTab`, `RightTab_Click`, `_activeRightTab`
- `MainWindow._loading` field removed — no longer used after handlers moved to `SettingsWindow`
- 10 dead lang keys removed from all 5 language files
- Excessive blank lines normalised across source files

---

## Bug fixes

| # | Bug | Fix |
|---|---|---|
| 1 | `[RTabLog]` shown as literal text in the Activity Log header | Key was deleted during cleanup but still referenced in XAML. Replaced with `ActivityLogHeader`. |
| 2 | "Settings saved" appearing twice on a single save | `DefaultTunnelBox_SelectionChanged` set `ActionActivate.IsChecked = true`, which fired `DefaultAction_Changed` → a second `SaveConfig` call. Fixed by wrapping the `IsChecked` assignment in a `_loading` guard. |
| 3 | Activity log corner radius ignored | `RichTextBox` was drawing its own opaque background over the parent `Border`'s rounded corners. Fixed: `Background="Transparent"`. |
| 4 | Spurious "Saved: Default tunnel" logged on Default Action tab open | `DefaultTunnelBox` population was outside the `_loading` guard, causing `SelectionChanged` to fire during refresh. Wrapped in `_loading = true/false`. |
| 5 | Wizard language picker showing `[NL] Nederlands` prefix | `DisplayMemberPath = "Name"` bypassed the `DataTemplate`, falling back to `ToString()` which includes the `[XX]` code prefix. Fixed by removing `DisplayMemberPath` and applying the same `DataTemplate` as Settings. |
| 6 | XAML `MC3000` — `--` inside XML comment | Python string replacement doubled a comment marker during a build pass. Fixed. |
| 7 | Multiple `CS0103` / `MC3000` build errors | Orphaned controls left in XAML after partial removal passes. All resolved. |
