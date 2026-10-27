using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// ZIP-based project archive (.psbproj).
    /// Structure:
    ///   project.psbproj (ZIP)
    ///   ├── project.json
    ///   ├── components/*.py              (optional)
    ///   ├── generated/demo_main.py       (optional)
    ///   ├── generated/simulation_signals.png (optional)
    ///   └── user_physics_models/**       (optional, large)
    /// </summary>
    public static class ProjectArchive
    {
        public const string Extension = ".psbproj";
        public const string ProjectJsonEntry = "project.json";
        public const string ComponentsDir = "components/";
        public const string GeneratedDir = "generated/";
        public const string FmuDir = "user_physics_models/";
        public const string ManifestEntry = "manifest.json";

        public class ArchiveOptions
        {
            public bool IncludeComponents { get; set; } = true;
            public bool IncludeGenerated { get; set; } = true;
            // Legacy blunt copy (all FMUs). Overrides IncludeFmusByLicense if true.
            public bool IncludeFmu { get; set; } = false;
            // v1.8.0: license-gated selective bundling (default).
            public bool IncludeFmusByLicense { get; set; } = true;
            public string? ComponentsSourceDir { get; set; }
            public string? GeneratedSourceDir { get; set; }
            public string? FmuSourceDir { get; set; }
            // Overrides for testing
            public string? UserPhysicsModelsRoot { get; set; }
            public int? FmuBundleMaxSizeKB { get; set; }
        }

        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        public static void Save(string archivePath, ProjectFileV2 project, ArchiveOptions? options = null)
        {
            options ??= new ArchiveOptions();

            if (File.Exists(archivePath)) File.Delete(archivePath);

            using var fs = File.Create(archivePath);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

            var jsonEntry = zip.CreateEntry(ProjectJsonEntry, CompressionLevel.Optimal);
            using (var es = jsonEntry.Open())
            using (var writer = new StreamWriter(es, new UTF8Encoding(false)))
            {
                writer.Write(JsonSerializer.Serialize(project, JsonOptions));
            }

            if (options.IncludeComponents)
            {
                var src = options.ComponentsSourceDir
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "generated");
                // v1.8.0: filter out simulation artifacts (demo_*, _*, *_dbg.py, *_powersim.py)
                AddComponentFilesToZip(zip, src, ComponentsDir);
            }

            if (options.IncludeGenerated)
            {
                var src = options.GeneratedSourceDir
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "generated");
                var demo = Path.Combine(src, "demo_main.py");
                if (File.Exists(demo))
                    zip.CreateEntryFromFile(demo, GeneratedDir + "demo_main.py", CompressionLevel.Optimal);
                var png = Path.Combine(src, "simulation_signals.png");
                if (File.Exists(png))
                    zip.CreateEntryFromFile(png, GeneratedDir + "simulation_signals.png", CompressionLevel.Optimal);
            }

            if (options.IncludeFmu)
            {
                // Legacy blunt copy (all FMUs).
                var src = options.FmuSourceDir
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "user_physics_models");
                AddDirectoryToZip(zip, src, FmuDir, "*.*", recurse: true);
            }
            else if (options.IncludeFmusByLicense)
            {
                // v1.8.0: license-gated selective bundling.
                BundleFmusByLicense(zip, project, options);
            }
        }

        /// <summary>
        /// v1.8.0: For each distinct physicsModelPath referenced by the project,
        /// detect its license and either bundle it (small + redistributable) or
        /// record it in manifest.json externalFmus with a reason.
        /// </summary>
        private static void BundleFmusByLicense(
            ZipArchive zip, ProjectFileV2 project, ArchiveOptions options)
        {
            var root = options.UserPhysicsModelsRoot
                ?? ResolveUserPhysicsModelsRoot();
            var maxKB = options.FmuBundleMaxSizeKB ?? AppSettings.FmuBundleMaxSizeKB;
            var maxBytes = (long)maxKB * 1024L;

            var fmuPaths = (project.Components ?? new List<ComponentInstanceV2>())
                .Where(c => !string.IsNullOrEmpty(c.PhysicsModelPath))
                .Select(c => c.PhysicsModelPath!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (fmuPaths.Count == 0) return;

            var manifest = new FmuBundleManifest();

            foreach (var relPath in fmuPaths)
            {
                var info = FmuLicenseDetector.Detect(root, relPath);
                var safeRel = relPath.Replace('\\', '/');
                var fmuAbsPath = Path.Combine(
                    root, safeRel.Replace('/', Path.DirectorySeparatorChar));

                bool include;
                string? externalReason = null;

                if (!File.Exists(fmuAbsPath))
                {
                    include = false;
                    externalReason = "FileNotFound";
                }
                else if (info.Classification == FmuLicenseClassification.NonRedistributable)
                {
                    include = false;
                    externalReason = "NonRedistributable";
                }
                else if (info.Classification == FmuLicenseClassification.Unknown)
                {
                    include = false;
                    externalReason = "UnknownLicense";
                }
                else if (info.SizeBytes > maxBytes)
                {
                    include = false;
                    externalReason = "SizeExceedsThreshold";
                }
                else
                {
                    include = true;
                }

                if (include)
                {
                    zip.CreateEntryFromFile(
                        fmuAbsPath, FmuDir + safeRel, CompressionLevel.Optimal);
                    manifest.IncludedFmus.Add(new IncludedFmu
                    {
                        RelativePath = safeRel,
                        LicenseId = info.LicenseId,
                        SizeBytes = info.SizeBytes,
                    });
                    manifest.Attributions.Add(new LicenseAttribution
                    {
                        FmuRelativePath = safeRel,
                        LicenseId = info.LicenseId,
                        CopyrightText = info.CopyrightText,
                    });
                }
                else
                {
                    manifest.ExternalFmus.Add(new ExternalFmu
                    {
                        RelativePath = safeRel,
                        Reason = externalReason ?? "Unknown",
                        SizeBytes = info.SizeBytes,
                        DownloadUrl = info.DownloadUrl,
                        LicenseId = info.LicenseId,
                    });
                }
            }

            // Write manifest.json (only if we recorded anything)
            if (manifest.IncludedFmus.Count > 0 || manifest.ExternalFmus.Count > 0)
            {
                var mEntry = zip.CreateEntry(ManifestEntry, CompressionLevel.Optimal);
                using var ms = mEntry.Open();
                using var writer = new StreamWriter(ms, new UTF8Encoding(false));
                writer.Write(JsonSerializer.Serialize(manifest, JsonOptions));
            }
        }

        private static string ResolveUserPhysicsModelsRoot()
        {
            if (!string.IsNullOrEmpty(AppSettings.UserPhysicsModelsRoot)
                && Directory.Exists(AppSettings.UserPhysicsModelsRoot))
                return AppSettings.UserPhysicsModelsRoot;

            // Walk up from exe location to find user_physics_models
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "user_physics_models");
                if (Directory.Exists(candidate)) return candidate;
                dir = dir.Parent;
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "user_physics_models");
        }

        /// <summary>
        /// Save the archive, then immediately extract it into the same directory
        /// so that project.json + components/*.py are available for direct editing
        /// and IDE access in workspace/projects/&lt;name&gt;/.
        /// </summary>
        public static void SaveAndExpand(string archivePath, ProjectFileV2 project, ArchiveOptions? options = null)
        {
            Save(archivePath, project, options);

            // v1.8.0: extract into <basename>/ for per-project isolation
            //   demo_pid.psbproj -> demo_pid/project.json, demo_pid/components/*.py
            var archiveDir = Path.GetDirectoryName(archivePath);
            if (string.IsNullOrEmpty(archiveDir) || !Directory.Exists(archiveDir)) return;

            var basename = Path.GetFileNameWithoutExtension(archivePath);
            if (string.IsNullOrEmpty(basename)) return;

            var projectDir = Path.Combine(archiveDir, basename);

            using var zip = ZipFile.OpenRead(archivePath);
            foreach (var entry in zip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;  // directory entry
                // v1.8.0: extract project.json too (subdirectory extraction, not same-dir)

                var targetPath = Path.Combine(projectDir, entry.FullName.Replace('/', Path.DirectorySeparatorChar));
                var targetDir = Path.GetDirectoryName(targetPath);
                if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);

                entry.ExtractToFile(targetPath, overwrite: true);
            }
        }

        public static (ProjectFileV2 Project, string? ExtractDir) Load(
            string archivePath,
            bool extractArtifacts = false,
            string? extractTarget = null)
        {
            if (!File.Exists(archivePath))
                throw new FileNotFoundException($"Archive not found: {archivePath}");

            using var fs = File.OpenRead(archivePath);
            using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

            var jsonEntry = zip.GetEntry(ProjectJsonEntry);
            if (jsonEntry == null)
                throw new InvalidDataException($"Missing {ProjectJsonEntry} in archive");

            ProjectFileV2 project;
            using (var es = jsonEntry.Open())
            using (var reader = new StreamReader(es, Encoding.UTF8))
            {
                var json = reader.ReadToEnd();
                project = JsonSerializer.Deserialize<ProjectFileV2>(json, JsonOptions)
                    ?? throw new InvalidDataException("Failed to deserialize project.json");
            }

            string? extractDir = null;
            if (extractArtifacts)
            {
                extractDir = extractTarget
                    ?? Path.Combine(Path.GetTempPath(), "psb_" + Guid.NewGuid().ToString("N").Substring(0, 8));
                Directory.CreateDirectory(extractDir);

                foreach (var entry in zip.Entries)
                {
                    if (entry.FullName == ProjectJsonEntry) continue;
                    if (string.IsNullOrEmpty(entry.Name)) continue;

                    var safeRel = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                    if (safeRel.Contains("..")) continue;

                    var targetPath = Path.Combine(extractDir, safeRel);
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!string.IsNullOrEmpty(targetDir)) Directory.CreateDirectory(targetDir);
                    entry.ExtractToFile(targetPath, overwrite: true);
                }
            }

            return (project, extractDir);
        }

        /// <summary>
        /// v1.8.0: Add only component .py files (excluding demo_*, _*, *_dbg.py, *_powersim.py).
        /// Components are the building blocks; simulation artifacts go into generated/.
        /// </summary>
        private static void AddComponentFilesToZip(ZipArchive zip, string srcDir, string zipPrefix)
        {
            if (!Directory.Exists(srcDir)) return;

            foreach (var file in Directory.EnumerateFiles(srcDir, "*.py", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(file);
                // Exclude simulation artifacts
                if (name.StartsWith("demo_")) continue;
                if (name.StartsWith("_")) continue;
                if (name.EndsWith("_dbg.py")) continue;
                if (name.EndsWith("_powersim.py")) continue;
                if (name.StartsWith("test_")) continue;

                zip.CreateEntryFromFile(file, zipPrefix + name, CompressionLevel.Optimal);
            }
        }

        private static void AddDirectoryToZip(
            ZipArchive zip,
            string srcDir,
            string zipPrefix,
            string pattern,
            bool recurse)
        {
            if (!Directory.Exists(srcDir)) return;
            var opt = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            foreach (var file in Directory.EnumerateFiles(srcDir, pattern, opt))
            {
                var rel = Path.GetRelativePath(srcDir, file).Replace('\\', '/');
                zip.CreateEntryFromFile(file, zipPrefix + rel, CompressionLevel.Optimal);
            }
        }
    }
}