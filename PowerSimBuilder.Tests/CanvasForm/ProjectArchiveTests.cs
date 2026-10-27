using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.CanvasForm
{
    public class ProjectArchiveTests
    {
        [Fact]
        public void Save_And_Load_RoundTrips()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(),
                "psb_archive_test_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var archivePath = Path.Combine(tmpDir, "test.psbproj");

                var project = new ProjectFileV2
                {
                    Metadata = new ProjectMetadata
                    {
                        Name = "roundtrip_test",
                        Description = "Save/Load round-trip",
                        Dt = 0.01,
                        TEnd = 10.0,
                    },
                    Components = new List<ComponentInstanceV2>
                    {
                        new ComponentInstanceV2
                        {
                            InstanceName = "pid_controller",
                            Descriptor = new ComponentDescriptor
                            {
                                Name = "ControllerBox",
                                Type = "ControllerBox",
                            },
                            Parameters = new Dictionary<string, object?> { ["kp"] = 2.0 },
                            Location = new PointData { X = 100, Y = 100 },
                        },
                        new ComponentInstanceV2
                        {
                            InstanceName = "plant",
                            Descriptor = new ComponentDescriptor
                            {
                                Name = "FirstOrderLag",
                                Type = "FirstOrderLag",
                                PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                                FmiType = "CoSimulation",
                            },
                            PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                            FmiType = "CoSimulation",
                            Parameters = new Dictionary<string, object?> { ["time_constant"] = 1.0 },
                            Location = new PointData { X = 300, Y = 100 },
                        },
                    },
                    Connections = new List<ConnectionV2>
                    {
                        new ConnectionV2 { From = "pid_controller.control", To = "plant.input" },
                        new ConnectionV2 { From = "plant.output", To = "pid_controller.measurement" },
                    },
                };

                ProjectArchive.Save(archivePath, project, new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = false,
                    IncludeGenerated = false,
                    IncludeFmu = false,
                });

                Assert.True(File.Exists(archivePath), "Archive file should exist");
                Assert.True(new FileInfo(archivePath).Length > 0, "Archive should not be empty");

                var (loaded, _) = ProjectArchive.Load(archivePath, extractArtifacts: false);

                Assert.Equal("roundtrip_test", loaded.Metadata.Name);
                Assert.Equal(0.01, loaded.Metadata.Dt, 3);
                Assert.Equal(2, loaded.Components.Count);
                Assert.Equal("pid_controller", loaded.Components[0].InstanceName);
                Assert.Equal("plant", loaded.Components[1].InstanceName);
                Assert.Equal("Dahlquist/source/Dahlquist.fmu", loaded.Components[1].PhysicsModelPath);
                Assert.Equal("CoSimulation", loaded.Components[1].FmiType);
                Assert.Equal(2, loaded.Connections.Count);
                Assert.Equal("pid_controller.control", loaded.Connections[0].From);
                Assert.Equal("plant.input", loaded.Connections[0].To);
            }
            finally
            {
                Directory.Delete(tmpDir, recursive: true);
            }
        }

        [Fact]
        public void SaveAndExpand_ExtractsArtifacts()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(),
                "psb_expand_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var archivePath = Path.Combine(tmpDir, "project.psbproj");

                var project = new ProjectFileV2
                {
                    Metadata = new ProjectMetadata
                    {
                        Name = "expand_test",
                        Dt = 0.01,
                        TEnd = 10.0,
                    },
                    Components = new List<ComponentInstanceV2>
                    {
                        new ComponentInstanceV2
                        {
                            InstanceName = "plant",
                            Descriptor = new ComponentDescriptor
                            {
                                Name = "FirstOrderLag",
                                Type = "FirstOrderLag",
                            },
                            Location = new PointData { X = 100, Y = 100 },
                        },
                    },
                };

                ProjectArchive.SaveAndExpand(archivePath, project, new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = false,
                    IncludeGenerated = false,
                    IncludeFmu = false,
                });

                // ZIP created
                Assert.True(File.Exists(archivePath), "ZIP should exist");

                // v1.8.0: VerifyProjectJsonInSubdir - project.json extracted to <basename>/
                var subdir = Path.Combine(tmpDir, "project");
                Assert.True(Directory.Exists(subdir), $"Subdir should exist: {subdir}");
                Assert.True(File.Exists(Path.Combine(subdir, "project.json")),
                    "project.json should be extracted into <basename>/");

                // Round-trip still works
                var (loaded, _) = ProjectArchive.Load(archivePath, extractArtifacts: false);
                Assert.Equal("expand_test", loaded.Metadata.Name);
                Assert.Equal(1, loaded.Components.Count);
            }
            finally
            {
                try { Directory.Delete(tmpDir, recursive: true); } catch { }
            }
        }

        [Fact]
        public void BundleFmusByLicense_IncludesDahlquistAndManifest()
        {
            // Locate repo root and user_physics_models
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                dir = dir.Parent;
            if (dir == null) return;  // skip if repo root not found

            var userPhysicsModels = Path.Combine(dir.FullName, "user_physics_models");
            var dahlquistFmu = Path.Combine(userPhysicsModels, "Dahlquist", "source", "Dahlquist.fmu");
            if (!File.Exists(dahlquistFmu)) return;  // skip if FMU not present

            var tmpDir = Path.Combine(Path.GetTempPath(),
                "psb_bundle_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var archivePath = Path.Combine(tmpDir, "bundle.psbproj");

                var project = new ProjectFileV2
                {
                    Metadata = new ProjectMetadata { Name = "bundle_test" },
                    Components = new List<ComponentInstanceV2>
                    {
                        new ComponentInstanceV2
                        {
                            InstanceName = "plant",
                            Descriptor = new ComponentDescriptor
                            {
                                Name = "FirstOrderLag",
                                Type = "FirstOrderLag",
                                PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                                FmiType = "CoSimulation",
                            },
                            PhysicsModelPath = "Dahlquist/source/Dahlquist.fmu",
                            FmiType = "CoSimulation",
                            Location = new PointData { X = 100, Y = 100 },
                        },
                    },
                };

                ProjectArchive.Save(archivePath, project, new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = false,
                    IncludeGenerated = false,
                    IncludeFmu = false,
                    IncludeFmusByLicense = true,
                    UserPhysicsModelsRoot = userPhysicsModels,
                    FmuBundleMaxSizeKB = 4096,
                });

                using var zip = ZipFile.OpenRead(archivePath);

                // FMU should be bundled
                var fmuEntry = zip.GetEntry("user_physics_models/Dahlquist/source/Dahlquist.fmu");
                Assert.NotNull(fmuEntry);

                // manifest.json should exist
                var manifestEntry = zip.GetEntry("manifest.json");
                Assert.NotNull(manifestEntry);

                using var stream = manifestEntry!.Open();
                using var reader = new StreamReader(stream);
                var manifestJson = reader.ReadToEnd();

                Assert.Contains("Dahlquist/source/Dahlquist.fmu", manifestJson);
                Assert.Contains("BSD-2-Clause", manifestJson);
                Assert.Contains("Modelica Association", manifestJson);
            }
            finally
            {
                try { Directory.Delete(tmpDir, true); } catch { }
            }
        }

        [Fact]
        public void Load_ThrowsOnMissingFile()
        {
            var missing = Path.Combine(Path.GetTempPath(),
                "nonexistent_" + Guid.NewGuid() + ".psbproj");
            Assert.Throws<FileNotFoundException>(() => ProjectArchive.Load(missing));
        }

        [Fact]
        public void Load_ThrowsOnMissingProjectJson()
        {
            var tmpDir = Path.Combine(Path.GetTempPath(),
                "psb_empty_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(tmpDir);
            try
            {
                var archivePath = Path.Combine(tmpDir, "empty.psbproj");
                using (var fs = File.Create(archivePath))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    // Intentionally empty (no project.json)
                }
                Assert.Throws<InvalidDataException>(() => ProjectArchive.Load(archivePath));
            }
            finally
            {
                Directory.Delete(tmpDir, recursive: true);
            }
        }
    }
}