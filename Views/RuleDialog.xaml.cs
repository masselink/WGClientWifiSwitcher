using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace MasselGUARD.Views
{
    public partial class RuleDialog : Window
    {
        public string ResultName   { get; private set; } = "";
        public string ResultSsid   { get; private set; } = "";
        public string ResultTunnel { get; private set; } = "";

        private readonly string? _currentSsid;
        private bool _nameManuallyEdited = false;

        public RuleDialog(string? currentSsid,
                          string existingName   = "",
                          string existingSsid   = "",
                          string existingTunnel = "",
                          List<string>? tunnels = null)
        {
            InitializeComponent();
            _currentSsid = currentSsid;

            // Populate tunnel dropdown
            TunnelBox.Items.Clear();
            TunnelBox.Items.Add("");   // blank = disconnect
            if (tunnels != null)
                foreach (var t in tunnels) TunnelBox.Items.Add(t);

            if (!string.IsNullOrEmpty(existingSsid))
            {
                DialogTitle.Text = Lang.T("RuleDialogEditTitle");
                SsidBox.Text     = existingSsid;
                _nameManuallyEdited = !string.IsNullOrEmpty(existingName);
                NameBox.Text     = existingName;
            }

            TunnelBox.Text = existingTunnel;
            NameBox.Focus();
        }

        /// <summary>Auto-generate name from SSID and tunnel unless user has typed one.</summary>
        private void AutoGenerateName()
        {
            if (_nameManuallyEdited) return;
            var ssid   = SsidBox.Text.Trim();
            var tunnel = TunnelBox.Text.Trim();
            string generated;
            if (string.IsNullOrEmpty(ssid))
                generated = "";
            else if (string.IsNullOrEmpty(tunnel))
                generated = $"{ssid} → disconnect";
            else
                generated = $"{ssid} → {tunnel}";

            // Suppress TextChanged feedback loop
            NameBox.TextChanged -= NameBox_TextChanged;
            NameBox.Text = generated;
            NameBox.TextChanged += NameBox_TextChanged;
        }

        private void NameBox_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
        {
            // Mark as manually edited only if user actually typed something
            _nameManuallyEdited = !string.IsNullOrEmpty(NameBox.Text);
        }

        private void SsidBox_TextChanged(object sender,
            System.Windows.Controls.TextChangedEventArgs e)
            => AutoGenerateName();

        private void TunnelBox_Changed(object sender,
            System.Windows.Controls.SelectionChangedEventArgs e)
            => AutoGenerateName();

        private void UseCurrent_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_currentSsid))
                SsidBox.Text = _currentSsid;
            else
                MessageBox.Show(
                    Lang.T("RuleDialogNoWifi"),
                    Lang.T("RuleDialogNoWifiTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var ssid = SsidBox.Text.Trim();
            if (string.IsNullOrEmpty(ssid))
            {
                MessageBox.Show(
                    Lang.T("RuleDialogSsidRequired"),
                    Lang.T("RuleDialogValidationTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                SsidBox.Focus();
                return;
            }
            ResultSsid   = ssid;
            ResultTunnel = TunnelBox.Text.Trim();
            // Use auto-generated name if user didn't type one
            var name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
                name = string.IsNullOrEmpty(ResultTunnel)
                    ? $"{ssid} → disconnect"
                    : $"{ssid} → {ResultTunnel}";
            ResultName   = name;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

        private void Title_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }
    }
}
