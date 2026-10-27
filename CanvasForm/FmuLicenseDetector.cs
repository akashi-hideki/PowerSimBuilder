using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerSimBuilder
{
    public enum FmuLicenseClassification
    {
        Redistributable,
        NonRedistributable,
        Unknown,
    }

    public class FmuLicenseInfo
    {
        public string ModelName { get; set; } = "";
        public string FmuRelativePath { get; set; } = "";
        public string LicenseId { get; set; } = "Unknown";
        public string CopyrightText { get; set; } = "";
        public FmuLicenseClassification Classification { get; set; } = FmuLicenseClassification.Unknown;
        public long SizeBytes { get; set; }
        public string? DownloadUrl { get; set; }
    }

    /// <summary>
    /// v1.8.0: Detects FMU license from model directory / model.yaml /
    /// known Reference-FMUs names, then classifies as Redistributable,
    /// NonRedistributable, or Unknown (safe default: Unknown means no bundle).
    /// </summary>
    public static class FmuLicenseDetector
    {
        private static readonly HashSet<string> RedistributableLicenses = new(StringComparer.OrdinalIgnoreCase)
        {
            "BSD-2-Clause", "BSD-3-Clause", "MIT", "Apache-2.0", "ISC", "Zlib",
            "BSD",  // ambiguous BSD -> treat as 3-clause, allow
        };

        private static readonly HashSet<string> NonRedistributableLicenses = new(StringComparer.OrdinalIgnoreCase)
        {
            "Commercial", "Proprietary", "MapleSoft", "Maplesoft",
        };

        /// <summary>
        /// Known Reference-FMUs model names (Modelica Association, BSD-2-Clause).
        /// Source: https://github.com/modelica/Reference-FMUs
        /// </summary>
        private static readonly HashSet<string> ReferenceFmuNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "BouncingBall", "Dahlquist", "Feedthrough", "Resource", "Stair",
            "Values", "VanDerPol", "StateSpace", "Rectifier", "ModelicaBlocks",
        };

        public static FmuLicenseInfo Detect(string userPhysicsModelsRoot, string fmuRelativePath)
        {
            // fmuRelativePath e.g. "Dahlquist/source/Dahlquist.fmu"
            var parts = fmuRelativePath.Replace('\\', '/').Split('/');
            var modelName = parts.Length > 0 ? parts[0] : "";

            var info = new FmuLicenseInfo
            {
                ModelName = modelName,
                FmuRelativePath = fmuRelativePath,
            };

            var modelDir = Path.Combine(userPhysicsModelsRoot, modelName);
            if (Directory.Exists(modelDir))
            {
                var fmuPath = Path.Combine(modelDir, "source",
                    Path.GetFileName(fmuRelativePath));
                if (File.Exists(fmuPath))
                {
                    info.SizeBytes = new FileInfo(fmuPath).Length;
                }
            }

            // Priority 1: LICENSE file
            if (Directory.Exists(modelDir))
            {
                var licenseFile = FindLicenseFile(modelDir);
                if (licenseFile != null)
                {
                    var text = File.ReadAllText(licenseFile);
                    info.LicenseId = GuessLicenseIdFromText(text);
                    info.CopyrightText = ExtractCopyright(text);
                    Classify(info);
                    return info;
                }

                // Priority 2: model.yaml license: field
                var yamlPath = Path.Combine(modelDir, "model.yaml");
                if (File.Exists(yamlPath))
                {
                    var yamlLicense = ReadYamlLicenseField(yamlPath);
                    if (!string.IsNullOrEmpty(yamlLicense))
                    {
                        info.LicenseId = yamlLicense!;
                        Classify(info);
                        return info;
                    }
                }
            }

            // Priority 3: Known Reference-FMUs
            if (ReferenceFmuNames.Contains(modelName))
            {
                info.LicenseId = "BSD-2-Clause";
                info.CopyrightText = "Copyright (C) 2011-2024 Modelica Association Project FMI. " +
                    "Licensed under BSD-2-Clause. " +
                    "Source: https://github.com/modelica/Reference-FMUs";
                info.DownloadUrl = "https://github.com/modelica/Reference-FMUs";
                info.Classification = FmuLicenseClassification.Redistributable;
                return info;
            }

            // Priority 4: Unknown (deny)
            info.LicenseId = "Unknown";
            info.Classification = FmuLicenseClassification.Unknown;
            return info;
        }

        private static string? FindLicenseFile(string modelDir)
        {
            foreach (var name in new[] { "LICENSE", "LICENSE.txt", "LICENSE.md",
                                          "license", "license.txt", "license.md" })
            {
                var p = Path.Combine(modelDir, name);
                if (File.Exists(p)) return p;
            }
            return null;
        }

        private static string GuessLicenseIdFromText(string text)
        {
            var lower = text.ToLowerInvariant();
            if (lower.Contains("apache license") && lower.Contains("2.0"))
                return "Apache-2.0";
            if (lower.Contains("bsd 2-clause") || lower.Contains("bsd-2-clause"))
                return "BSD-2-Clause";
            if (lower.Contains("bsd 3-clause") || lower.Contains("bsd-3-clause"))
                return "BSD-3-Clause";
            if (lower.Contains("mit license") || lower.Contains("mit license"))
                return "MIT";
            if (lower.Contains("isc license"))
                return "ISC";
            if (lower.Contains("zlib license"))
                return "Zlib";
            return "Unknown";
        }

        private static string ExtractCopyright(string text)
        {
            // Extract first Copyright line (simple heuristic)
            foreach (var line in text.Split('\n'))
            {
                var t = line.Trim();
                if (t.StartsWith("Copyright", StringComparison.OrdinalIgnoreCase) ||
                    t.StartsWith("(c) Copyright", StringComparison.OrdinalIgnoreCase))
                {
                    return t.Length > 300 ? t.Substring(0, 300) : t;
                }
            }
            return "";
        }

        private static string? ReadYamlLicenseField(string yamlPath)
        {
            try
            {
                foreach (var line in File.ReadAllLines(yamlPath))
                {
                    var t = line.TrimStart();
                    if (t.StartsWith("license:", StringComparison.OrdinalIgnoreCase))
                    {
                        var v = t.Substring("license:".Length).Trim().Trim('"', '\'');
                        return string.IsNullOrEmpty(v) ? null : v;
                    }
                }
            }
            catch { }
            return null;
        }

        private static void Classify(FmuLicenseInfo info)
        {
            if (RedistributableLicenses.Contains(info.LicenseId))
                info.Classification = FmuLicenseClassification.Redistributable;
            else if (NonRedistributableLicenses.Contains(info.LicenseId))
                info.Classification = FmuLicenseClassification.NonRedistributable;
            else
                info.Classification = FmuLicenseClassification.Unknown;
        }
    }
}
