using System;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    public class ComponentScannerTests
    {
        private static string FindRepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "PowerSimBuilder.sln"))) return dir;
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            throw new InvalidOperationException("PowerSimBuilder.sln not found");
        }

        private static string MakeTempRoot()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "psb_scan_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tmp);
            return tmp;
        }

        [Fact]
        public void ScanBundled_FindsKnownComponents()
        {
            var root = FindRepoRoot();
            var list = ComponentScanner.ScanBundled(root);

            Assert.Contains(list, c => c.Name == "Gain" && c.Source == ComponentSourceKind.Bundled);
            Assert.Contains(list, c => c.Name == "Timer");
            Assert.Contains(list, c => c.Name == "PIDController");
        }

        [Fact]
        public void ScanBundled_AllMarkedBundled()
        {
            var root = FindRepoRoot();
            var list = ComponentScanner.ScanBundled(root);
            Assert.NotEmpty(list);
            Assert.All(list, c => Assert.Equal(ComponentSourceKind.Bundled, c.Source));
        }

        [Fact]
        public void ScanOverride_MissingDir_ReturnsEmpty()
        {
            var tmp = MakeTempRoot();
            try
            {
                Assert.Empty(ComponentScanner.ScanOverride(tmp));
            }
            finally { Directory.Delete(tmp, true); }
        }

        [Fact]
        public void ScanOverride_FindsClasses()
        {
            var tmp = MakeTempRoot();
            var dir = Path.Combine(tmp, "user_components");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "Gain.py"), "class Gain:\n    source = 'override'\n");
            File.WriteAllText(Path.Combine(dir, "CustomFilter.py"), "class CustomFilter:\n    pass\n");
            try
            {
                var list = ComponentScanner.ScanOverride(tmp);
                Assert.Equal(2, list.Count);
                Assert.All(list, c => Assert.Equal(ComponentSourceKind.Override, c.Source));
                Assert.Contains(list, c => c.Name == "Gain");
                Assert.Contains(list, c => c.Name == "CustomFilter");
            }
            finally { Directory.Delete(tmp, true); }
        }

        [Fact]
        public void ScanAll_OverrideWinsOverBundled()
        {
            var tmp = MakeTempRoot();
            var bundledDir = Path.Combine(tmp, "src", "python_runtime", "components");
            var overrideDir = Path.Combine(tmp, "user_components");
            Directory.CreateDirectory(bundledDir);
            Directory.CreateDirectory(overrideDir);
            File.WriteAllText(Path.Combine(bundledDir, "Gain.py"), "class Gain:\n    source = 'bundled'\n");
            File.WriteAllText(Path.Combine(overrideDir, "Gain.py"), "class Gain:\n    source = 'override'\n");
            try
            {
                var all = ComponentScanner.ScanAll(tmp, tmp);
                var gain = all.Single(c => c.Name == "Gain");
                Assert.Equal(ComponentSourceKind.Override, gain.Source);
            }
            finally { Directory.Delete(tmp, true); }
        }

        [Fact]
        public void ScanAll_IncludesBothWhenDistinct()
        {
            var tmp = MakeTempRoot();
            var bundledDir = Path.Combine(tmp, "src", "python_runtime", "components");
            var overrideDir = Path.Combine(tmp, "user_components");
            Directory.CreateDirectory(bundledDir);
            Directory.CreateDirectory(overrideDir);
            File.WriteAllText(Path.Combine(bundledDir, "Gain.py"), "class Gain:\n    pass\n");
            File.WriteAllText(Path.Combine(overrideDir, "MyCustom.py"), "class MyCustom:\n    pass\n");
            try
            {
                var all = ComponentScanner.ScanAll(tmp, tmp);
                Assert.Equal(2, all.Count);
                Assert.Contains(all, c => c.Name == "Gain" && c.Source == ComponentSourceKind.Bundled);
                Assert.Contains(all, c => c.Name == "MyCustom" && c.Source == ComponentSourceKind.Override);
            }
            finally { Directory.Delete(tmp, true); }
        }
    }
}