using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// SPEC §3.2 Build 拡張の 3 分岐検証:
    ///   - 明示選択 → 選択で上書き
    ///   - null       → 旧挙動（auto extract）
    ///   - 空リスト   → 明示的に空
    /// </summary>
    public class ControllerBoxDescriptorBuilderPortSelectionTests
    {
        private const string SamplePy =
            "def step(self):\n" +
            "    a = self.globals['thermistor_top']\n" +
            "    b = self.globals['heater_top_duty']\n" +
            "    c = self.globals['g_system_tick']\n" +
            "    self.globals['error_code'] = 0\n";

        private static string WriteTempPy(string body = null)
        {
            var path = Path.Combine(Path.GetTempPath(),
                "psb_test_" + Guid.NewGuid().ToString("N") + "_powersim.py");
            File.WriteAllText(path, body ?? SamplePy, new UTF8Encoding(false));
            return path;
        }

        [Fact]
        public void Build_WithExplicitSelection_OverridesAutoExtract()
        {
            var py = WriteTempPy();
            try
            {
                var ins = new List<string> { "thermistor_top", "door_open" };
                var outs = new List<string> { "heater_top_duty" };

                var desc = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, ins, outs);

                Assert.Equal(ins.ToArray(),  desc.InputPorts);
                Assert.Equal(outs.ToArray(), desc.OutputPorts);
            }
            finally { File.Delete(py); }
        }

        [Fact]
        public void Build_WithNullSelection_DelegatesToExtractPorts()
        {
            var py = WriteTempPy();
            try
            {
                // null = no override. Whatever ExtractPorts returns, it must be:
                //   (a) deterministic
                //   (b) identical to a second call with nulls (same path)
                var d1 = ControllerBoxDescriptorBuilder.Build("proj", py, null, null, null);
                var d2 = ControllerBoxDescriptorBuilder.Build("proj", py, null, null, null);
                Assert.Equal(d1.InputPorts,  d2.InputPorts);
                Assert.Equal(d1.OutputPorts, d2.OutputPorts);

                // (c) distinguishes null from forced-empty (unless extraction
                //     itself is empty, in which case null == empty is fine)
                var dEmpty = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null,
                    new List<string>(), new List<string>());
                if (d1.InputPorts.Length + d1.OutputPorts.Length > 0)
                {
                    Assert.NotEqual(
                        dEmpty.InputPorts.Length + dEmpty.OutputPorts.Length,
                        d1.InputPorts.Length + d1.OutputPorts.Length);
                }
            }
            finally { File.Delete(py); }
        }
        [Fact]
        public void Build_WithEmptySelection_ReturnsEmptyPorts()
        {
            var py = WriteTempPy();
            try
            {
                var desc = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null,
                    new List<string>(), new List<string>());

                Assert.Empty(desc.InputPorts);
                Assert.Empty(desc.OutputPorts);
            }
            finally { File.Delete(py); }
        }

        [Fact]
        public void Build_OnlyInputSelected_OutputUsesAutoExtract()
        {
            var py = WriteTempPy();
            try
            {
                var baseline = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, null, null);

                var ins = new List<string> { "custom_input" };
                var desc = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, ins, null);

                Assert.Equal(new[] { "custom_input" }, desc.InputPorts);
                // Output side (null) delegates to ExtractPorts -> same as baseline
                Assert.Equal(baseline.OutputPorts, desc.OutputPorts);
            }
            finally { File.Delete(py); }
        }
        [Fact]
        public void Build_OnlyOutputSelected_InputUsesAutoExtract()
        {
            var py = WriteTempPy();
            try
            {
                var baseline = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, null, null);

                var outs = new List<string> { "custom_output" };
                var desc = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, null, outs);

                Assert.Equal(new[] { "custom_output" }, desc.OutputPorts);
                Assert.Equal(baseline.InputPorts, desc.InputPorts);
            }
            finally { File.Delete(py); }
        }
        [Fact]
        public void Build_AlwaysReturnsSameMetadata()
        {
            var py = WriteTempPy();
            try
            {
                var sel = new List<string> { "x" };
                var d1  = ControllerBoxDescriptorBuilder.Build(
                    "P", py, null, sel, sel);
                var d2  = ControllerBoxDescriptorBuilder.Build(
                    "P", py, null, null, null);

                Assert.Equal("P",          d1.Name);
                Assert.Equal("ControllerBox", d1.Type);
                Assert.Equal("Control",    d1.Category);
                Assert.Equal(d1.Name,     d2.Name);
                Assert.Equal(d1.Type,     d2.Type);
                Assert.Equal(d1.Category, d2.Category);
            }
            finally { File.Delete(py); }
        }

        [Fact]
        public void ExtractGlobalsForSelection_NeverThrows()
        {
            var py = WriteTempPy();
            try
            {
                var g = ControllerBoxDescriptorBuilder.ExtractGlobalsForSelection(
                    File.ReadAllText(py, Encoding.UTF8));
                Assert.NotNull(g);
                // Note: exact contents depend on the generator's marker format
                // (Stage 2 `# @variables:` / Stage 3 `GLOBAL_VARIABLES`).
                // Strict content assertions belong in ControllerBoxDescriptorBuilderTests.cs
                // alongside a real generator sample.
            }
            finally { File.Delete(py); }
        }
        [Fact]
        public void ExtractGlobalsForSelection_EmptyString_ReturnsEmpty()
        {
            var g = ControllerBoxDescriptorBuilder.ExtractGlobalsForSelection("");
            Assert.Empty(g);
        }

        [Fact]
        public void ExtractGlobalsForSelection_Null_ReturnsEmpty()
        {
            var g = ControllerBoxDescriptorBuilder.ExtractGlobalsForSelection(null);
            Assert.Empty(g);
        }
    
        // ==== P4.2: ParameterDescriptor entries carry selected_*_ports ====

        [Fact]
        public void Build_WithExplicitSelection_AddsSelectedPortsToParameters()
        {
            var py = WriteTempPy();
            try
            {
                var ins = new List<string> { "thermistor_top", "door_open" };
                var outs = new List<string> { "heater_top_duty" };
                var desc = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, ins, outs);

                var inParam  = desc.Parameters.FirstOrDefault(p => p.Name == "selected_input_ports");
                var outParam = desc.Parameters.FirstOrDefault(p => p.Name == "selected_output_ports");
                var verParam = desc.Parameters.FirstOrDefault(p => p.Name == "port_selection_version");

                Assert.NotNull(inParam);
                Assert.NotNull(outParam);
                Assert.NotNull(verParam);
                Assert.Equal("thermistor_top,door_open", inParam.DefaultValue?.ToString());
                Assert.Equal("heater_top_duty", outParam.DefaultValue?.ToString());
                Assert.Equal("1", verParam.DefaultValue?.ToString());
            }
            finally { File.Delete(py); }
        }

        [Fact]
        public void Build_WithNullSelection_DoesNotAddSelectedPortsParams()
        {
            var py = WriteTempPy();
            try
            {
                var desc = ControllerBoxDescriptorBuilder.Build("proj", py, null, null, null);
                Assert.DoesNotContain(desc.Parameters, p => p.Name == "selected_input_ports");
                Assert.DoesNotContain(desc.Parameters, p => p.Name == "selected_output_ports");
                Assert.DoesNotContain(desc.Parameters, p => p.Name == "port_selection_version");
            }
            finally { File.Delete(py); }
        }

        [Fact]
        public void Build_WithEmptySelection_AddsEmptyCsvParams()
        {
            var py = WriteTempPy();
            try
            {
                var desc = ControllerBoxDescriptorBuilder.Build(
                    "proj", py, null, new List<string>(), new List<string>());
                var inParam  = desc.Parameters.FirstOrDefault(p => p.Name == "selected_input_ports");
                var outParam = desc.Parameters.FirstOrDefault(p => p.Name == "selected_output_ports");

                Assert.NotNull(inParam);
                Assert.NotNull(outParam);
                Assert.Equal("", inParam.DefaultValue?.ToString());
                Assert.Equal("", outParam.DefaultValue?.ToString());
            }
            finally { File.Delete(py); }
        }
}
}