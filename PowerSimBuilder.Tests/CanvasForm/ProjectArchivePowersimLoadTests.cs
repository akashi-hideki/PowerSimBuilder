using System;
using System.IO;
using System.IO.Compression;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.CanvasForm
{
    /// <summary>
    /// v1.8.1: Load extracts components/*_powersim.py into generated/ so that
    /// demo_main.py's "generated/foo_powersim.py" path resolves correctly.
    /// </summary>
    public class ProjectArchivePowersimLoadTests
    {
        private static string CreateTempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "psb_psl_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public void Load_ExtractsPowersimFromComponentsToGenerated()
        {
            var root = CreateTempDir();
            try
            {
                var archive = Path.Combine(root, "test.psbproj");
                using (var fs = File.Create(archive))
                using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                {
                    var e1 = zip.CreateEntry("project.json");
                    using (var w = new StreamWriter(e1.Open()))
                        w.Write("{\"components\":[],\"connections\":[]}");
                    var e2 = zip.CreateEntry("components/DemoController_powersim.py");
                    using (var w = new StreamWriter(e2.Open()))
                        w.Write("# demo controller\n");
                    var e3 = zip.CreateEntry("components/other.py");
                    using (var w = new StreamWriter(e3.Open()))
                        w.Write("# other\n");
                }

                var (project, extractDir) = ProjectArchive.Load(archive, extractArtifacts: true);
                Assert.NotNull(extractDir);
                Assert.True(File.Exists(Path.Combine(extractDir!, "generated", "DemoController_powersim.py")),
                    "expected generated/DemoController_powersim.py");
                Assert.False(File.Exists(Path.Combine(extractDir!, "components", "DemoController_powersim.py")),
                    "components/DemoController_powersim.py should not exist after rewrite");
                Assert.True(File.Exists(Path.Combine(extractDir!, "components", "other.py")),
                    "components/other.py should stay");
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }
    }
}
