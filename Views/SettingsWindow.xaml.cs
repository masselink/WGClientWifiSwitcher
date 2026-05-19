using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MasselGUARD.Infrastructure;
using MasselGUARD.Models;
using MasselGUARD.Services;
using MasselGUARD.ViewModels;

namespace MasselGUARD.Views
{
    public partial class SettingsWindow : Window
    {
        private readonly MainWindow       _main;
        private readonly SettingsViewModel _vm;
        private string _activeTab = "General";
        private bool   _loading   = true;
        private bool   _themeSwitching = false;

        private string _originalTheme = "";   // to revert if cancelled
        private Models.AppConfig _draft = new(); // staged copy — only written to config on Save

        public SettingsWindow(MainWindow main)
        {
            _main = main;
            _vm   = new SettingsViewModel(main.ConfigSvc, main.LogSvc);

            // Wire ViewModel dialog requests
            _vm.AddRuleRequested    += OnAddRule;
            _vm.EditRuleRequested   += OnEditRule;
            _vm.ExportRequested     += OnExportSettings;
            _vm.ImportRequested     += OnImportSettings;
            _vm.ModeChanged         += _ => { _main.ApplyManualMode(); RefreshCurrentTab(); };
            _vm.LogLevelChanged     += v => main.LogSvc.IsExtended = v == "extended";

            InitializeComponent();
            DataContext = _vm;

            Loaded += (_, _) =>
            {
                _loading = false;
                // Create a deep copy of the LIVE config — all edits go here until Save is pressed
                _draft = _main.ConfigSvc.Config.DeepClone();
                // Remember current theme so we can revert if cancelled
                _originalTheme = ThemeManager.Instance.CurrentThemeName ?? "";
                ShowTab("General");
                RefreshUpdateState();
            };
        }

        // ── Tab routing ───────────────────────────────────────────────────────
        private void TabBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;
            string tab = btn.Name switch
            {
                "TabBtnGroups"        => "Groups",
                "TabBtnAppearance"    => "Appearance",
                "TabBtnDefaultAction" => "DefaultAction",
                "TabBtnRules"         => "Rules",
                "TabBtnAdvanced"      => "Advanced",
                "TabBtnAbout"         => "About",
                _                     => "General",
            };
            ShowTab(tab);
        }

        public void ShowTab(string tab)
        {
            _activeTab = tab;

            PageGeneral.Visibility       = tab == "General"       ? Visibility.Visible : Visibility.Collapsed;
            PageGroups.Visibility        = tab == "Groups"        ? Visibility.Visible : Visibility.Collapsed;
            PageAppearance.Visibility    = tab == "Appearance"    ? Visibility.Visible : Visibility.Collapsed;
            PageDefaultAction.Visibility = tab == "DefaultAction" ? Visibility.Visible : Visibility.Collapsed;
            PageRules.Visibility         = tab == "Rules"         ? Visibility.Visible : Visibility.Collapsed;
            PageAdvanced.Visibility      = tab == "Advanced"      ? Visibility.Visible : Visibility.Collapsed;
            PageAbout.Visibility         = tab == "About"         ? Visibility.Visible : Visibility.Collapsed;

            TabBtnGeneral.Tag       = tab == "General"       ? "Active" : null;
            TabBtnGroups.Tag        = tab == "Groups"        ? "Active" : null;
            TabBtnAppearance.Tag    = tab == "Appearance"    ? "Active" : null;
            TabBtnDefaultAction.Tag = tab == "DefaultAction" ? "Active" : null;
            TabBtnRules.Tag         = tab == "Rules"         ? "Active" : null;
            TabBtnAdvanced.Tag      = tab == "Advanced"      ? "Active" : null;
            TabBtnAbout.Tag         = tab == "About"         ? "Active" : null;

            if (tab == "Advanced")   { RefreshInstallState(); RefreshDllStatus(); RefreshWireGuardSection(); ScanOrphans(); PopulateLogLevelPicker(); SyncStartWithWindows(); }
            if (tab == "About")      RefreshUpdateState();
            if (tab == "Appearance") PopulateThemePicker();
            if (tab == "General")    RefreshGroupList();
            if (tab == "Groups")     RefreshGroupList();
            if (tab == "Rules" || tab == "DefaultAction") RefreshAutomationControls();
        }

        private void RefreshCurrentTab() => ShowTab(_activeTab);

        // ── General tab ───────────────────────────────────────────────────────
        private void LanguagePicker_SelectionChanged(object sender,
            SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (LanguagePicker.SelectedItem is LangItem item)
            {
                Lang.Instance.Load(item.Code);
                _vm.Language = item.Code;
            }
        }

        private void AppMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            if (ModeStandalone?.IsChecked == true) _vm.Mode = AppMode.Standalone;
            else if (ModeCompanion?.IsChecked == true) _vm.Mode = AppMode.Companion;
            else _vm.Mode = AppMode.Mixed;
        }

        private void RefreshGroupList()
        {
            if (_loading) return;

            // ── General tab controls ──────────────────────────────────────────
            if (LanguagePicker != null && LanguagePicker.Items.Count == 0)
            {
                foreach (var (code, name) in Lang.AvailableLanguages())
                    LanguagePicker.Items.Add(new LangItem(code, name));
            }
            if (LanguagePicker != null)
                LanguagePicker.SelectedItem = LanguagePicker.Items
                    .OfType<LangItem>()
                    .FirstOrDefault(i => string.Equals(i.Code,
                        _draft.Language, StringComparison.OrdinalIgnoreCase));

            // Sync app mode radios (General tab)
            _loading = true;
            var mode = _draft.Mode;
            if (ModeStandalone != null) ModeStandalone.IsChecked = mode == AppMode.Standalone;
            if (ModeCompanion  != null) ModeCompanion.IsChecked  = mode == AppMode.Companion;
            if (ModeMixed      != null) ModeMixed.IsChecked      = mode == AppMode.Mixed;
            _loading = false;

            // ── Groups tab controls ───────────────────────────────────────────
            _loading = true;
            if (HideTunnelCountToggle  != null) HideTunnelCountToggle.IsChecked  = _draft.AlwaysHideTunnelCount;
            if (HideEmptyGroupsToggle  != null) HideEmptyGroupsToggle.IsChecked  = _draft.HideEmptyGroups;
            _loading = false;

            // ── Group list (used by both General and Groups tab) ──────────────
            GroupListPanel.Items.Clear();
            var groups = _draft.TunnelGroups;
            var hidden = _draft.HiddenTabs;

            // Theme colour presets shown in the colour picker
            var themePresets = new[]
            {
                ("", "—"),               // no colour / transparent
                ("Accent",   "Accent"),
                ("Success",  "Success"),
                ("Danger",   "Danger"),
                ("Surface",  "Surface"),
                ("#1E3A5F",  "#1E3A5F"),
                ("#2D4A1E",  "#2D4A1E"),
                ("#4A1E1E",  "#4A1E1E"),
                ("#2D1E4A",  "#2D1E4A"),
                ("#1E4A4A",  "#1E4A4A"),
            };

            // ── Helper: build one group row ────────────────────────────────────
            void AddGroupRow(string displayName, string groupKey,
                bool canDelete, bool canRename, bool canReorder,
                string currentColor, int? listIdx)
            {
                bool isHidden = hidden.Contains(groupKey);
                var row = new System.Windows.Controls.WrapPanel
                {
                    Orientation = System.Windows.Controls.Orientation.Horizontal,
                    Margin      = new Thickness(0, 0, 0, 4),
                };

                // Eye toggle (hide/show)
                var eyeBtn = new Button
                {
                    Content   = isHidden ? "👁‍🗨" : "👁",
                    Style     = (Style)FindResource("FlatBtn"),
                    FontSize  = 11,
                    Padding   = new Thickness(4, 2, 4, 2),
                    Margin    = new Thickness(0, 0, 4, 0),
                    ToolTip   = isHidden ? "Show tab" : "Hide tab",
                    Opacity   = isHidden ? 0.4 : 1.0,
                };
                eyeBtn.Click += (_, _) =>
                {
                    if (hidden.Contains(groupKey)) hidden.Remove(groupKey);
                    else hidden.Add(groupKey);
                    RefreshGroupList();
                };
                row.Children.Add(eyeBtn);

                // Default star — marks which group opens on startup
                var isDefault = _draft.DefaultGroup == groupKey;
                var starBtn   = new Button
                {
                    Content   = isDefault ? "⭐" : "☆",
                    Style     = (Style)FindResource("FlatBtn"),
                    FontSize  = 11,
                    Padding   = new Thickness(4, 2, 4, 2),
                    Margin    = new Thickness(0, 0, 4, 0),
                    ToolTip   = isDefault ? "This group opens on startup" : "Set as default on startup",
                    Opacity   = isDefault ? 1.0 : 0.5,
                };
                starBtn.Click += (_, _) =>
                {
                    _draft.DefaultGroup =
                        _draft.DefaultGroup == groupKey ? "" : groupKey;
                    RefreshGroupList();
                };
                row.Children.Add(starBtn);

                // Name field (read-only for All/Uncategorized)
                var nameBox = new TextBox
                {
                    Text            = displayName,
                    Width           = 160,
                    IsReadOnly      = !canRename,
                    FontFamily      = (System.Windows.Media.FontFamily)FindResource("Theme.FontFamily"),
                    FontSize        = 11,
                    Padding         = new Thickness(6, 3, 6, 3),
                    Background      = (System.Windows.Media.Brush)FindResource("CardBg"),
                    Foreground      = (System.Windows.Media.Brush)FindResource(canRename ? "TextPrimary" : "TextMuted"),
                    BorderBrush     = (System.Windows.Media.Brush)FindResource("BorderColor"),
                    BorderThickness = new Thickness(1),
                    Opacity         = canRename ? 1.0 : 0.7,
                };
                if (canRename && listIdx.HasValue)
                {
                    int idx = listIdx.Value;
                    nameBox.LostFocus += (_, _) =>
                    {
                        var text = nameBox.Text.Trim();
                        if (!string.IsNullOrEmpty(text) && text != groups[idx].Name)
                        {
                            // Update hidden key if renamed
                            if (hidden.Contains(groups[idx].Name))
                            {
                                hidden.Remove(groups[idx].Name);
                                hidden.Add(text);
                            }
                            groups[idx].Name = text;
                        }
                    };
                }
                row.Children.Add(nameBox);

                // Colour picker
                var colPicker = new System.Windows.Controls.ComboBox
                {
                    Width   = 80,
                    Margin  = new Thickness(4, 0, 0, 0),
                    Padding = new Thickness(4, 2, 4, 2),
                    FontSize= 10,
                    Background      = (System.Windows.Media.Brush)FindResource("CardBg"),
                    Foreground      = (System.Windows.Media.Brush)FindResource("TextPrimary"),
                    BorderBrush     = (System.Windows.Media.Brush)FindResource("BorderColor"),
                };
                foreach (var (hex, label) in themePresets)
                {
                    var item = new System.Windows.Controls.ComboBoxItem
                    {
                        Tag     = hex,
                        Content = label,
                    };
                    if (!string.IsNullOrEmpty(hex))
                    {
                        try
                        {
                            System.Windows.Media.Brush swatch;
                            if (hex.StartsWith("#"))
                                swatch = new System.Windows.Media.SolidColorBrush(
                                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(hex));
                            else
                                swatch = (System.Windows.Media.Brush)FindResource(hex);

                            item.Background = swatch;
                            // Set label to white or black based on luminance
                            item.Foreground = GetContrastBrush(swatch);
                        }
                        catch { }
                    }
                    colPicker.Items.Add(item);
                }
                // Pre-select current colour
                foreach (System.Windows.Controls.ComboBoxItem ci in colPicker.Items)
                    if ((string)ci.Tag == (currentColor ?? "")) { colPicker.SelectedItem = ci; break; }
                if (colPicker.SelectedItem == null) colPicker.SelectedIndex = 0;

                if (listIdx.HasValue)
                {
                    int idx = listIdx.Value;
                    colPicker.SelectionChanged += (_, _) =>
                    {
                        if (colPicker.SelectedItem is System.Windows.Controls.ComboBoxItem ci)
                        {
                            groups[idx].Color = (string)ci.Tag;
                            _main._vm.RebuildTunnelList();
                        }
                    };
                }
                row.Children.Add(colPicker);

                // Reorder buttons (only for custom groups)
                if (canReorder && listIdx.HasValue)
                {
                    int idx = listIdx.Value;
                    void MakeBtn(string label, Action click)
                    {
                        var b = new Button
                        {
                            Content = label, Style=(Style)FindResource("FlatBtn"),
                            FontSize=11, Padding=new Thickness(5,2,5,2), Margin=new Thickness(4,0,0,0),
                        };
                        b.Click += (_,_) => click();
                        row.Children.Add(b);
                    }
                    if (idx > 0)
                        MakeBtn("↑", () => { var t=groups[idx]; groups.RemoveAt(idx); groups.Insert(idx-1,t); RefreshGroupList(); });
                    if (idx < groups.Count - 1)
                        MakeBtn("↓", () => { var t=groups[idx]; groups.RemoveAt(idx); groups.Insert(idx+1,t); RefreshGroupList(); });
                    if (canDelete)
                        MakeBtn("✕", () => { groups.RemoveAt(idx); hidden.Remove(groupKey); RefreshGroupList(); });
                }

                GroupListPanel.Items.Add(row);
            }

            // ── Custom groups ─────────────────────────────────────────────────
            for (int i = 0; i < groups.Count; i++)
                AddGroupRow(groups[i].Name, groups[i].Name,
                    canDelete: true, canRename: true, canReorder: true,
                    currentColor: groups[i].Color, listIdx: i);

            // ── Uncategorized (built-in, non-deletable) ───────────────────────
            AddGroupRow(Lang.T("TabUncategorized"), "Uncategorized",
                canDelete: false, canRename: false, canReorder: false,
                currentColor: "", listIdx: null);
        }

        private void ShowRulesColumn_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loading) return;
            _draft.ShowTunnelRulesColumn =
                ShowRulesColumnToggle?.IsChecked == true;
        }

        private void HideWifiRules_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loading) return;
            _draft.ShowWifiRulesOnMainWindow =
                !(HideWifiRulesToggle?.IsChecked == true);
            _main.RefreshWifiRulesPanel();
        }

        private void HideEmptyGroups_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loading) return;
            _draft.HideEmptyGroups =
                HideEmptyGroupsToggle?.IsChecked == true;
        }

        private void HideTunnelCount_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loading) return;
            _draft.AlwaysHideTunnelCount =
                HideTunnelCountToggle?.IsChecked == true;   // rebuilds tabs + calls UpdateHiddenCountBadge
        }

        private void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            var name = NewGroupNameBox?.Text.Trim() ?? "";
            if (string.IsNullOrEmpty(name)) name = Lang.T("DefaultGroupName");
            if (NewGroupNameBox != null) NewGroupNameBox.Text = "";
            var newGroup = new TunnelGroup(name);
            // Add to both config AND the VM's staged collection so DoSave preserves it
            _draft.TunnelGroups.Add(newGroup);
            _vm.TunnelGroups.Add(newGroup);
            RefreshGroupList();
        }

        private static System.Windows.Media.Brush GetContrastBrush(
            System.Windows.Media.Brush bg)
        {
            // Compute luminance of the brush's dominant colour
            if (bg is System.Windows.Media.SolidColorBrush scb)
            {
                var c = scb.Color;
                double lum = 0.2126 * (c.R / 255.0)
                           + 0.7152 * (c.G / 255.0)
                           + 0.0722 * (c.B / 255.0);
                return lum > 0.45
                    ? System.Windows.Media.Brushes.Black
                    : System.Windows.Media.Brushes.White;
            }
            return System.Windows.Media.Brushes.White;
        }

        // ── Appearance tab ────────────────────────────────────────────────────
        private void PopulateThemePicker()
        {
            _themeSwitching = true;
            var allThemes = ThemeManager.AvailableThemes();

            string GetType(string folder) {
                try {
                    var def = System.Text.Json.JsonSerializer.Deserialize<ThemeDefinition>(
                        System.IO.File.ReadAllText(System.IO.Path.Combine(
                            AppContext.BaseDirectory, "theme", folder, "theme.json")),
                        new System.Text.Json.JsonSerializerOptions{PropertyNameCaseInsensitive=true});
                    return def?.Type ?? "dark";
                } catch { return "dark"; }
            }

            var dark  = allThemes.Where(f => GetType(f) == "dark").ToList();
            var light = allThemes.Where(f => GetType(f) == "light").ToList();

            DarkThemePicker.Items.Clear();
            foreach (var f in dark)
                DarkThemePicker.Items.Add(new ThemePickerItem(f, ThemeManager.GetThemeDisplayName(f)));
            DarkThemePicker.SelectedItem = DarkThemePicker.Items
                .OfType<ThemePickerItem>()
                .FirstOrDefault(i => i.FolderName == _draft.ActiveDarkTheme);

            LightThemePicker.Items.Clear();
            foreach (var f in light)
                LightThemePicker.Items.Add(new ThemePickerItem(f, ThemeManager.GetThemeDisplayName(f)));
            LightThemePicker.SelectedItem = LightThemePicker.Items
                .OfType<ThemePickerItem>()
                .FirstOrDefault(i => i.FolderName == _draft.ActiveLightTheme);

            AutoThemeToggle.IsChecked = _draft.AutoTheme;
            if (TrayPopupToggle != null)
            {
                _loading = true;
                TrayPopupToggle.IsChecked = _draft.ShowTrayPopupOnSwitch;
                _loading = false;
            }

            // Notification duration picker
            if (NotifDurationPicker != null)
            {
                _loading = true;
                NotifDurationPicker.Items.Clear();
                foreach (var s in new[] { 3, 5, 10, 15, 30 })
                    NotifDurationPicker.Items.Add(new System.Windows.Controls.ComboBoxItem
                        { Content = $"{s}s", Tag = s });
                int cur = _draft.NotificationDurationSeconds;
                NotifDurationPicker.SelectedItem = NotifDurationPicker.Items
                    .OfType<System.Windows.Controls.ComboBoxItem>()
                    .FirstOrDefault(i => (int)i.Tag == cur)
                    ?? NotifDurationPicker.Items[1];
                _loading = false;
            }
            _themeSwitching = false;
        }

        private void DarkThemePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || _themeSwitching) return;
            if (DarkThemePicker.SelectedItem is ThemePickerItem item)
                _vm.ActiveDarkTheme = item.FolderName;
        }

        private void LightThemePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading || _themeSwitching) return;
            if (LightThemePicker.SelectedItem is ThemePickerItem item)
                _vm.ActiveLightTheme = item.FolderName;
        }

        private void AutoTheme_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading || _themeSwitching) return;
            _vm.AutoTheme = AutoThemeToggle.IsChecked == true;
        }

        private void NotifDuration_Changed(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (NotifDurationPicker?.SelectedItem is System.Windows.Controls.ComboBoxItem ci)
                _draft.NotificationDurationSeconds = (int)ci.Tag;
        }

        private void TrayPopup_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            _vm.ShowTrayPopup = TrayPopupToggle.IsChecked == true;
        }

        // ── Default Action tab ────────────────────────────────────────────────
        private void RefreshAutomationControls()
        {
            var cfg = _draft;
            var tunnels = _main.GetTunnelNames();

            // Sync _vm.Rules from live config (may have changed via main window rule buttons)
            // and also sync _draft.Rules to match
            _loading = true;
            _vm.Rules.Clear();
            foreach (var r in _main.ConfigSvc.Config.Rules)
                _vm.Rules.Add(r);
            _draft.Rules = _main.ConfigSvc.Config.Rules.ToList();
            _loading = false;

            _loading = true;

            // Rules visibility toggles (moved here from General)
            if (HideWifiRulesToggle != null)
                HideWifiRulesToggle.IsChecked = !cfg.ShowWifiRulesOnMainWindow;
            if (ShowRulesColumnToggle != null)
                ShowRulesColumnToggle.IsChecked = cfg.ShowTunnelRulesColumn;

            // Rules
            if (RulesListView != null)
                RulesListView.ItemsSource = cfg.Rules;

            // Manual mode
            if (ManualModeToggle != null)
                ManualModeToggle.IsChecked = cfg.ManualMode;
            if (AutomationPanel != null)
            {
                AutomationPanel.IsEnabled = !cfg.ManualMode;
                AutomationPanel.Opacity   = cfg.ManualMode ? 0.4 : 1.0;
            }

            // WiFi default action radios
            if (ActionNone     != null) ActionNone.IsChecked     = cfg.DefaultAction == "none" || string.IsNullOrEmpty(cfg.DefaultAction);
            if (ActionDiscon   != null) ActionDiscon.IsChecked   = cfg.DefaultAction == "disconnect";
            if (ActionActivate != null) ActionActivate.IsChecked = cfg.DefaultAction == "activate";

            if (DefaultTunnelBox != null)
            {
                DefaultTunnelBox.Items.Clear();
                foreach (var t in tunnels) DefaultTunnelBox.Items.Add(t);
                DefaultTunnelBox.Text = cfg.DefaultTunnel ?? "";
            }

            // Open WiFi
            if (OpenWifiTunnelBox != null)
            {
                OpenWifiTunnelBox.Items.Clear();
                OpenWifiTunnelBox.Items.Add(Lang.T("OpenWifiNone"));
                foreach (var t in tunnels) OpenWifiTunnelBox.Items.Add(t);
                var match = tunnels.FirstOrDefault(t =>
                    string.Equals(t, cfg.OpenWifiTunnel, StringComparison.OrdinalIgnoreCase));
                OpenWifiTunnelBox.SelectedItem = (object?)match ?? Lang.T("OpenWifiNone");
            }

            _loading = false;
        }

        private void DefaultAction_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            if      (ActionNone?.IsChecked   == true) _vm.DefaultAction = "none";
            else if (ActionDiscon?.IsChecked == true) _vm.DefaultAction = "disconnect";
            else                                      _vm.DefaultAction = "activate";
        }

        private void DefaultTunnelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (DefaultTunnelBox?.SelectedItem is string t)
            {
                _draft.DefaultTunnel = t;
                _draft.DefaultAction = "activate";
                _loading = true;
                ActionActivate.IsChecked = true;
                _loading = false;
                _main.SaveConfigPublic($"Default tunnel: {t}");
            }
        }

        private void DefaultTunnelBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            var text = DefaultTunnelBox?.Text.Trim() ?? "";
            if (!string.IsNullOrEmpty(text) && text != _draft.DefaultTunnel)
            {
                _draft.DefaultTunnel = text;
                _draft.DefaultAction = "activate";
                _loading = true;
                ActionActivate.IsChecked = true;
                _loading = false;
                _main.SaveConfigPublic($"Default tunnel: {text}");
            }
        }

        private void OpenWifiTunnel_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            var none = Lang.T("OpenWifiNone");
            var sel  = OpenWifiTunnelBox?.SelectedItem as string;
            _vm.OpenWifiTunnel = sel == none ? "" : sel ?? "";
        }

        // ── WiFi Rules tab ────────────────────────────────────────────────────
        private void ManualMode_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            bool on = ManualModeToggle?.IsChecked == true;
            _vm.DisableWifiRules = on;
            // Dim the WiFi rules section when disabled
            if (AutomationPanel != null) AutomationPanel.Opacity = on ? 0.4 : 1.0;
            if (AutomationPanel != null) AutomationPanel.IsEnabled = !on;
        }

        private void OnAddRule()
        {
            var dlg = new RuleDialog(_main.WifiSvc.CurrentSsid,
                tunnels: _main.GetTunnelNames()) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            var rule = new TunnelRule { Ssid = dlg.ResultSsid, Tunnel = dlg.ResultTunnel };
            _vm.AddRule(rule);
            RefreshAutomationControls();
        }

        private void OnEditRule(TunnelRule rule)
        {
            var dlg = new RuleDialog(_main.WifiSvc.CurrentSsid,
                existingName:   rule.Name,
                existingSsid:   rule.Ssid,
                existingTunnel: rule.Tunnel,
                tunnels:        _main.GetTunnelNames()) { Owner = this };
            if (dlg.ShowDialog() != true) return;
            rule.Ssid   = dlg.ResultSsid;
            rule.Tunnel = dlg.ResultTunnel;
            _vm.UpdateRule(rule);
            RefreshAutomationControls();
        }

        private void AddRule_Click(object sender, RoutedEventArgs e)    => _vm.AddRuleCommand.Execute(null);
        private void EditRule_Click(object sender, RoutedEventArgs e)   => _vm.EditRuleCommand.Execute(null);
        private void DeleteRule_Click(object sender, RoutedEventArgs e) => _vm.DeleteRuleCommand.Execute(null);

        private void SaveRules_Click(object sender, RoutedEventArgs e)  => _vm.SaveRulesCommand.Execute(null);

        private void RulesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _vm.SelectedRule = RulesListView?.SelectedItem as TunnelRule;
            if (EditBtn   != null) EditBtn.IsEnabled   = _vm.SelectedRule != null;
            if (DeleteBtn != null) DeleteBtn.IsEnabled = _vm.SelectedRule != null;
        }

        // ── Advanced tab ──────────────────────────────────────────────────────
        private void PopulateLogLevelPicker()
        {
            if (LogLevelPicker == null || LogLevelPicker.Items.Count > 0) return;
            _loading = true;
            LogLevelPicker.Items.Add(Lang.T("LogLevelNormal"));
            LogLevelPicker.Items.Add(Lang.T("LogLevelExtended"));
            LogLevelPicker.SelectedIndex = _draft.LogLevelSetting == "extended" ? 1 : 0;
            _loading = false;
        }

        private void LogLevel_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_loading) return;
            if (LogLevelPicker?.SelectedIndex == 1)
                _vm.LogLevel = "extended";
            else
                _vm.LogLevel = "normal";
        }

        private void RefreshInstallState()
        {
            var mode      = _main.AppRunMode;
            var installed = _main.GetInstalledPath();

            string statusText;
            string? statusPath = null;
            string btnLabel;
            System.Windows.Media.Brush statusColor;

            switch (mode)
            {
                case MainWindow.AppRunModeKind.Managed:
                    statusText  = Lang.T("InstallStatusManaged");
                    statusPath  = installed;
                    btnLabel    = Lang.T("BtnUninstall");
                    statusColor = (System.Windows.Media.Brush)FindResource("Success");
                    break;
                case MainWindow.AppRunModeKind.ManagedPortable:
                    statusText  = Lang.T("InstallStatusPortable", installed ?? "");
                    statusPath  = installed;
                    btnLabel    = Lang.T("BtnInstall");
                    statusColor = (System.Windows.Media.Brush)FindResource("Accent");
                    break;
                default: // Standalone
                    statusText  = Lang.T("InstallStatusNotInstalled");
                    btnLabel    = Lang.T("BtnInstall");
                    statusColor = (System.Windows.Media.Brush)FindResource("TextMuted");
                    break;
            }

            if (InstallStatusLabel != null)
            {
                InstallStatusLabel.Text       = statusText;
                InstallStatusLabel.Foreground = statusColor;
            }
            if (InstallPathLabel  != null)
                InstallPathLabel.Text = statusPath ?? "";
            if (FindName("InstallBtn") is System.Windows.Controls.Button btn)
                btn.Content = btnLabel;

            if (SuppressUpdatePromptToggle != null)
            {
                _loading = true;
                SuppressUpdatePromptToggle.IsChecked =
                    _draft.SuppressPortableUpdatePrompt;
                _loading = false;
            }

            // Sync frequency pills
            SyncFrequencyPills();
        }

        private void RefreshDllStatus()
        {
            var baseDir    = AppContext.BaseDirectory;
            var tunnelPath = System.IO.Path.Combine(baseDir, "tunnel.dll");
            var wgPath     = System.IO.Path.Combine(baseDir, "wireguard.dll");
            SetLabel("TunnelDllLabel", System.IO.File.Exists(tunnelPath)
                ? $"tunnel.dll  ({new System.IO.FileInfo(tunnelPath).Length / 1024} KB)"
                : Lang.T("SettingsDllMissing"));
            SetLabel("WgDllLabel", System.IO.File.Exists(wgPath)
                ? $"wireguard.dll  ({new System.IO.FileInfo(wgPath).Length / 1024} KB)"
                : Lang.T("SettingsDllMissing"));
        }

        private void RefreshWireGuardSection()
        {
            SetLabel("WgInstallLabel",
                MainWindow.DetectWireGuardInstallDir() ?? Lang.T("SettingsWgNotFound"));
        }

        private void ScanOrphans()
        {
            var orphans = _main.GetOrphanedServices();
            if (FindName("OrphansList") is System.Windows.Controls.ListBox lb)
            {
                lb.Items.Clear();
                foreach (var o in orphans) lb.Items.Add(o);
            }
            SetLabel("OrphanCountLabel", orphans.Count == 0
                ? Lang.T("SettingsNoOrphans") : $"{orphans.Count} found");
        }

        private void SetLabel(string name, string text)
        {
            if (FindName(name) is System.Windows.Controls.TextBlock tb) tb.Text = text;
        }

        private void RemoveOrphan_Click(object sender, RoutedEventArgs e)
        {
            if (FindName("OrphansList") is System.Windows.Controls.ListBox lb &&
                lb.SelectedItem is MainWindow.OrphanedService o)
            {
                _main.RemoveOrphan(o);
                ScanOrphans();
            }
        }

        private void RemoveAllOrphans_Click(object sender, RoutedEventArgs e)
        {
            foreach (var o in _main.GetOrphanedServices())
                _main.RemoveOrphan(o);
            ScanOrphans();
        }

        private void Install_Click(object sender, RoutedEventArgs e)
        {
            // Delegate to existing install logic via main window
        }

        private void OpenWireGuard_Click(object sender, RoutedEventArgs e)      => _main.OpenWireGuardGui();

        private void ExportSettings_Click(object sender, RoutedEventArgs e)     => _vm.ExportCommand.Execute(null);
        private void ImportSettings_Click(object sender, RoutedEventArgs e)     => _vm.ImportCommand.Execute(null);

        private void OnExportSettings()
        {
            var warn = MessageBox.Show(Lang.T("SettingsExportWarning"),
                Lang.T("SettingsExportTitle"),
                MessageBoxButton.OKCancel, MessageBoxImage.Information);
            if (warn != MessageBoxResult.OK) return;

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title      = Lang.T("SettingsExportTitle"),
                Filter     = "MasselGUARD settings (*.masselguard)|*.masselguard|JSON (*.json)|*.json",
                FileName   = $"MasselGUARD-settings-{DateTime.Now:yyyyMMdd}",
                DefaultExt = ".masselguard",
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                _main.ConfigSvc.Export(dlg.FileName, UpdateChecker.CurrentVersionString);
                _main.LogInfoPublic(Lang.T("SettingsExportSuccess", dlg.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("SettingsExportError", ex.Message),
                    Lang.T("SettingsExportTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnImportSettings()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title  = Lang.T("SettingsImportTitle"),
                Filter = "MasselGUARD settings (*.masselguard;*.json)|*.masselguard;*.json",
            };
            if (dlg.ShowDialog() != true) return;

            try
            {
                string fileVersion = _main.ConfigSvc.Import(dlg.FileName);
                string current     = UpdateChecker.CurrentVersionString;

                if (!string.IsNullOrEmpty(fileVersion) && fileVersion != current)
                {
                    int cmp = string.Compare(fileVersion, current, StringComparison.OrdinalIgnoreCase);
                    var key = cmp > 0 ? "SettingsImportVersionNewer" : "SettingsImportVersionWarning";
                    var proceed = MessageBox.Show(
                        Lang.T(key, fileVersion, current),
                        Lang.T("SettingsImportTitle"),
                        MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (proceed != MessageBoxResult.Yes)
                    {
                        // Re-load the original config
                        _main.ConfigSvc.Load();
                        return;
                    }
                }

                _vm.LoadFromConfig();
                ShowTab(_activeTab);
                MessageBox.Show(Lang.T("SettingsImportSuccess"),
                    Lang.T("SettingsImportTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(Lang.T("SettingsImportError", ex.Message),
                    Lang.T("SettingsImportTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ── About tab ─────────────────────────────────────────────────────────
        private void RefreshUpdateState()
        {
            var cfg     = _main.ConfigSvc.Config;
            var current = UpdateChecker.CurrentVersionString;

            // Version label — large
            if (VersionLabel != null)
                VersionLabel.Text = $"v{current}";

            // Last checked label
            if (LastCheckedLabel != null)
                LastCheckedLabel.Text = cfg.LastUpdateCheck == default
                    ? Lang.T("SettingsUpdateNeverChecked")
                    : Lang.T("SettingsUpdateLastChecked",
                        cfg.LastUpdateCheck.ToLocalTime().ToString("g"));

            // Status badge: colour + text
            bool hasLatest  = !string.IsNullOrEmpty(cfg.LatestKnownVersion);
            bool upToDate   = hasLatest && string.Compare(current, cfg.LatestKnownVersion,
                                  StringComparison.OrdinalIgnoreCase) >= 0;
            bool updateAvail = hasLatest && !upToDate;

            if (UpdateStatusBadge != null && UpdateStatusLabel != null)
            {
                if (!hasLatest)
                {
                    UpdateStatusBadge.Background = (System.Windows.Media.Brush)
                        Application.Current.Resources["Surface"];
                    UpdateStatusBadge.BorderBrush = (System.Windows.Media.Brush)
                        Application.Current.Resources["BorderColor"];
                    UpdateStatusBadge.BorderThickness = new Thickness(1);
                    UpdateStatusLabel.Foreground = (System.Windows.Media.Brush)
                        Application.Current.Resources["TextMuted"];
                    UpdateStatusLabel.Text = Lang.T("SettingsUpdateUnknown");
                }
                else if (upToDate)
                {
                    UpdateStatusBadge.Background = (System.Windows.Media.Brush)
                        Application.Current.Resources["Success"];
                    UpdateStatusBadge.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    UpdateStatusBadge.BorderThickness = new Thickness(0);
                    UpdateStatusLabel.Foreground = (System.Windows.Media.Brush)
                        Application.Current.Resources["WindowBg"];
                    UpdateStatusLabel.Text = Lang.T("SettingsUpdateCurrent", current);
                }
                else
                {
                    UpdateStatusBadge.Background = (System.Windows.Media.Brush)
                        Application.Current.Resources["Accent"];
                    UpdateStatusBadge.BorderBrush = System.Windows.Media.Brushes.Transparent;
                    UpdateStatusBadge.BorderThickness = new Thickness(0);
                    UpdateStatusLabel.Foreground = (System.Windows.Media.Brush)
                        Application.Current.Resources["WindowBg"];
                    UpdateStatusLabel.Text = Lang.T("SettingsUpdateAvailable", cfg.LatestKnownVersion!);
                }
            }

            // Check Now button label
            if (CheckUpdateBtn != null)
                CheckUpdateBtn.Content = Lang.T("BtnCheckUpdate");

            // Download button
            if (DoUpdateBtn != null)
            {
                DoUpdateBtn.Visibility = updateAvail ? Visibility.Visible : Visibility.Collapsed;
                if (updateAvail)
                    DoUpdateBtn.Content = Lang.T("BtnDownloadUpdate", cfg.LatestKnownVersion!);
            }

            // Frequency pills
            SyncFrequencyPills();
        }

        private void SyncFrequencyPills()
        {
            var freq = _draft.UpdateCheckFrequency ?? "weekly";
            if (FreqOnStart != null) FreqOnStart.IsChecked = freq == "onstart";
            if (FreqDaily   != null) FreqDaily.IsChecked   = freq == "daily";
            if (FreqWeekly  != null) FreqWeekly.IsChecked  = freq == "weekly";
            if (FreqManual  != null) FreqManual.IsChecked  = freq == "manual";
        }

        private void UpdateFreq_Changed(object sender, RoutedEventArgs e)
        {
            if (_loading) return;
            if (sender is RadioButton rb && rb.Tag is string tag)
                _draft.UpdateCheckFrequency = tag;
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (CheckUpdateBtn != null)
            {
                CheckUpdateBtn.IsEnabled = false;
                CheckUpdateBtn.Content   = Lang.T("SettingsUpdateChecking");
            }
            var latest = await UpdateChecker.CheckNowAsync(
                _main.ConfigSvc.Config, _main.ConfigSvc.Save);
            if (CheckUpdateBtn != null)
            {
                CheckUpdateBtn.IsEnabled = true;
                CheckUpdateBtn.Content   = Lang.T("BtnCheckUpdate");
            }
            RefreshUpdateState();
            if (latest != null && UpdateChecker.IsNewerVersion(latest.TagName))
            {
                var current = UpdateChecker.CurrentVersionString;
                if (_main.ShowThemedYesNo(
                    Lang.T("UpdateAvailableMsg", latest.TagName, current),
                    Lang.T("UpdateAvailableTitle")))
                    _main.RunUpdate();
            }
        }

        private void RunWizard_Click(object sender, RoutedEventArgs e)
        {
            var wiz = new WizardWindow(_main) { Owner = this };
            wiz.ShowDialog();
            _vm.LoadFromConfig();
            ShowTab("General");
        }

        // ── Window chrome ─────────────────────────────────────────────────────
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e) => Close();
        // ── Handlers required by XAML ─────────────────────────────────────────
        private void Mode_Changed(object sender, System.Windows.RoutedEventArgs e)
            => AppMode_Changed(sender, e);

        private void SuppressUpdatePrompt_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loading) return;
            _draft.SuppressPortableUpdatePrompt =
                SuppressUpdatePromptToggle?.IsChecked == true;
        }

        private void SyncStartWithWindows()
        {
            if (StartWithWindowsToggle == null) return;
            _loading = true;
            StartWithWindowsToggle.IsChecked = _main.GetStartWithWindows();
            _loading = false;
        }

        private void StartWithWindows_Changed(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_loading) return;
            _main.SetStartWithWindows(StartWithWindowsToggle?.IsChecked == true);
        }

        private void InstallBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _main.RunInstallPublic();
            RefreshInstallState();
        }

        private void ScanOrphans_Click(object sender, System.Windows.RoutedEventArgs e)
            => ScanOrphans();

        private void DoUpdate_Click(object sender, System.Windows.RoutedEventArgs e)
            => _main.RunUpdate();

        private void GithubLink_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://github.com/masselink/MasselGUARD") { UseShellExecute = true }); }
            catch { }
        }

        private bool _savedSuccessfully = false;

        private void SaveBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            _savedSuccessfully = true;

            // Snapshot BEFORE committing so diff is accurate
            var before = _main.ConfigSvc.Config.DeepClone();

            // Commit draft fields that bypass _vm (groups, hidden tabs, toggles)
            _main.ConfigSvc.Config.TunnelGroups        = _draft.TunnelGroups;
            _main.ConfigSvc.Config.HiddenTabs          = _draft.HiddenTabs;
            _main.ConfigSvc.Config.DefaultGroup        = _draft.DefaultGroup;
            _main.ConfigSvc.Config.AlwaysHideTunnelCount = _draft.AlwaysHideTunnelCount;
            _main.ConfigSvc.Config.HideEmptyGroups     = _draft.HideEmptyGroups;
            _main.ConfigSvc.Config.ShowWifiRulesOnMainWindow = _draft.ShowWifiRulesOnMainWindow;
            _main.ConfigSvc.Config.ShowTunnelRulesColumn = _draft.ShowTunnelRulesColumn;
            _main.ConfigSvc.Config.NotificationDurationSeconds = _draft.NotificationDurationSeconds;

            // Sync _vm.TunnelGroups from _draft so DoSave doesn't overwrite with stale data
            _vm.TunnelGroups.Clear();
            foreach (var g in _draft.TunnelGroups) _vm.TunnelGroups.Add(g);

            _vm.DoSave();

            // Log only changed fields now that config is fully committed
            if (_main.LogSvc.IsExtended)
                LogChangedSettings(before, _main.ConfigSvc.Config);
            // Apply side effects immediately
            Lang.Instance.Load(_vm.Language);
            _main.LogSvc.IsExtended = _vm.LogLevel == "extended";
            _main.ApplyManualMode();
            _main._vm.RebuildTunnelList();
            _main.RebuildTunnelGroupsPublic();
            _main._vm.NotifyRulesColumnChanged();
            _main.RefreshWifiRulesPanel();
            Close();
        }

        private void LogChangedSettings(Models.AppConfig before, Models.AppConfig after)
        {
            var log = _main.LogSvc;

            void Check(string label, object? a, object? b)
            {
                string sa = (a?.ToString() ?? "—").ToLowerInvariant();
                string sb = (b?.ToString() ?? "—").ToLowerInvariant();
                if (sa != sb)
                    log.Debug($"[Settings] {label,-26} {a}  →  {b}");
            }

            Check("Language",              before.Language,              after.Language);
            Check("Mode",                  before.Mode,                  after.Mode);
            Check("Manual mode",           before.ManualMode,            after.ManualMode);
            Check("Default action",        before.DefaultAction,         after.DefaultAction);
            Check("Default tunnel",        before.DefaultTunnel,         after.DefaultTunnel);
            Check("Open network",          before.OpenWifiTunnel,        after.OpenWifiTunnel);
            Check("Theme (dark)",          before.ActiveDarkTheme,       after.ActiveDarkTheme);
            Check("Theme (light)",         before.ActiveLightTheme,      after.ActiveLightTheme);
            Check("Auto theme",            before.AutoTheme,             after.AutoTheme);
            Check("Log level",             before.LogLevelSetting,       after.LogLevelSetting);
            Check("Tray popups",           before.ShowTrayPopupOnSwitch, after.ShowTrayPopupOnSwitch);
            Check("Notif duration (s)",    before.NotificationDurationSeconds, after.NotificationDurationSeconds);
            Check("Show rules column",     before.ShowTunnelRulesColumn, after.ShowTunnelRulesColumn);
            Check("WiFi rules panel",      before.ShowWifiRulesOnMainWindow, after.ShowWifiRulesOnMainWindow);
            Check("Hide empty groups",     before.HideEmptyGroups,       after.HideEmptyGroups);
            Check("Hide count badge",      before.AlwaysHideTunnelCount, after.AlwaysHideTunnelCount);
            Check("Default group",         before.DefaultGroup,          after.DefaultGroup);

            // Rules list: compare by count and content
            var rulesAdded   = after.Rules.Where(r => !before.Rules.Any(b => b.Ssid == r.Ssid)).ToList();
            var rulesRemoved = before.Rules.Where(r => !after.Rules.Any(a => a.Ssid == r.Ssid)).ToList();
            foreach (var r in rulesAdded)
                log.Debug($"[Settings] Rule added:   {r.Ssid,-20} → {(string.IsNullOrEmpty(r.Tunnel) ? "disconnect" : r.Tunnel)}");
            foreach (var r in rulesRemoved)
                log.Debug($"[Settings] Rule removed: {r.Ssid}");

            // Groups: added/removed
            var grpAdded   = after.TunnelGroups.Where(g => !before.TunnelGroups.Any(b => b.Name == g.Name)).ToList();
            var grpRemoved = before.TunnelGroups.Where(g => !after.TunnelGroups.Any(a => a.Name == g.Name)).ToList();
            foreach (var g in grpAdded)   log.Debug($"[Settings] Group added:   {g.Name}");
            foreach (var g in grpRemoved) log.Debug($"[Settings] Group removed: {g.Name}");
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);
            // If closed without saving, revert any live theme preview
            if (!_savedSuccessfully && !string.IsNullOrEmpty(_originalTheme))
            {
                try { ThemeManager.Instance.Load(_originalTheme); } catch { }
            }
        }

    }
}