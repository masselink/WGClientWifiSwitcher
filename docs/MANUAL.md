# MasselGUARD — User Manual

**Version 2.9.0**

---

## Contents

1. [Introduction](#1-introduction)
2. [Installation and run modes](#2-installation-and-run-modes)
3. [First run — Setup wizard](#3-first-run--setup-wizard)
4. [The main window](#4-the-main-window)
5. [Managing tunnels](#5-managing-tunnels)
6. [Connecting and disconnecting](#6-connecting-and-disconnecting)
7. [Default action and open network protection](#7-default-action-and-open-network-protection)
8. [WiFi Rules](#8-wifi-rules)
9. [Settings — General](#9-settings--general)
10. [Settings — Tunnel Groups](#10-settings--tunnel-groups)
11. [Settings — Appearance](#11-settings--appearance)
12. [Settings — Default Action](#12-settings--default-action)
13. [Settings — WiFi Rules](#13-settings--wifi-rules)
14. [Settings — Advanced](#14-settings--advanced)
15. [Pre/post scripts](#15-prepost-scripts)
16. [Quick Connect](#16-quick-connect)
17. [Import / Export settings](#17-import--export-settings)
18. [The activity log](#18-the-activity-log)
19. [System tray](#19-system-tray)
20. [Themes](#20-themes)
21. [Multiple languages](#21-multiple-languages)
22. [Frequently asked questions](#22-frequently-asked-questions)

---

## 1. Introduction

MasselGUARD is a WireGuard automation tool for Windows. It monitors your WiFi connection and activates the right WireGuard tunnel automatically based on rules you define. It also works as a manual WireGuard front-end when automation is not wanted.

---

## 2. Installation and run modes

**Requirements:** Windows 10 or 11 (64-bit), [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0), Administrator rights.

### Run modes

| Mode | Meaning |
|---|---|
| **Standalone** | Running as a portable exe; no installed version detected |
| **Managed (Portable)** | An installed version exists; this is a separate copy |
| **Managed** | Running from the installed location — shown **green** in footer |

### Installing

1. Settings → Advanced → Installation → **Install**
2. Choose a parent folder
3. Optionally enable **Start with Windows** (Scheduled Task, no UAC on relaunch)
4. MasselGUARD relaunches from the installed location

### Managed Portable — version prompt

When running as Managed Portable and the version **differs** from the installed copy (including build number differences), a themed prompt offers to overwrite. The message adapts: "is newer than" or "differs from" depending on direction.

---

## 3. First run — Setup wizard

Runs on first launch and when starting a newer version than the last wizard run.

**Step 0 — Welcome:** Upgrade banner (on version change). Install-choice card (first-run Standalone). Import settings card.

**Step 1 — Language & Appearance:** Language picker and theme selector. Changes apply immediately as a preview.

**Step 2 — Operating mode:** Standalone / Companion / Mixed.

**Step 3 — Automation:** Disable WiFi rules toggle. Show WiFi rules panel toggle.

**Step 4 — Done:** Version label and Check for updates.

---

## 4. The main window

### Tunnel list (left panel)

Columns: **Tunnel** | **Type** | **Status** | **Rules** | **Action**

- **Colour strip** — 4 px strip per row showing the tunnel's group colour
- **Badges** — `⚡` (default action) and `🔓` (open network protection) after the tunnel name
- **Status** — uptime for active tunnels: `● Connected  2h 34m`
- **Rules** — count of WiFi rules referencing this tunnel; click to highlight matching rules in the WiFi Rules panel. Rebuilds immediately on rule add/edit/delete
- **Action** — Connect / Disconnect, centred

**Toolbar buttons:** + Add | Edit | Import | **Defaults** | Delete

### Defaults button

Opens a themed popup centred on the main window with:
- **⚡ Default action tunnel** — dropdown + "— clear —"
- **🔓 Open network protection** — dropdown + "— clear —"

Saves immediately on clicking Save. Badges and footer update in place.

### WiFi Rules panel (optional, left panel)

Columns: **Name** (widest) | **SSID** | **Action** | **Hits** | **Tunnel**

- **Hits** — how many times each rule has triggered (persisted, accent colour when > 0)
- Rows are **draggable** to reorder
- Highlighted rows (from clicking a tunnel's rule count): 2 px Accent left border + tinted background
- Add / Edit / Delete buttons; Delete uses themed confirmation dialog
- Collapses when hidden or Manual Mode active

### Activity Log (right panel)

Column header: **Time** | **Event**. Entry count badge. Export Log button.

### Footer bar

Left: run mode (green when Managed) | Centre: ⚡ default tunnel + 🔓 open protection | Right: Administrator status

---

## 5. Managing tunnels

### Tunnel groups

Manage in **Settings → Tunnel Groups**. Each group row has:
- 👁 Hide/show the group tab
- ⭐ Set as startup default
- Name field (editable inline)
- Colour picker (hex or theme key)
- ↑ ↓ ✕ reorder and delete

**Drag tunnels into groups:** Drag any tunnel row and drop it onto a group tab button to reassign it immediately.

**Toggles:** Always hide tunnel count | Hide empty groups

### Drag-to-reorder tunnels

Drag tunnel rows to reorder within the current group. A 2 px Accent drop-line shows the insertion point.

---

## 6. Connecting and disconnecting

Click Connect / Disconnect per tunnel. Automation does this automatically on network changes.

Active tunnels show elapsed uptime: `< 1 min` → `Xs`, `< 1 h` → `Xm YYs`, `< 1 day` → `Xh YYm`, `≥ 1 day` → `Xd YYh YYm`.

---

## 7. Default action and open network protection

### Default action

What happens when connecting to WiFi with no matching rule. Options: Do nothing / Disconnect all / Activate a tunnel. The assigned tunnel shows `⚡` in the list and `⚡ TunnelName` in the footer.

### Open network protection

Activates automatically on **passwordless** WiFi before any SSID rule. The assigned tunnel shows `🔓` in the list and `🔓 TunnelName` in the footer.

### Setting them

- **Defaults button** in the tunnel toolbar (popup centred on window) — immediate save
- **Settings → Default Action** — saves on Settings Save
- **Edit tunnel dialog** footer bar toggles — saves on dialog Save

---

## 8. WiFi Rules

### Rule dialog fields

| Field | Description |
|---|---|
| **Name** | Display name — auto-generates from SSID + tunnel as you type. Stops auto-generating once manually edited. |
| **SSID** | Network name — case-sensitive. "Use Current" fills from active WiFi. |
| **Tunnel** | Leave empty to disconnect all tunnels on this network. |

### Hits counter

The **Hits** column shows how many times each rule has triggered. Persisted in config — survives restarts. Shown in Accent colour when > 0, muted when 0.

### Drag to reorder

Drag rows in the WiFi Rules panel on the main window to change evaluation order. Rules evaluate top to bottom; first match wins.

### Tunnel list updates

Adding, editing, or deleting a rule immediately refreshes both the WiFi Rules panel **and** the Rules column count in the tunnel list.

---

## 9. Settings — General

- Language picker
- App mode: Standalone / Companion / Mixed

All changes deferred until Save. Cancel discards everything.

---

## 10. Settings — Tunnel Groups

- **Always hide tunnel count** — removes the `n` number from all group tab buttons
- **Hide empty groups** — suppresses tabs with no tunnels in the current filter
- Group list — add/edit/reorder/delete groups, set colour and visibility
- Add group: type name + click + Add

Changes deferred until Save.

---

## 11. Settings — Appearance

- Dark theme / Light theme pickers — live preview; cancel reverts
- Auto theme — follows Windows dark/light preference
- **Background notifications** toggle — show WPF toast when a tunnel auto-switches
- **Notification duration** — 3 / 5 / 10 / 15 / 30 seconds

### Toast notification format

```
╔══════════════════════════════════════════╗
║ 🛡 MasselGUARD  ·  WiFi Rule Matched  ✕ ║
╟──────────────────────────────────────────╢
║  1.MasselinkVPN-Split-AG                 ║
║  Rule: MasselNET → activate              ║
╚══════════════════════════════════════════╝
```

- App name from `Theme.AppName` — custom themes override it
- Strip colour: Accent (rule), Success/green (open network), Warning (default action)
- Slides in from bottom-right; auto-dismisses after configured duration

---

## 12. Settings — Default Action

- Default action picker: None / Disconnect / Activate tunnel
- Open network protection tunnel picker
- Same as Defaults button popup but deferred to Settings Save

---

## 13. Settings — WiFi Rules

**Layout (top to bottom):**
1. Rules list — Add / Edit / Delete buttons
2. **Disable WiFi rules** toggle — pauses all automation
3. **Hide WiFi rules on main window** toggle
4. **Show Rules column** in tunnel list toggle

Rules changes save via the main Save button. The separate "Save rules" button has been removed.

---

## 14. Settings — Advanced

**Order:**
1. Import / Export settings
2. Log level (Normal / Extended)
3. Installation — run mode, Install/Uninstall button
4. Start with Windows — Scheduled Task at `RunLevel=Highest`
5. WireGuard client — open the WireGuard for Windows app
6. Orphaned services — scan and clean up
7. DLL status
8. Update checker — frequency, check now

### Extended log on Save

When extended logging is active, only **changed** fields are logged after Save:
```
[DBG] [Settings] Mode                       Standalone  →  Companion
[DBG] [Settings] Rule added:   MasselNET    → disconnect
[DBG] [Settings] Group added:  Work
```

---

## 15. Pre/post scripts

Four hook points per tunnel: Before connect / After connect / Before disconnect / After disconnect. `.bat` or `.ps1` files. Logged in Extended mode.

---

## 16. Quick Connect

Connect a `.conf` or `.conf.dpapi` file without importing. Appears as `⚡ filename` at top of tunnel list. Disappears after disconnecting.

---

## 17. Import / Export settings

**Export** — saves to `.masselguard` (JSON). Tunnel configs not included.

**Import** — replaces settings. Version mismatch shows a themed warning. Available in Settings → Advanced and on wizard Step 0.

---

## 18. The activity log

Column header: **Time** | **Event** — consistent with Tunnels and WiFi Rules panels.

Extended mode adds: `[DBG]` entries, disconnect duration, settings change details.

Entry count badge in header. Export Log saves to `.txt`.

---

## 19. System tray

**Icon states:**
- Filled green shield — one or more tunnels active
- Outline grey shield — no active tunnels

**Tray menu:**
- 🪟 Show Window
- 🛡 Tunnels (submenu) — shield is green when active
- ⬛→ Exit

Right-click → menu. Double-click → show main window. × in main window → minimise to tray (tunnels keep running).

---

## 20. Themes

Six built-in: Default Dark/Light, Grey Dark/Light, High Contrast Dark/Light. Theme switcher in title bar. Custom themes in `theme\` folder. Settings → Appearance shows live preview — Cancel reverts.

Custom themes can set `AppName` to change the name shown in toast notifications.

---

## 21. Multiple languages

English, Dutch, German, French, Spanish. Change in Settings → General. Add a language: copy `lang\en.json`, translate, add `_code` and `_language` keys.

---

## 22. Frequently asked questions

**Rules fire twice when switching networks.**
Fixed — debounce re-fire guard and `ApplyWifiState` duplicate guard prevent double execution.

**Groups tab in Settings does nothing when clicked.**
Fixed — the tab now correctly shows the Tunnel Groups page with group management controls.

**My tunnel group picker is empty when editing a tunnel.**
Fixed — the dialog now receives the group list directly from the live config.

**Settings Save shows too many changed fields.**
Fixed — `_draft` now correctly snapshots the live config on Settings open.

**Rules column doesn't update after adding a rule.**
Fixed — `_vm.RebuildTunnelList()` now called after every rule add/edit/delete.

**Can I reorder WiFi rules?**
Yes — drag rows in the WiFi Rules panel on the main window.

**Where is the WiFi rules Save button?**
Removed. All settings (including rules) save when the main Settings Save button is pressed.

**Can I drag a tunnel into a different group?**
Yes — drag the tunnel row and drop it onto the target group tab.

**Can I run without a UAC prompt?**
Yes — enable Start with Windows in Settings → Advanced after installing. Subsequent launches relaunch via the Scheduled Task automatically.

**What does the Hits column show?**
How many times each WiFi rule has triggered since it was created. Persisted across restarts.
