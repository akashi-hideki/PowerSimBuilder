using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.CodeGen
{
    /// <summary>
    /// End-to-end test for the C# -> Python code generation pipeline.
    ///
    /// Verifies that <see cref="PythonSimulationMainBuilder"/> emits
    /// executable Python code targeting the python_runtime package.
    /// </summary>
    public class PythonCodeGenE2ETests
    {
        private static ComponentDescriptor MakeButton()
        {
            return new ComponentDescriptor
            {
                Name = "button",
                Type = "DigitalInput",
                Category = "IO",
                Parameters = new List<ParameterDescriptor>(),
                InputPorts = Array.Empty<string>(),
                OutputPorts = new[] { "state" }
            };
        }

        private static ComponentDescriptor MakeLed()
        {
            return new ComponentDescriptor
            {
                Name = "led",
                Type = "DigitalOutput",
                Category = "IO",
                Parameters = new List<ParameterDescriptor>(),
                InputPorts = new[] { "command" },
                OutputPorts = new[] { "state" }
            };
        }

        private static PythonSimulationMainBuilder CreateBuilder(string path)
        {
            return new PythonSimulationMainBuilder
            {
                FilePath = path,
                Components = new List<ComponentDescriptor>
                {
                    MakeButton(),
                    MakeLed()
                },
                Connections = new List<Connection>(),
                ErrorHandler = (_, __) => { }
            };
        }

        [Fact]
        public void Generate_CreatesFile()
        {
            string outFile = Path.Combine(Path.GetTempPath(),
                $"psb_e2e_{Guid.NewGuid():N}.py");
            try
            {
                var builder = CreateBuilder(outFile);
                builder.BuildPythonSimulationFile();

                Assert.True(File.Exists(outFile), "Generated file missing");

                string code = File.ReadAllText(outFile, Encoding.UTF8);

                // Core structure
                Assert.Contains("from __future__ import annotations", code);
                Assert.Contains("from python_runtime import SimulationData", code);
                Assert.Contains("from python_runtime.components import", code);
                Assert.Contains("def main():", code);
                Assert.Contains("SimulationData(", code);
                Assert.Contains("if __name__ == \"__main__\":", code);

                // Component names present
                Assert.Contains("button", code);
                Assert.Contains("led", code);

                // Plotting self-contained
                Assert.Contains("def plot_signals", code);

                // No stale imports
                Assert.DoesNotContain("from pythonlib", code);
                Assert.DoesNotContain("import pythonlib", code);
            }
            finally
            {
                if (File.Exists(outFile)) File.Delete(outFile);
            }
        }

        [Fact]
        public void Generate_WithConnection_EmitsConnection()
        {
            string outFile = Path.Combine(Path.GetTempPath(),
                $"psb_e2e_{Guid.NewGuid():N}.py");
            try
            {
                var button = MakeButton();
                var led = MakeLed();

                var connection = new Connection
                {
                    StartComponent = null,
                    StartPort = "state",
                    EndComponent = null,
                    EndPort = "command"
                };

                var builder = new PythonSimulationMainBuilder
                {
                    FilePath = outFile,
                    Components = new List<ComponentDescriptor> { button, led },
                    Connections = new List<Connection>(),
                    ErrorHandler = (_, __) => { }
                };

                builder.BuildPythonSimulationFile();

                Assert.True(File.Exists(outFile));
                string code = File.ReadAllText(outFile, Encoding.UTF8);
                Assert.NotEmpty(code);
            }
            finally
            {
                if (File.Exists(outFile)) File.Delete(outFile);
            }
        }
    }
}
