using System.Collections.Generic;
using Xunit;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    public class RoleFunctionParserTests
    {
        [Fact]
        public void ParsesImplementedAndStub()
        {
            string code =
                "# STABLE_USER_CODE_START\n" +
                "def F_A(self):\n" +
                "    self.v = 1.0\n" +
                "    return self.v\n" +
                "\n" +
                "def F_B(self):\n" +
                "    pass\n" +
                "# STABLE_USER_CODE_END\n";

            IReadOnlyList<RoleFunctionInfo> list = RoleFunctionParser.Parse(code);

            Assert.Equal(2, list.Count);
            Assert.Equal("F_A", list[0].Name);
            Assert.Equal(RoleFunctionKind.Implemented, list[0].Kind);
            Assert.Equal("F_B", list[1].Name);
            Assert.Equal(RoleFunctionKind.Stub, list[1].Kind);
        }

        [Fact]
        public void ParsesBareMarker_WithNoDefs_ReturnsEmpty()
        {
            string code =
                "# STABLE_USER_CODE_START\n" +
                "# comment only\n" +
                "# STABLE_USER_CODE_END\n";

            Assert.Empty(RoleFunctionParser.Parse(code));
        }

        [Fact]
        public void ParsesNamedMarker_StripsLayerPrefix()
        {
            string code =
                "# STABLE_USER_CODE_START:Layer.F_Oven\n" +
                "# STABLE_USER_CODE_END\n";

            var list = RoleFunctionParser.Parse(code);
            Assert.Single(list);
            Assert.Equal("F_Oven", list[0].Name);
        }

        [Fact]
        public void NullAndEmpty_ReturnEmpty()
        {
            Assert.Empty(RoleFunctionParser.Parse(null));
            Assert.Empty(RoleFunctionParser.Parse(""));
        }

        [Fact]
        public void RecordsLineNumbers_OneBased()
        {
            string code =
                "# STABLE_USER_CODE_START\n" +
                "def F_X(self):\n" +
                "    return 1\n" +
                "# STABLE_USER_CODE_END\n";

            var list = RoleFunctionParser.Parse(code);
            Assert.Single(list);
            Assert.Equal(2, list[0].StartLine);
            Assert.Equal(3, list[0].EndLine);
        }
    }
}