using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// v2 project file schema. Supports ZIP archive (.psbproj) and
    /// single-file JSON (legacy compatible). All data is serializable.
    /// </summary>
    public class ProjectFileV2
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = "2.0";

        [JsonPropertyName("metadata")]
        public ProjectMetadata Metadata { get; set; } = new();

        [JsonPropertyName("components")]
        public List<ComponentInstanceV2> Components { get; set; } = new();

        [JsonPropertyName("connections")]
        public List<ConnectionV2> Connections { get; set; } = new();
    }

    public class ProjectMetadata
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = "";

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = "";

        [JsonPropertyName("dt")]
        public double Dt { get; set; } = 0.001;

        [JsonPropertyName("tEnd")]
        public double TEnd { get; set; } = 5.0;
    }

    public class ComponentInstanceV2
    {
        [JsonPropertyName("instanceName")]
        public string InstanceName { get; set; } = "";

        [JsonPropertyName("descriptor")]
        public ComponentDescriptor Descriptor { get; set; } = new();

        [JsonPropertyName("parameters")]
        public Dictionary<string, object?> Parameters { get; set; } = new();

        [JsonPropertyName("location")]
        public PointData Location { get; set; } = new();

        [JsonPropertyName("physicsModelPath")]
        public string? PhysicsModelPath { get; set; }

        [JsonPropertyName("fmiType")]
        public string? FmiType { get; set; }
    }

    public class ConnectionV2
    {
        /// <summary>"instanceName.port" form, e.g. "plant.output".</summary>
        [JsonPropertyName("from")]
        public string From { get; set; } = "";

        [JsonPropertyName("to")]
        public string To { get; set; } = "";
    }

    public class PointData
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }
    }
}