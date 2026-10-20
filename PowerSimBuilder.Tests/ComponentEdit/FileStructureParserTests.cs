using System.Linq;
using Xunit;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    public class FileStructureParserTests
    {
        [Fact]
        public void ParsesSingleClassWithMethods()
        {
            string code =
                "class Foo:\n" +
                "    def __init__(self):\n" +
                "        pass\n" +
                "    def F_Custom(self):\n" +
                "        return 0\n";

            var classes = FileStructureParser.Parse(code);
            Assert.Single(classes);
            Assert.Equal("Foo", classes[0].Name);
            Assert.Equal(2, classes[0].Methods.Count);
            Assert.Contains(classes[0].Methods, m => m.Name == "F_Custom" && m.IsRoleFunction && m.IsStub);
        }

        [Fact]
        public void ParsesMultipleClasses()
        {
            string code =
                "class Foo:\n" +
                "    def F_A(self):\n" +
                "        pass\n" +
                "\n" +
                "class Bar:\n" +
                "    def F_B(self):\n" +
                "        return 1\n";

            var classes = FileStructureParser.Parse(code);
            Assert.Equal(2, classes.Count);
            Assert.Equal("Foo", classes[0].Name);
            Assert.Equal("Bar", classes[1].Name);
        }

        [Fact]
        public void ParsesBaseClass()
        {
            string code =
                "class Foo(Base):\n" +
                "    def F_A(self):\n" +
                "        pass\n";
            var classes = FileStructureParser.Parse(code);
            Assert.Single(classes);
            Assert.Equal("Base", classes[0].BaseClass);
        }

        [Fact]
        public void StubDetection()
        {
            string code =
                "class Foo:\n" +
                "    def F_Stub(self):\n" +
                "        pass\n" +
                "    def F_Real(self):\n" +
                "        x = 1\n" +
                "        return x\n";
            var classes = FileStructureParser.Parse(code);
            var methods = classes[0].Methods;
            Assert.True(methods.First(m => m.Name == "F_Stub").IsStub);
            Assert.False(methods.First(m => m.Name == "F_Real").IsStub);
        }

        [Fact]
        public void EmptyAndNull()
        {
            Assert.Empty(FileStructureParser.Parse(null));
            Assert.Empty(FileStructureParser.Parse(""));
            Assert.Empty(FileStructureParser.Parse("# only comments\n"));
        }

        [Fact]
        public void ParsesRealGeneratedFile()
        {
            string code =
                "class DriverInputController(StaTableControllerBase):\n" +
                "    def __init__(self, comp_id=\"driverinput\"):\n" +
                "        pass\n" +
                "    def F_ReadSensorsMovingAvg(self):\n" +
                "        return 0\n" +
                "    def F_EmitIrReady(self):\n" +
                "        return 0\n";

            var classes = FileStructureParser.Parse(code);
            Assert.Single(classes);
            Assert.Equal("DriverInputController", classes[0].Name);
            Assert.Equal("StaTableControllerBase", classes[0].BaseClass);
            Assert.Equal(3, classes[0].Methods.Count);
        }
    }
}