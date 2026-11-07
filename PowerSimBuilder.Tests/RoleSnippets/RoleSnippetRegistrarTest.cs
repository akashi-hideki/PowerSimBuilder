using System.IO;
using PowerSimBuilder.SimPartsManege;
using Xunit;

namespace PowerSimBuilder.Tests.RoleSnippets
{
    public class RoleSnippetRegistrarTests : System.IDisposable
    {
        private readonly string _tmpRoot;
        private readonly RoleSnippetRegistrar _sut;

        public RoleSnippetRegistrarTests()
        {
            _tmpRoot = Path.Combine(Path.GetTempPath(), "psb_rsnip_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmpRoot);
            _sut = new RoleSnippetRegistrar(_tmpRoot);
        }

        public void Dispose() { try { Directory.Delete(_tmpRoot, true); } catch { } }

        private const string Sample =
@"class Controller:
    def F_StartOvenPid(self):
        return 0
    def F_CheckOvenOverheat(self):
        # STABLE_USER_CODE_START:MwOven.F_CheckOvenOverheat
        temp = self.globals.get(""thermistor_top"", 25) or 25
        if temp > 250:
            self.fire_event(""MwOven"", ""OV_OVERHEAT"")
        return 0
        # STABLE_USER_CODE_END:MwOven.F_CheckOvenOverheat
    def F_Next(self):
        return 0
";

        [Fact]
        public void Extract_ReturnsEnclosingFunction()
        {
            var fn = RoleSnippetRegistrar.ExtractFunctionAtCursor(Sample, 5);
            Assert.NotNull(fn);
            Assert.Equal("F_CheckOvenOverheat", fn!.Name);
            Assert.Equal("MwOven", fn.LayerName);
            Assert.Contains("fire_event", fn.Body);
            Assert.DoesNotContain("STABLE_USER_CODE", fn.Body);
        }

        [Fact]
        public void Extract_NoFunction_ReturnsNull()
            => Assert.Null(RoleSnippetRegistrar.ExtractFunctionAtCursor("x = 1\ny = 2", 0));

        [Fact]
        public void Register_NewSnippet_CreatesFileAndMetadata()
        {
            var fn = RoleSnippetRegistrar.ExtractFunctionAtCursor(Sample, 5)!;
            var r = _sut.Register(fn, "generated/test.py", notes: "n1");
            Assert.True(r.Success);
            Assert.True(File.Exists(r.SavedPath!));
            var content = File.ReadAllText(r.SavedPath!);
            Assert.Contains("fire_event", content);
            Assert.DoesNotContain("STABLE_USER_CODE", content);
            var list = _sut.ListSnippets("MwOven");
            Assert.Single(list);
            Assert.Equal("n1", list[0].Notes);
        }

        [Fact]
        public void Register_Duplicate_RequiresOverwrite()
        {
            var fn = RoleSnippetRegistrar.ExtractFunctionAtCursor(Sample, 5)!;
            _sut.Register(fn, "src1");
            var r2 = _sut.Register(fn, "src2");
            Assert.False(r2.Success);
            Assert.True(r2.RequiresOverwrite);
        }

        [Fact]
        public void Register_DuplicateWithOverwrite_SucceedsOnce()
        {
            var fn = RoleSnippetRegistrar.ExtractFunctionAtCursor(Sample, 5)!;
            _sut.Register(fn, "src1");
            var r2 = _sut.Register(fn, "src2", allowOverwrite: true);
            Assert.True(r2.Success);
            Assert.Single(_sut.ListSnippets("MwOven"));
        }

        [Fact]
        public void Register_NoMarkers_FailsWithLayerError()
        {
            const string t = "class A:\n    def F_Test(self):\n        return 0\n";
            var fn = RoleSnippetRegistrar.ExtractFunctionAtCursor(t, 1)!;
            var r = _sut.Register(fn, "src");
            Assert.False(r.Success);
            Assert.Contains("LayerName", r.Error);
        }

        [Fact]
        public void Register_MissingFunctionPrefix_Fails()
        {
            var fn = RoleSnippetRegistrar.ExtractFunctionAtCursor(Sample, 5)!;
            fn.Name = "NotARoleFn";
            var r = _sut.Register(fn, "src");
            Assert.False(r.Success);
            Assert.Contains("F_", r.Error);
        }

        [Fact]
        public void ListSnippets_FilteredByLayer()
        {
            _sut.Register(RoleSnippetRegistrar.ExtractFunctionAtCursor(Sample, 5)!, "s1");
            _sut.Register(new RoleSnippetRegistrar.ExtractedFunction
            {
                Name = "F_Other", LayerName = "DriverInput", Body = "return 0\n"
            }, "s2");
            Assert.Single(_sut.ListSnippets("MwOven"));
            Assert.Single(_sut.ListSnippets("DriverInput"));
            Assert.Equal(2, _sut.ListSnippets().Count);
        }
    }
}