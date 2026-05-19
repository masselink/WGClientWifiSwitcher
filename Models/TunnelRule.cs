using System.Text.Json.Serialization;
using MasselGUARD.Infrastructure;

namespace MasselGUARD.Models
{
    /// <summary>
    /// A mapping from a WiFi SSID to a WireGuard tunnel.
    /// Empty Tunnel means "disconnect all".
    /// </summary>
    public class TunnelRule : ObservableObject
    {
        private string _ssid          = "";
        private string _tunnel        = "";
        private string _networkType   = "wifi";
        private string _name          = "";

        public string Name
        {
            get => _name;
            set => SetField(ref _name, value);
        }

        public string Ssid
        {
            get => _ssid;
            set { SetField(ref _ssid, value); OnPropertyChanged(nameof(SsidDisplay)); }
        }

        public string Tunnel
        {
            get => _tunnel;
            set { SetField(ref _tunnel, value); OnPropertyChanged(nameof(TunnelDisplay)); }
        }

        /// <summary>"wifi" | "ethernet" | "vpn" | "any"</summary>
        public string NetworkType
        {
            get => _networkType;
            set => SetField(ref _networkType, value);
        }


        [JsonIgnore] public string SsidDisplay => string.IsNullOrEmpty(_ssid) ? "—" : _ssid;

        [JsonIgnore]
        public string TunnelDisplay =>
            string.IsNullOrEmpty(_tunnel) ? "\u2014 disconnect" : _tunnel;

        /// <summary>Number of times this rule has been triggered. Persisted in config.</summary>
        public int ExecutionCount { get; set; } = 0;
    }
}
