using System;
using System.IO;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Psblog
{
    public class PsblogParserTests
    {
        private static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src", "python_runtime")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            throw new InvalidOperationException("repo root not found");
        }

        private static string SamplePath(string name)
            => Path.Combine(FindRepoRoot(), "examples", "logs", name);

        [Fact]
        public void Parse_MinimalLog_ReturnsVars()
        {
            var dto = PsblogParser.Parse(SamplePath("sample_minimal.psblog"));
            Assert.Equal("psblog/v1", dto.Schema);
            Assert.Equal("uart:COM3", dto.Source);
            Assert.Contains("door_open", dto.Vars.Keys);
            Assert.Equal("uint8_t", dto.Vars["door_open"].Type);
            Assert.Equal(5, dto.Rows.Count);
            Assert.Equal(0, dto.Warnings);
        }

        [Fact]
        public void Parse_FileNotFound_Throws()
        {
            var missing = Path.Combine(FindRepoRoot(), "examples", "logs", "no_such_file.psblog");
            var ex = Assert.Throws<PsblogParseException>(
                () => PsblogParser.Parse(missing));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public void Parse_WithRoleHint_ReturnsRole()
        {
            var dto = PsblogParser.Parse(SamplePath("sample_pid_tuning.psblog"));
            Assert.Equal(4, dto.Vars.Count);
            Assert.Equal("target", dto.Vars["oven_target_temp"].Role);
            Assert.Equal("pid_out", dto.Vars["heater_top_duty"].Role);
            Assert.Equal("output", dto.Vars["oven_current_temp"].Role);
            Assert.Equal("error", dto.Vars["error_code"].Role);
        }
    }
}
