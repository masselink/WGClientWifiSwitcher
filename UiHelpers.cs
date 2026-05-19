namespace MasselGUARD
{
    /// <summary>
    /// Item in the language picker. Strips any leading emoji/flag prefix from the
    /// display name since WPF cannot render emoji flag sequences.
    /// </summary>
    public class LangItem
    {
        public string Code { get; }
        public string Name { get; }

        public LangItem(string code, string rawName)
        {
            Code = code.ToUpperInvariant();

            // Strip leading non-ASCII flag prefix characters (emoji pairs, spaces)
            string trimmed = rawName.TrimStart();
            int i = 0;
            while (i < trimmed.Length && trimmed[i] > 127)
                i++;
            Name = i > 0 && i < trimmed.Length ? trimmed[i..].TrimStart() : trimmed;
        }

        public override string ToString() => $"[{Code}] {Name}";
    }

    /// <summary>Item in the theme ComboBox pickers.</summary>
    public class ThemePickerItem
    {
        public string FolderName  { get; }
        public string DisplayName { get; }

        public ThemePickerItem(string folder, string display)
        {
            FolderName  = folder;
            DisplayName = display;
        }

        public override string ToString() => DisplayName;
    }
}
