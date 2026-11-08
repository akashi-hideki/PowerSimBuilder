using System.Collections.Generic;
using System.Text.Json;

namespace PowerSimBuilder
{
    /// <summary>SignalTrace DTO returned by <c>python -m python_runtime.psblog --json-out</c>.</summary>
    public sealed class PsblogTraceDto
    {
        public Dictionary<string, string> Header { get; set; } = new();
        public Dictionary<string, PsblogVarDto> Vars { get; set; } = new();
        public List<JsonElement[]> Rows { get; set; } = new();
        public Dictionary<string, string> Footer { get; set; } = new();
        public int Warnings { get; set; }
        public List<string> SourceFiles { get; set; } = new();

        public string Schema =>
            Header.TryGetValue("schema", out var s) ? s : "";
        public string CapturedAt =>
            Header.TryGetValue("captured_at", out var s) ? s : "";
        public string Source =>
            Header.TryGetValue("source", out var s) ? s : "";
        public string Dropped =>
            Footer.TryGetValue("dropped", out var s) ? s : "0";
    }

    public sealed class PsblogVarDto
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public string Unit { get; set; } = "";
        public string Group { get; set; } = "";
        public string Description { get; set; } = "";
        public string Role { get; set; } = "";
    }
}
