using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    public class SimTimeOverrideTests
    {
        [Fact]
        public void Replaces_Default_Value()
        {
            string code = "def main():\n    dt = 0.001\n    t_end = 5.0\n    sim_data = ...\n";
            var result = SimTimeOverride.Apply(code, 300.0);
            Assert.Contains("t_end = 300", result);
            Assert.DoesNotContain("t_end = 5.0", result);
        }

        [Theory]
        [InlineData("    t_end = 5.0",     "    t_end = 300")]
        [InlineData("\tt_end = 5.0",       "\tt_end = 300")]
        [InlineData("t_end = 5.0",         "t_end = 300")]
        [InlineData("        t_end=5.0",   "        t_end = 300")]
        public void Preserves_Indent(string input, string expected)
        {
            Assert.Equal(expected, SimTimeOverride.Apply(input, 300.0));
        }

        [Fact]
        public void Does_Not_Change_Unrelated_Code()
        {
            const string code = "x = 1\ny = t_end_other\n";
            Assert.Equal(code, SimTimeOverride.Apply(code, 300.0));
        }

        [Fact]
        public void Multiple_Occurrences_All_Replaced()
        {
            const string code = "def a():\n    t_end = 5.0\n\ndef b():\n    t_end = 5.0\n";
            var result = SimTimeOverride.Apply(code, 100.0);
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(result, "t_end = 100").Count);
        }

        [Fact]
        public void Handles_Integer_And_Decimal()
        {
            Assert.Contains("t_end = 300", SimTimeOverride.Apply("t_end = 5.0", 300));
            Assert.Contains("t_end = 0.5",  SimTimeOverride.Apply("t_end = 5.0", 0.5));
        }

        [Fact]
        public void Empty_Null_Returns_Unchanged()
        {
            Assert.Equal("", SimTimeOverride.Apply("", 300.0));
            Assert.Null(SimTimeOverride.Apply(null, 300.0));
        }
    }
}