using System.Collections.Generic;

namespace PowerSimBuilder.ComponentEdit
{
    public class FileClassInfo
    {
        public string Name { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string BaseClass { get; set; }
        public List<FileMethodInfo> Methods { get; set; } = new List<FileMethodInfo>();

        public override string ToString() => Name;
    }

    public class FileMethodInfo
    {
        public string Name { get; set; }
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public bool IsRoleFunction { get; set; }
        public bool IsStub { get; set; }

        public override string ToString() => Name;
    }
}