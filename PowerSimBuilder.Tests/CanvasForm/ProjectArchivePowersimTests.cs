using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.CanvasForm
{
    /// <summary>
    /// v1.8.1: .psbproj bundles StaTable-generated *_powersim.py in components/.
    /// </summary>
    public class ProjectArchivePowersimTests
    {
        private static string CreateTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "psb_powersim_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public void Save_IncludePowersim_BundlesPowersimInComponents()
        {
            var root = CreateTempDir();
            try
            {
                var genDir = Path.Combine(root, "generated");
                Directory.CreateDirectory(genDir);
                File.WriteAllText(Path.Combine(genDir, "DemoController_powersim.py"), "# demo controller\n");
                File.WriteAllText(Path.Combine(genDir, "other_component.py"), "# regular\n");

                var project = new ProjectFileV2
                {
                    Components = new System.Collections.Generic.List<ComponentInstanceV2>(),
                    Connections = new System.Collections.Generic.List<ConnectionV2>(),
                };
                var options = new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = true,
                    IncludeGenerated = false,
                    IncludeFmusByLicense = false,
                    IncludePowersim = true,
                    ComponentsSourceDir = genDir,
                };
                var archive = Path.Combine(root, "test.psbproj");
                ProjectArchive.Save(archive, project, options);

                using var zip = ZipFile.OpenRead(archive);
                var entries = zip.Entries.Select(e => e.FullName).ToList();
                Assert.Contains("components/DemoController_powersim.py", entries);
                Assert.Contains("components/other_component.py", entries);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void Save_IncludePowersimFalse_ExcludesPowersim()
        {
            var root = CreateTempDir();
            try
            {
                var genDir = Path.Combine(root, "generated");
                Directory.CreateDirectory(genDir);
                File.WriteAllText(Path.Combine(genDir, "DemoController_powersim.py"), "# x\n");

                var project = new ProjectFileV2
                {
                    Components = new System.Collections.Generic.List<ComponentInstanceV2>(),
                    Connections = new System.Collections.Generic.List<ConnectionV2>(),
                };
                var options = new ProjectArchive.ArchiveOptions
                {
                    IncludeComponents = true,
                    IncludeGenerated = false,
                    IncludeFmusByLicense = false,
                    IncludePowersim = false,
                    ComponentsSourceDir = genDir,
                };
                var archive = Path.Combine(root, "test.psbproj");
                ProjectArchive.Save(archive, project, options);

                using var zip = ZipFile.OpenRead(archive);
                var entries = zip.Entries.Select(e => e.FullName).ToList();
                Assert.DoesNotContain("components/DemoController_powersim.py", entries);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }
    }
}
