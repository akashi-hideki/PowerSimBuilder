using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.CanvasForm
{
    public class ProjectArchiveManifestTests
    {
        [Fact]
        public void LoadWithManifest_ReturnsManifestWhenPresent()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(),
                "psb_mf_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var archivePath = Path.Combine(tmpDir, "with_manifest.psbproj");
                var project = new ProjectFileV2
                {
                    Metadata = new ProjectMetadata { Name = "with_manifest" },
                };

                ProjectArchive.Save(archivePath, project, new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = false,
                    IncludeGenerated = false,
                    IncludeFmu = false,
                    IncludeFmusByLicense = false,  // no FMUs, but manifest may still be created
                });

                // Manually inject a manifest for testing LoadWithManifest
                using (var fs = File.Open(archivePath, FileMode.Open, FileAccess.ReadWrite))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Update))
                {
                    var entry = zip.CreateEntry("manifest.json", CompressionLevel.Optimal);
                    using var es = entry.Open();
                    using var w = new StreamWriter(es);
                    var manifest = new FmuBundleManifest();
                    manifest.ExternalFmus.Add(new ExternalFmu
                    {
                        RelativePath = "TestModel/source/TestModel.fmu",
                        Reason = "UnknownLicense",
                        SizeBytes = 1234,
                        LicenseId = "Unknown",
                    });
                    w.Write(JsonSerializer.Serialize(manifest, ProjectArchive.JsonOptions));
                }

                var (loaded, extractDir, loadedManifest) =
                    ProjectArchive.LoadWithManifest(archivePath, extractArtifacts: false);

                Assert.Equal("with_manifest", loaded.Metadata.Name);
                Assert.NotNull(loadedManifest);
                Assert.Equal(1, loadedManifest!.ExternalFmus.Count);
                Assert.Equal("TestModel/source/TestModel.fmu", loadedManifest.ExternalFmus[0].RelativePath);
                Assert.Equal("UnknownLicense", loadedManifest.ExternalFmus[0].Reason);
            }
            finally
            {
                try { Directory.Delete(tmpDir, true); } catch { }
            }
        }

        [Fact]
        public void LoadWithManifest_NullWhenAbsent()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(),
                "psb_mf_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var archivePath = Path.Combine(tmpDir, "no_manifest.psbproj");
                var project = new ProjectFileV2
                {
                    Metadata = new ProjectMetadata { Name = "no_manifest" },
                };
                ProjectArchive.Save(archivePath, project, new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = false,
                    IncludeGenerated = false,
                    IncludeFmu = false,
                    IncludeFmusByLicense = false,
                });

                var (_, _, manifest) = ProjectArchive.LoadWithManifest(archivePath);
                Assert.Null(manifest);
            }
            finally
            {
                try { Directory.Delete(tmpDir, true); } catch { }
            }
        }

        [Fact]
        public void CheckExternalFmus_DetectsMissing()
        {
            var emptyRoot = Path.Combine(Path.GetTempPath(),
                "psb_empty_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(emptyRoot);
            try
            {
                var manifest = new FmuBundleManifest();
                manifest.ExternalFmus.Add(new ExternalFmu
                {
                    RelativePath = "MissingModel/source/MissingModel.fmu",
                    Reason = "SizeExceedsThreshold",
                    SizeBytes = 100_000_000,
                });

                var missing = ProjectArchive.CheckExternalFmus(manifest,
                    extractDir: null, userPhysicsModelsRoot: emptyRoot);

                Assert.Single(missing);
                Assert.Equal("MissingModel/source/MissingModel.fmu", missing[0].RelativePath);
            }
            finally
            {
                try { Directory.Delete(emptyRoot, true); } catch { }
            }
        }

        [Fact]
        public void CheckExternalFmus_PresentFileNotReported()
        {
            var root = Path.Combine(Path.GetTempPath(),
                "psb_present_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var modelDir = Path.Combine(root, "PresentModel", "source");
            Directory.CreateDirectory(modelDir);
            try
            {
                var fmuPath = Path.Combine(modelDir, "PresentModel.fmu");
                File.WriteAllText(fmuPath, "dummy");

                var manifest = new FmuBundleManifest();
                manifest.ExternalFmus.Add(new ExternalFmu
                {
                    RelativePath = "PresentModel/source/PresentModel.fmu",
                    Reason = "SizeExceedsThreshold",
                });

                var missing = ProjectArchive.CheckExternalFmus(manifest,
                    extractDir: null, userPhysicsModelsRoot: root);

                Assert.Empty(missing);
            }
            finally
            {
                try { Directory.Delete(root, true); } catch { }
            }
        }

        [Fact]
        public void CheckExternalFmus_NullManifestReturnsEmpty()
        {
            var missing = ProjectArchive.CheckExternalFmus(null);
            Assert.Empty(missing);
        }
    }
}
