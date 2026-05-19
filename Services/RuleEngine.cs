using System;
using System.Linq;
using MasselGUARD.Models;

namespace MasselGUARD.Services
{
    /// <summary>
    /// Pure rule-evaluation logic.
    /// No UI references, no side-effects — returns the action to take.
    /// </summary>
    public class RuleEngine
    {
        public enum ActionKind { None, Disconnect, Activate }

        public record RuleResult(ActionKind Action, string? TunnelName, string Reason);

        private static readonly RuleResult DoNothing =
            new(ActionKind.None, null, "No matching rule");

        // ── WiFi evaluation ───────────────────────────────────────────────────

        /// <summary>
        /// Evaluate what should happen when the WiFi network changes.
        /// Order: open-network protection → SSID rules → default action.
        /// </summary>
        public RuleResult EvaluateWifi(
            AppConfig cfg,
            string?   ssid,
            bool      isOpenNetwork)
        {
            if (cfg.ManualMode)
                return DoNothing;

            // 1. Open network protection
            if (isOpenNetwork && !string.IsNullOrEmpty(cfg.OpenWifiTunnel))
                return new(ActionKind.Activate, cfg.OpenWifiTunnel,
                    "Open network protection");

            if (string.IsNullOrEmpty(ssid))
                return DoNothing;

            // 2. SSID rules
            var match = cfg.Rules.FirstOrDefault(r =>
                string.Equals(r.Ssid, ssid, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                match.ExecutionCount++;
                if (string.IsNullOrEmpty(match.Tunnel))
                    return new(ActionKind.Disconnect, null,
                        $"Rule: {ssid} → disconnect");
                return new(ActionKind.Activate, match.Tunnel,
                    $"Rule: {ssid} → {match.Tunnel}");
            }

            // 3. Default action
            return cfg.DefaultAction switch
            {
                "disconnect" => new(ActionKind.Disconnect, null, "Default action: disconnect"),
                "activate" when !string.IsNullOrEmpty(cfg.DefaultTunnel)
                             => new(ActionKind.Activate, cfg.DefaultTunnel,
                                   $"Default action: activate {cfg.DefaultTunnel}"),
                _            => DoNothing,
            };
        }

        /// <summary>
        /// Evaluate what should happen when the WiFi disconnects entirely.
        /// </summary>
        public RuleResult EvaluateWifiDisconnected(AppConfig cfg)
        {
            if (cfg.ManualMode) return DoNothing;

            return cfg.DefaultAction switch
            {
                "disconnect" => new(ActionKind.Disconnect, null,
                    "Default action on WiFi disconnect"),
                _ => DoNothing,
            };
        }
    }
}
