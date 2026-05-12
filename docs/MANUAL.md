# MasselGUARD — User Manual

**Version 2.5.0**

---

## Contents

1. [Introduction](#1-introduction)
2. [Installation](#2-installation)
3. [First run — Setup wizard](#3-first-run--setup-wizard)
4. [The main window](#4-the-main-window)
5. [Managing tunnels](#5-managing-tunnels)
6. [Connecting and disconnecting](#6-connecting-and-disconnecting)
7. [WiFi Rules](#7-wifi-rules)
8. [LAN Rules](#8-lan-rules)
9. [Default Action](#9-default-action)
10. [Open Network Protection](#10-open-network-protection)
11. [Settings — General](#11-settings--general)
12. [Settings — Appearance](#12-settings--appearance)
13. [Settings — Advanced](#13-settings--advanced)
14. [Pre/post scripts](#14-prepost-scripts)
15. [Quick Connect](#15-quick-connect)
16. [The activity log](#16-the-activity-log)
17. [System tray](#17-system-tray)
18. [Themes](#18-themes)
19. [Multiple languages](#19-multiple-languages)
20. [Frequently asked questions](#20-frequently-asked-questions)

---

## 1. Introduction

MasselGUARD is a WireGuard automation tool for Windows. It monitors your network connections and activates the right WireGuard tunnel automatically based on rules you define.

**What it can do:**

- Automatically start a tunnel when you connect to a specific WiFi network
- Automatically start a tunnel when you plug in an Ethernet cable on a specific network
- Protect you on open (passwordless) hotspots by forcing a tunnel before anything else connects
- Run scripts before or after a tunnel connects or disconnects
- Work entirely without the WireGuard app (Standalone mode) or alongside it (Companion mode)

**What it does not do:**

- Create WireGuard server configurations — you need a WireGuard server or VPN provider for that
- Work without Administrator rights — it creates and manages Windows services

---

## 2. Installation

### Requirements

- Windows 10 or Windows 11 (64-bit)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)
- Administrator account

For **Standalone mode**: `tunnel.dll` and `wireguard.dll` must be present next to `MasselGUARD.exe` (included in the release zip).

For **Companion mode**: [WireGuard for Windows](https://wireguard.com/install) must be installed.

### First-time setup

1. Extract the zip to any folder (e.g. `Downloads\MasselGUARD`)
2. Double-click `MasselGUARD.exe`
3. Windows will show a UAC prompt — click **Yes** (Administrator rights are required)
4. The setup wizard opens automatically

### Installing to Program Files

MasselGUARD has a built-in installer. After the wizard completes:

1. Open **Settings → Advanced**
2. Click **Install**
3. Choose your install folder (default: `C:\Program Files\MasselGUARD`)
4. Optionally enable **Start with Windows**

> **Upgrading:** when you install a newer version over an existing one, all files including themes and language files are overwritten. Your config (`%APPDATA%\MasselGUARD\config.json`) and encrypted tunnel configs are not touched.

---

## 3. First run — Setup wizard

The wizard runs automatically the first time MasselGUARD starts. You can re-run it at any time from **Settings → General → Run Wizard**.

### Step 1 — Language

Choose your preferred language. You can change this later in Settings → General.

### Step 2 — Operating mode

Choose how MasselGUARD manages tunnels:

| Mode | When to choose |
|---|---|
| **Standalone** | You want MasselGUARD to manage everything. No WireGuard app needed. You will import `.conf` files directly. |
| **Companion** | You already use WireGuard for Windows and want MasselGUARD to automate it. |
| **Mixed** | You want both: some tunnels managed by MasselGUARD, others by WireGuard for Windows. |

### Step 3 — Automation

Choose whether MasselGUARD should react to network changes automatically, or whether you prefer to start and stop tunnels manually.

- **Automatic** — MasselGUARD watches your WiFi and Ethernet connections and applies your rules
- **Manual** — MasselGUARD acts as a launcher only; you connect and disconnect yourself

You can toggle manual mode at any time in **Settings → WiFi Rules → Manual mode**.

### Finishing the wizard

Click **Finish** to apply your choices. MasselGUARD will open the main window.

---

## 4. The main window

The main window has two panels side by side.

### Left panel — Tunnels

The top of the left panel shows the **TUNNELS** header with a count badge. Below it is a tab strip for tunnel groups (All, your named groups, Uncategorized). The list below shows your tunnels with their type, status, and a Connect/Disconnect button.

At the bottom: **+ Add**, **Edit**, **Import**, and **Delete/Unlink/Remove** buttons.

### Right panel — Activity Log

Shows a timestamped log of everything MasselGUARD does: network changes, tunnel connections, rule matches, errors. The **Export Log** button at the bottom saves the current log to a `.txt` file.

### Status bar

Along the bottom of the window: the current WiFi network (or "No WiFi"), the active tunnel name, your app mode, and whether MasselGUARD is running as Administrator.

### Title bar

The **⚙** button opens Settings. The **⚡** button cycles through dark → light → auto theme. The **×** button minimises to the system tray (it does not close the application).

---

## 5. Managing tunnels

### Adding a tunnel — Standalone mode

1. Click **+ Add**
2. Give the tunnel a name
3. Either:
   - **Fields tab**: fill in the WireGuard config fields (interface address, private key, peer public key, endpoint, allowed IPs)
   - **Raw tab**: paste a complete `.conf` file
4. Optionally assign a **Group** and add **Notes**
5. Click **Save**

The config is encrypted with DPAPI and stored in `%APPDATA%\MasselGUARD\tunnels\`.

### Adding a tunnel — Companion mode

1. Click **Import**
2. Choose **Link to WireGuard profile**
3. Select an existing WireGuard profile — MasselGUARD links to it without copying the config

### Editing a tunnel

Select a tunnel in the list and click **Edit**.

- **Local tunnels**: opens the full editor with Fields, Raw, and Scripts tabs
- **WireGuard-linked tunnels**: opens a metadata dialog for group, notes, and scripts

### Deleting / removing a tunnel

Select a tunnel and click the action button at the bottom right. The label changes depending on context:

| Label | Meaning |
|---|---|
| **Delete** | Removes the tunnel and deletes the encrypted config file |
| **Remove** | Removes the tunnel record (config file already missing) |
| **Unlink** | Removes the WireGuard profile link (the WireGuard profile itself is not deleted) |

### Organising into groups

- Go to **Settings → General → Tunnel Groups** to create, rename, and reorder groups
- When editing a tunnel, use the **Group** dropdown to assign it
- Use the tab strip above the tunnel list to filter by group

---

## 6. Connecting and disconnecting

### Manual connect/disconnect

Click the **Connect** or **Disconnect** button next to any tunnel in the list.

### Automatic connect/disconnect

If automation is enabled, MasselGUARD connects and disconnects tunnels based on your rules whenever your network changes. You do not need to do anything.

### Connecting multiple tunnels

MasselGUARD's rule system works with one active tunnel at a time — it stops the current tunnel before starting the new one. If you need multiple simultaneous tunnels, connect them manually.

### What happens when a tunnel is active

- The tunnel name appears in the status bar
- The tray icon shows a green badge with the number of active tunnels
- The Connect button in the list becomes Disconnect

---

## 7. WiFi Rules

WiFi rules tell MasselGUARD what to do when you connect to a specific WiFi network.

### Opening the rules list

Go to **Settings → WiFi Rules**.

### Adding a rule

1. Click **+ Add Rule**
2. Enter the **WiFi network name (SSID)** exactly as it appears when you connect — it is case-sensitive
3. Choose a **Tunnel** from the dropdown, or leave it empty to disconnect all tunnels on that network
4. Click **Save**

### Example rules

| SSID | Tunnel | Effect |
|---|---|---|
| `HomeWifi` | `home-vpn` | Activates `home-vpn` at home |
| `OfficeWifi` | `office-vpn` | Activates `office-vpn` at the office |
| `CafeGuest` | *(empty)* | Disconnects all tunnels at the café |

### Manual mode

The toggle at the top of the WiFi Rules page enables or disables all WiFi-based automation. When manual mode is on, the rules list and default action are greyed out — MasselGUARD will not react to network changes.

### Open Network Protection

At the bottom of the WiFi Rules page. Select a tunnel to activate automatically whenever you connect to an open (passwordless) WiFi network. This fires before any SSID rule is checked, so you are always protected on public hotspots.

Set the dropdown to **— none —** to disable open network protection.

---

## 8. LAN Rules

LAN rules tell MasselGUARD what to do when you connect an Ethernet cable to a specific network.

### Why LAN rules are useful

Different Ethernet networks often mean different security contexts — home, office, client site, hotel room. A LAN rule lets you automatically activate the right tunnel for each one.

### How MasselGUARD identifies an Ethernet network

MasselGUARD reads two identifiers from each connected Ethernet adapter:

- **Adapter name** — the name shown in Windows Network Connections (e.g. `"Ethernet"`, `"LAN 2"`). You can rename adapters in Windows.
- **DNS suffix** — the domain assigned by your network's DHCP server (e.g. `"corp.example.com"`, `"office.local"`). This is the most reliable way to distinguish office vs home networks.

Both are checked. If either matches your rule's filter, the rule fires. Matching is partial and case-insensitive: `"office"` matches `"office.local"`.

### Finding your adapter name and DNS suffix

1. Open a Command Prompt
2. Run `ipconfig /all`
3. Find your Ethernet adapter
4. Note the **Description**, **Connection-specific DNS Suffix**, and the adapter header name

Or enable **Extended** logging in Settings → Advanced — MasselGUARD logs all detected adapter details at `[DBG]` level every time an Ethernet change is detected.

### Adding a LAN rule

1. Go to **Settings → LAN Rules**
2. Click **+ Add Rule**
3. Enter the adapter name or DNS suffix (or part of it)
4. Choose a tunnel, or leave empty to disconnect on that network
5. Click **Save**

### Example LAN rules

| Filter | Tunnel | Matches |
|---|---|---|
| `corp.example.com` | `office-vpn` | Any adapter with that DNS suffix |
| `Ethernet 2` | `home-vpn` | The adapter named exactly "Ethernet 2" |
| `office` | `office-vpn` | Any adapter with "office" anywhere in the name or DNS suffix |

---

## 9. Default Action

The Default Action determines what happens when you connect to a network that has no matching rule.

Go to **Settings → Default Action**.

### WiFi default action

Applies when you connect to a WiFi network that is not in your WiFi rules list.

| Option | Effect |
|---|---|
| **Do nothing** | MasselGUARD ignores the network change |
| **Disconnect all** | Disconnects any active tunnel |
| **Activate tunnel** | Connects the selected tunnel |

### LAN default action

Applies when an Ethernet adapter connects and no LAN rule matches.

Same three options as WiFi. Useful if you want a specific tunnel active on all wired connections unless a more specific rule overrides it.

---

## 10. Open Network Protection

Open network protection is configured at the bottom of **Settings → WiFi Rules**.

When you connect to a WiFi network with no password (open network), MasselGUARD detects this using the Windows WLAN API and immediately activates the chosen protection tunnel — before evaluating any SSID rule.

**Why this matters:** on a public hotspot your device connects and begins sending data before you might think to activate a VPN. Open network protection closes this window.

**Configuration:**
- Select the tunnel to activate on open networks from the dropdown
- Set to **— none —** to disable

---

## 11. Settings — General

### Language

Select your preferred display language. The interface updates immediately.

### App mode

Change between Standalone, Companion, and Mixed. A restart is not required, but mode changes affect which tunnels are visible and how they are managed.

### Tunnel groups

- Click **New group** to add a named category
- Click the name field and type to rename
- Use **↑** and **↓** to reorder groups
- Click **✕** to delete a group (tunnels in that group move to Uncategorized)

### Run wizard

Re-runs the setup wizard. Useful when setting up on a new machine or changing mode.

---

## 12. Settings — Appearance

### Theme

**Dark theme** and **Light theme** dropdowns select the theme used in each mode. These are independent — you can have Default Dark as your dark theme and High Contrast Light as your light theme.

**Auto theme** follows the Windows system dark/light preference. MasselGUARD polls the system setting every 5 seconds and switches automatically.

The theme toggle button in the title bar (🌙 / ☀ / ⚡) cycles between: dark → light → auto → dark.

### Background notifications

When enabled, a branded popup appears near the system tray whenever MasselGUARD automatically switches tunnels. The popup shows the tunnel name and the reason (rule match, default action, or open network protection).

---

## 13. Settings — Advanced

### Installation

Shows the current install status. Use **Install** to copy MasselGUARD to Program Files and optionally add it to Windows startup. Use **Uninstall** to remove it.

### DLL status

Shows whether `tunnel.dll` and `wireguard.dll` are present and their file size. A size of around 1.3 MB for `wireguard.dll` and 3.6 MB for `tunnel.dll` is expected. If either is missing, Standalone mode will not work.

### WireGuard client

Links to the official WireGuard for Windows application. Used in Companion and Mixed modes.

### Orphaned tunnel services

If MasselGUARD is closed while a tunnel is running (e.g. after a crash), the Windows service (`WireGuardTunnel$<name>`) may remain registered. This section lists those orphaned services and lets you remove them individually or all at once.

### Log level

- **Normal** — shows OK results and warnings only
- **Extended** — shows all events including detailed `[DBG]` diagnostic entries (adapter detection, tunnel config fields, connect timing)

---

## 14. Pre/post scripts

Scripts let you run custom code at four points around a tunnel connection.

| Hook | When it runs |
|---|---|
| **Before connect** | Immediately before the tunnel starts |
| **After connect** | After the tunnel is confirmed active |
| **Before disconnect** | Immediately before the tunnel stops |
| **After disconnect** | After the tunnel has stopped |

### Setting up a script

1. Select a tunnel and click **Edit**
2. For local tunnels: go to the **Scripts** tab
3. For WireGuard-linked tunnels: scripts are at the bottom of the metadata dialog
4. Click **Browse…** to select a `.bat` or `.ps1` file

### Embedding a script (local tunnels only)

Click **Embed** next to a script slot. An editor appears where you can type or paste the script content directly. The script is stored inside the tunnel config — no external file needed. If a file path is already set when you click Embed, the file content is loaded into the editor automatically.

### What scripts can do

- Mount or unmount network drives
- Update DNS settings
- Send notifications
- Log tunnel events to an external system
- Run any Windows command

### Script output

The script's exit code, stdout, and stderr are shown in the Activity Log when log level is set to Extended. A non-zero exit code is logged as a warning but does not abort the connect or disconnect operation.

---

## 15. Quick Connect

Quick Connect lets you connect a tunnel from a `.conf` file without permanently importing it.

### Using Quick Connect

1. Click **Quick Connect** in the status bar (or from the tray menu)
2. Select a `.conf` or `.conf.dpapi` file
3. The tunnel connects immediately

The tunnel appears at the top of the tunnel list as **⚡ filename**. Click **Disconnect** to stop it.

### What Quick Connect does not do

- The config is not saved to `%APPDATA%\MasselGUARD\tunnels\`
- The tunnel disappears from the list after disconnecting
- Quick Connect tunnels are not available for rules or default actions

---

## 16. The activity log

The Activity Log is the right panel of the main window. It records everything MasselGUARD does.

### Reading the log

Each line starts with a timestamp (`HH:mm:ss`). Detail lines are shown with a `↳` prefix under their parent entry.

**Colours:**

| Colour | Meaning |
|---|---|
| Blue (accent) | Informational — network changes, mode changes |
| Green | Success — tunnel connected, config saved |
| Orange/red | Warning — connection failed, script error |
| Muted | Debug `[DBG]` entries (Extended log level only) |

### Log levels

Set in **Settings → Advanced → Log level**.

- **Normal** — only green (success) and red (warning) messages
- **Extended** — everything, including blue info messages and muted `[DBG]` diagnostic details

### Exporting

Click **Export Log** at the bottom of the right panel to save the current log as a `.txt` file.

---

## 17. System tray

MasselGUARD runs in the system tray. Click the shield icon to show or hide the main window.

### Tray icon

| Icon state | Meaning |
|---|---|
| Plain shield | No tunnels active |
| Green badge with number | That many tunnels are active |

### Tray menu

Right-click the tray icon for:

- A list of all tunnels with their current status — click to connect/disconnect
- **Quick Connect**
- **Settings**
- **Exit** — closes MasselGUARD completely

### Closing the main window

The **×** button in the top-right corner of the main window minimises MasselGUARD to the tray — it does not stop the application or any active tunnels. To exit completely, right-click the tray icon and choose **Exit**.

---

## 18. Themes

MasselGUARD ships with six built-in themes:

| Theme | Style |
|---|---|
| Default Dark | Dark blue-grey, rounded corners |
| Default Light | Light blue-grey, rounded corners |
| Grey Dark | Dark neutral grey, sharp corners |
| Grey Light | Light neutral grey, sharp corners |
| High Contrast Dark | Pure black background, white text, yellow accent |
| High Contrast Light | Pure white background, black text, blue accent |

### Switching themes

- Use the **🌙 / ☀ / ⚡** toggle in the title bar to cycle quickly
- Go to **Settings → Appearance** to choose specific dark and light themes independently

### Custom themes

You can create your own theme by adding a folder to the `theme\` directory next to `MasselGUARD.exe`. See `theme\THEME_INFO.md` for the full list of customisable properties, including colours, corner radius, window opacity, custom logos, and tray menu colours.

Themes apply instantly — no restart needed. Just switch away and back to reload a modified theme.

---

## 19. Multiple languages

MasselGUARD supports English, Dutch, German, French, and Spanish out of the box. Change the language in **Settings → General**.

### Adding a language

1. Copy `lang\en.json` to `lang\<code>.json` (e.g. `lang\pt.json` for Portuguese)
2. Translate the values (leave the keys unchanged)
3. Add `"_code": "pt"` and `"_language": "Português"` at the top
4. Restart MasselGUARD — the new language appears in the picker

---

## 20. Frequently asked questions

**MasselGUARD says it is already running.**
Another instance is open. Check the system tray — the shield icon should be there. Click it to show the window. To start fresh, right-click the tray icon and choose Exit, then re-launch.

**My tunnel connects but immediately shows as disconnected.**
The wireguard-NT service process exits after loading the kernel driver — this is normal. The tunnel is still active. Windows Event Viewer may show a false termination error; ignore it.

**My WiFi rule is not firing.**
Check that the SSID in the rule exactly matches your network name — it is case-sensitive. Enable Extended logging to see what SSID MasselGUARD is detecting. Also check that Manual mode is off (Settings → WiFi Rules).

**My LAN rule is not firing.**
Enable Extended logging. When you connect an Ethernet cable, the log will show detected adapter details (`[DBG] LAN adapter: name='...' dns='...'`). Compare the values shown to your rule filter.

**I cannot import a `.conf` file.**
Make sure the file is a valid WireGuard config with at minimum an `[Interface]` section containing `PrivateKey` and `Address`. Configs exported from commercial VPN providers sometimes use non-standard formats.

**My pre/post script is not running.**
Check that the file exists and the path has no typos. Check the Activity Log with Extended logging — script execution, output, and exit codes are logged as `[Script]` entries.

**The application crashes on startup.**
Most likely the `.NET 10 Desktop Runtime` is not installed. Download it from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0).

**I want to move MasselGUARD to another computer.**
Install MasselGUARD on the new machine. Copy `%APPDATA%\MasselGUARD\config.json` to the same path on the new machine. Note that DPAPI-encrypted tunnel configs (`.conf.dpapi`) cannot be transferred — they are tied to your Windows account on the original machine. Re-import the `.conf` files on the new machine.

**Can I run MasselGUARD without Administrator rights?**
No. Creating and managing Windows services (which is how WireGuard tunnels work) requires Administrator privileges.
