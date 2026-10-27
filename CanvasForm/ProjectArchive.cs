using System;
using System.IO;
using System.IO.Compression;
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

        public class ArchiveOptions
        {
            public bool IncludeComponents { get; set; } = true;
            public bool IncludeGenerated { get; set; } = true;
            public bool IncludeFmu { get; set; } = false;
            public string? ComponentsSourceDir { get; set; }
            public string? GeneratedSourceDir { get; set; }
            public string? FmuSourceDir { get; set; }
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
                var src = options.FmuSourceDir
                    ?? Path.Combine(Directory.GetCurrentDirectory(), "user_physics_models");
                AddDirectoryToZip(zip, src, FmuDir, "*.*", recurse: true);
            }
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