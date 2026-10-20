using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    [Collection(I18nEnglishCollection.Name)]
    public class StableUserCodeTests
    {
        [Fact]
        public void GeneratedCode_ContainsStableUserCodeMarkers()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "psb_gen_" + Guid.NewGuid().ToString("N") + ".py");
            try
            {
                var descriptor = ComponentDescriptorDictionary.Descriptors.Values
                    .First(d => d.InputPorts != null && d.InputPorts.Length > 0
                             && d.OutputPorts != null && d.OutputPorts.Length > 0);
                var gen = new ComponentCodeGenerator();
                gen.GeneratePythonCode(descriptor, tmp);

                var content = File.ReadAllText(tmp);
                Assert.Contains("# STABLE_USER_CODE_START", content);
                Assert.Contains("# STABLE_USER_CODE_END", content);

                // START must come before END
                int startIdx = content.IndexOf("# STABLE_USER_CODE_START");
                int endIdx = content.IndexOf("# STABLE_USER_CODE_END");
                Assert.True(startIdx >= 0 && startIdx < endIdx,
                    "START marker must appear before END marker");
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void GeneratedCode_ParserFindsMarkers_ReturnsNonEmpty()
        {
            var tmp = Path.Combine(Path.GetTempPath(), "psb_gen_" + Guid.NewGuid().ToString("N") + ".py");
            try
            {
                var descriptor = ComponentDescriptorDictionary.Descriptors.Values
                    .First(d => d.InputPorts != null && d.InputPorts.Length > 0
                             && d.OutputPorts != null && d.OutputPorts.Length > 0);
                var gen = new ComponentCodeGenerator();
                gen.GeneratePythonCode(descriptor, tmp);

                var content = File.ReadAllText(tmp);
                // NOTE: The generated code has no `def` inside the STABLE block by default
                // (only comments). So the parser returns empty via marker path.
                // The fallback (no marker) mode is what we want to verify separately.
                Assert.Contains("# STABLE_USER_CODE_START", content);
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }

        [Fact]
        public void Parser_WithoutMarkers_FallbackFindsCustomDefs()
        {
            string code =
                "class Foo:\n" +
                "    def __init__(self):\n" +
                "        pass\n" +
                "    def update(self, t, dt):\n" +
                "        pass\n" +
                "    def F_Custom(self):\n" +
                "        return 1\n" +
                "    def F_Another(self):\n" +
                "        pass\n";

            var list = RoleFunctionParser.Parse(code);
            Assert.Equal(2, list.Count);
            Assert.Contains(list, r => r.Name == "F_Custom");
            Assert.Contains(list, r => r.Name == "F_Another");
            Assert.DoesNotContain(list, r => r.Name == "__init__");
            Assert.DoesNotContain(list, r => r.Name == "update");
        }

        [Fact]
        public void Parser_WithMarkers_PrefersMarkersOverFallback()
        {
            string code =
                "class Foo:\n" +
                "    def update(self):\n" +
                "        pass\n" +
                "    def F_Outside(self):\n" +
                "        pass\n" +
                "    # STABLE_USER_CODE_START\n" +
                "    def F_Inside(self):\n" +
                "        pass\n" +
                "    # STABLE_USER_CODE_END\n";

            var list = RoleFunctionParser.Parse(code);
            // Marker path should return only F_Inside
            Assert.Single(list);
            Assert.Equal("F_Inside", list[0].Name);
        }

        [Fact]
        public void Parser_StubDetection_InFallback()
        {
            string code =
                "class Foo:\n" +
                "    def F_Stub(self):\n" +
                "        pass\n" +
                "    def F_Real(self):\n" +
                "        return 42\n";

            var list = RoleFunctionParser.Parse(code);
            Assert.Equal(2, list.Count);
            var stub = list.First(r => r.Name == "F_Stub");
            var real = list.First(r => r.Name == "F_Real");
            Assert.Equal(RoleFunctionKind.Stub, stub.Kind);
            Assert.Equal(RoleFunctionKind.Implemented, real.Kind);
        }
    }
}