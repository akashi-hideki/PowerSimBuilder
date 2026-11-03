using System;
using System.IO;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// E2E-style test: real 7-layer design.xml generates deterministic code.
    /// Verifies the Source SHA256 header is stable (matches the checked-in file).
    /// </summary>
    public class StaTableBatchGenerationTests
    {
        private static string FindRepoRoot()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "PowerSimBuilder.sln"))) return dir;
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            return null;
        }

        [Fact]
        public void DesignXml_GeneratesDeterministicCode()
        {
            var root = FindRepoRoot();
            Assert.NotNull(root);
            var xmlPath = Path.Combine(root, "tests", "data", "scenarios", "cooking_heater", "design.xml");
            Assert.True(File.Exists(xmlPath), "design.xml not found: " + xmlPath);

            var reader = new StaTableProjectReader();
            var result = reader.ParseFile(xmlPath);
            Assert.Empty(result.Errors);
            Assert.NotNull(result.Project);
            Assert.Equal(7, result.Project.Tabs.Count);

            var fixedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var code1 = new StaTableCodeGenerator { GeneratedAt = fixedTime }.Generate(result.Project);
            var code2 = new StaTableCodeGenerator { GeneratedAt = fixedTime }.Generate(result.Project);

            Assert.Equal(code1, code2);
            Assert.Contains("Source SHA256: f801106859ebe4cde302ff32f6875e73388468a01ee33e25610a2dcca67b28a0", code1);
            Assert.Contains("StaTable project: CookingHeaterController", code1);
            Assert.Contains("Tabs: 7 (DriverInput", code1);
        }
    }
}
