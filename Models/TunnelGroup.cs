namespace MasselGUARD.Models
{
    /// <summary>A named group that organises tunnels into collapsible sections.</summary>
    public class TunnelGroup
    {
        public string Name       { get; set; } = "";
        public bool   IsExpanded { get; set; } = true;
        public bool   IsHidden   { get; set; } = false;
        public string Color      { get; set; } = "";

        public TunnelGroup() { }
        public TunnelGroup(string name) { Name = name; }
    }
}
