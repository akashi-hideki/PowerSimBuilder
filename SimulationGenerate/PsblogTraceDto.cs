using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        [JsonPropertyName("source_files")]
        public List<string> SourceFiles { get; set; } = new();
        [JsonPropertyName("classifications")]
        public List<PsblogClassificationDto> Classifications { get; set; } = new();

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


    public sealed class PsblogClassificationDto
    {
        [JsonPropertyName("var_name")]
        public string VarName { get; set; } = "";

        [JsonPropertyName("category")]
        public string Category { get; set; } = "";

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("proposed_component")]
        public string? ProposedComponent { get; set; }

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = "";
    }
}
