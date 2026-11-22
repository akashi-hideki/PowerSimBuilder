using System;

namespace PowerSimBuilder.Physics
{
    /// <summary>
    /// Represents a single FMU model from a catalog (GitHub or custom JSON).
    /// Mirrors the Python dataclass `FmuModel` in fmu_catalog_browser.py.
    /// </summary>
    public class FmuModel
    {
        public string Name { get; set; } = "";
        public string DownloadUrl { get; set; } = "";
        public string FmiVersion { get; set; } = "";
        public string FmiType { get; set; } = "";  // "CoSimulation" or "ModelExchange"
        public string Platform { get; set; } = "";
        public long SizeBytes { get; set; } = 0;
        public string Source { get; set; } = "";
        
        public override string ToString() => 
            $"{Name} (FMI {FmiVersion}, {FmiType}, {Platform})";
    }
}