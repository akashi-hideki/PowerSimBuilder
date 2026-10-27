using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// v1.8.0: manifest.json recorded in .psbproj — describes which FMUs
    /// are bundled and which must be obtained externally (with reasons).
    /// </summary>
    public class FmuBundleManifest
    {
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = "1.0";

        [JsonPropertyName("includedFmus")]
        public List<IncludedFmu> IncludedFmus { get; set; } = new();

        [JsonPropertyName("externalFmus")]
        public List<ExternalFmu> ExternalFmus { get; set; } = new();

        [JsonPropertyName("attributions")]
        public List<LicenseAttribution> Attributions { get; set; } = new();
    }

    public class IncludedFmu
    {
        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = "";

        [JsonPropertyName("licenseId")]
        public string LicenseId { get; set; } = "";

        [JsonPropertyName("sizeBytes")]
        public long SizeBytes { get; set; }
    }

    public class ExternalFmu
    {
        [JsonPropertyName("relativePath")]
        public string RelativePath { get; set; } = "";

        /// <summary>"NonRedistributable" | "UnknownLicense" | "SizeExceedsThreshold"</summary>
        [JsonPropertyName("reason")]
        public string Reason { get; set; } = "";

        [JsonPropertyName("sizeBytes")]
        public long SizeBytes { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string? DownloadUrl { get; set; }

        [JsonPropertyName("licenseId")]
        public string? LicenseId { get; set; }
    }

    public class LicenseAttribution
    {
        [JsonPropertyName("fmuRelativePath")]
        public string FmuRelativePath { get; set; } = "";

        [JsonPropertyName("licenseId")]
        public string LicenseId { get; set; } = "";

        [JsonPropertyName("copyrightText")]
        public string CopyrightText { get; set; } = "";
    }
}
