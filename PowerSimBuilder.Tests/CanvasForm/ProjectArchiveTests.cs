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