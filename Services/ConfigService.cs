using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using MasselGUARD.Models;

namespace MasselGUARD.Services
{
    /// <summary>
    /// Handles all AppConfig persistence: load, save, import, export.
    /// No UI references. Raises ConfigChanged when the config is replaced.
    /// </summary>
    public class ConfigService
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MasselGUARD", "config.json");

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
        };

        public AppConfig Config { get; private set; } = new();

        public event Action? ConfigChanged;

        /// <summary>
        /// True if no config.json existed when Load() was called
        /// (first run — show wizard).
        /// </summary>
        public bool IsFirstRun { get; private set; }

        // ── Load ─────────────────────────────────────────────────────────────
        public void Load()
        {
            if (!File.Exists(ConfigPath))
            {
                IsFirstRun = true;
                Config     = new AppConfig();
                return;
            }
            try
            {
                Config = JsonSerializer.Deserialize<AppConfig>(
                    File.ReadAllText(ConfigPath), JsonOpts) ?? new AppConfig();
            }
            catch
            {
                Config = new AppConfig();
            }
        }

        // ── Save ─────────────────────────────────────────────────────────────
        public void Save()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath,
                JsonSerializer.Serialize(Config, JsonOpts));
            ConfigChanged?.Invoke();
        }

        // ── Export ───────────────────────────────────────────────────────────
        /// <summary>
        /// Writes a .masselguard export file containing automation settings,
        /// rules, groups, and UI preferences. Never includes tunnel configs
        /// or DPAPI material.
        /// </summary>
        public void Export(string path, string appVersion)
        {
            var cfg = Config;
            var export = new
            {
                ExportVersion    = 1,
                AppVersion       = appVersion,
                ExportedAt       = DateTime.UtcNow,
                Rules            = cfg.Rules,
                TunnelGroups     = cfg.TunnelGroups,
                DefaultAction    = cfg.DefaultAction,
                DefaultTunnel    = cfg.DefaultTunnel,
                OpenWifiTunnel   = cfg.OpenWifiTunnel,
                ManualMode       = cfg.ManualMode,
                Mode             = cfg.Mode.ToString(),
                Language         = cfg.Language,
                ActiveTheme      = cfg.ActiveTheme,
                ActiveDarkTheme  = cfg.ActiveDarkTheme,
                ActiveLightTheme = cfg.ActiveLightTheme,
                AutoTheme        = cfg.AutoTheme,
                LogLevelSetting  = cfg.LogLevelSetting,
                ShowTrayPopupOnSwitch = cfg.ShowTrayPopupOnSwitch,
            };
            File.WriteAllText(path,
                JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }));
        }

        // ── Import ───────────────────────────────────────────────────────────
        /// <summary>
        /// Reads a .masselguard file and merges compatible fields into Config.
        /// Returns the AppVersion string found in the file (empty if absent).
        /// Unknown fields are silently ignored for forward compatibility.
        /// </summary>
        public string Import(string path)
        {
            var json = File.ReadAllText(path);
            using var doc  = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var fileVersion = root.TryGetProperty("AppVersion", out var v)
                ? v.GetString() ?? "" : "";

            void Str(string key, Action<string> set)
            {
                if (root.TryGetProperty(key, out var el) &&
                    el.ValueKind == JsonValueKind.String)
                    set(el.GetString() ?? "");
            }
            void Bool(string key, Action<bool> set)
            {
                if (root.TryGetProperty(key, out var el) &&
                    (el.ValueKind == JsonValueKind.True ||
                     el.ValueKind == JsonValueKind.False))
                    set(el.GetBoolean());
            }

            Str ("DefaultAction",     v => Config.DefaultAction    = v);
            Str ("DefaultTunnel",     v => Config.DefaultTunnel    = v);
            Str ("OpenWifiTunnel",    v => Config.OpenWifiTunnel   = v);
            Str ("Language",          v => Config.Language         = v);
            Str ("ActiveTheme",       v => Config.ActiveTheme      = v);
            Str ("ActiveDarkTheme",   v => Config.ActiveDarkTheme  = v);
            Str ("ActiveLightTheme",  v => Config.ActiveLightTheme = v);
            Str ("LogLevelSetting",   v => Config.LogLevelSetting  = v);
            Bool("ManualMode",        v => Config.ManualMode       = v);
            Bool("AutoTheme",         v => Config.AutoTheme        = v);
            Bool("ShowTrayPopupOnSwitch", v => Config.ShowTrayPopupOnSwitch = v);

            if (root.TryGetProperty("Rules", out var rulesEl))
            {
                var rules = JsonSerializer.Deserialize<
                    System.Collections.Generic.List<TunnelRule>>(
                    rulesEl.GetRawText(), opts);
                if (rules != null) Config.Rules = rules;
            }
            if (root.TryGetProperty("TunnelGroups", out var grpEl))
            {
                var groups = JsonSerializer.Deserialize<
                    System.Collections.Generic.List<TunnelGroup>>(
                    grpEl.GetRawText(), opts);
                if (groups != null) Config.TunnelGroups = groups;
            }

            Save();
            return fileVersion;
        }
    }
}
