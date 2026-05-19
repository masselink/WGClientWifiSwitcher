namespace MasselGUARD.Models
{
    /// <summary>
    /// A tunnel configuration entry as stored in config.json.
    /// Source="local" means it is managed by MasselGUARD (tunnel.dll).
    /// Any other source means it is a WireGuard-for-Windows profile link.
    /// </summary>
    public class StoredTunnel
    {
        public string  Name   { get; set; } = "";
        /// <summary>Encrypted DPAPI config content, or @embed:... for inline scripts.</summary>
        public string  Config { get; set; } = "";
        /// <summary>"local" | "wireguard" — determines which backend handles this tunnel.</summary>
        public string  Source { get; set; } = "local";
        /// <summary>File path to the .conf or .conf.dpapi file.</summary>
        public string? Path   { get; set; } = null;
        public string  Group  { get; set; } = "";
        public string  Notes  { get; set; } = "";

        // ── Scripts ──────────────────────────────────────────────────────────
        public string PreConnectScript    { get; set; } = "";
        public string PostConnectScript   { get; set; } = "";
        public string PreDisconnectScript { get; set; } = "";
        public string PostDisconnectScript{ get; set; } = "";

        // ── Advanced ─────────────────────────────────────────────────────────
        public bool KillSwitch    { get; set; } = false;
        public int  RetryCount    { get; set; } = 0;
        public int  RetryDelaySec { get; set; } = 5;
    }
}
