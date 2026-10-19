namespace PowerSimBuilder.ComponentEdit
{
    public enum ComponentSourceKind
    {
        Bundled,
        Override,
        Adapter,
    }

    public class ComponentInfo
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Category { get; set; }
        public ComponentSourceKind Source { get; set; }
        public string FilePath { get; set; }

        public bool IsEditable
        {
            get { return Source != ComponentSourceKind.Bundled; }
        }

        public string SourceLabel
        {
            get
            {
                switch (Source)
                {
                    case ComponentSourceKind.Bundled:  return I18n.T("EditPanel.SourceBundled");
                    case ComponentSourceKind.Override: return I18n.T("EditPanel.SourceOverride");
                    case ComponentSourceKind.Adapter:  return I18n.T("EditPanel.SourceAdapter");
                    default: return "?";
                }
            }
        }

        public string DisplayName
        {
            get
            {
                switch (Source)
                {
                    case ComponentSourceKind.Bundled:  return "\uD83D\uDD12 " + Name;
                    case ComponentSourceKind.Override: return "\uD83D\uDD04 " + Name;
                    case ComponentSourceKind.Adapter:  return "\uD83D\uDD0C " + Name;
                    default: return Name;
                }
            }
        }

        public override string ToString() { return DisplayName; }
    }
}