using System;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// Quantifies ControllerBoxDescriptorBuilder output for the real 7-layer design.xml.
    /// Verifies InputPorts/OutputPorts that the GUI import path produces.
    /// </summary>
    public class ControllerBoxDescriptorBuilderTests
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
        public void DesignXml_Builder_ProducesExpectedBasePorts()
        {
            var root = FindRepoRoot();
            Assert.NotNull(root);
            var xmlPath = Path.Combine(root, "tests", "data", "scenarios", "cooking_heater", "design.xml");
            Assert.True(File.Exists(xmlPath), "design.xml not found");

            var reader = new StaTableProjectReader();
            var parsed = reader.ParseFile(xmlPath);
            Assert.Empty(parsed.Errors);

            var gen = new StaTableCodeGenerator();
            var py = gen.Generate(parsed.Project);

            var tmpFile = Path.Combine(Path.GetTempPath(), "psb_desc_" + Guid.NewGuid().ToString("N") + ".py");
            File.WriteAllText(tmpFile, py);
            try
            {
                var desc = ControllerBoxDescriptorBuilder.Build(parsed.Project.Name, tmpFile, xmlPath);

                Assert.Equal("ControllerBox", desc.Type);
                Assert.Equal("Control", desc.Category);

                // Stage 1 (self.globals[...] access) is the current behavior;
                // it captures the FULL read/write set, which is broader than the psbproj curated 4/5 set.
                // Read+write vars appear in BOTH input and output (by design of Stage 1).
                var expectedInputs = new[]
                {
                    "door_open", "fan_on", "heater_bottom_on", "heater_middle_on", "heater_top_on",
                    "humidity", "ir_value", "mw_anode_current", "mw_target_power",
                    "oven_current_temp", "oven_target_temp", "pump_pulse_count", "rx_buf",
                    "seq_total", "stage_duration", "stage_elapsed", "steam_sensor",
                    "steam_target_temp", "thermistor_bottom", "thermistor_middle",
                    "thermistor_top", "zc_count",
                };
                Assert.Equal(expectedInputs, desc.InputPorts.OrderBy(x => x).ToArray());

                var expectedOutputs = new[]
                {
                    "boiler_temp", "error_code", "fan_duty", "fan_on", "heater_bottom_duty",
                    "heater_bottom_on", "heater_middle_duty", "heater_middle_on", "heater_top_duty",
                    "heater_top_on", "humidity", "mag_on", "mw_power", "oven_current_temp",
                    "pump_on", "pump_pulse_count", "seq_step", "stage_elapsed", "stage_mode",
                    "zc_count",
                };
                Assert.Equal(expectedOutputs, desc.OutputPorts.OrderBy(x => x).ToArray());

                // g_system_tick must be excluded
                Assert.DoesNotContain("g_system_tick", desc.InputPorts);
                Assert.DoesNotContain("g_system_tick", desc.OutputPorts);

                // generated_path parameter must point to the .py
                var genPathParam = desc.Parameters.FirstOrDefault(x => x.Name == "generated_path");
                Assert.NotNull(genPathParam);
                Assert.Equal(Path.GetFullPath(tmpFile), genPathParam.DefaultValue);

                // Diagnostic output (shows in test log with -v)
                Console.WriteLine("InputPorts : " + string.Join(", ", desc.InputPorts.OrderBy(x => x)));
                Console.WriteLine("OutputPorts: " + string.Join(", ", desc.OutputPorts.OrderBy(x => x)));
            }
            finally
            {
                try { File.Delete(tmpFile); } catch { }
            }
        }
    }
}
