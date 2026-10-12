using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.CodeGen
{
    public class PythonSimulationMainBuilderTests
    {
        private ComponentDescriptor CreateSimpleDescriptor()
        {
            return new ComponentDescriptor
            {
                Name = "TestGenerator",
                Type = "ThreePhaseGenerator",
                Category = "Test",
                Parameters = new List<ParameterDescriptor>
                {
                    new ParameterDescriptor { Name = "pole_pairs", Type = "int", DefaultValue = 2 },
                    new ParameterDescriptor { Name = "rated_voltage", Type = "double", DefaultValue = 220.0 }
                },
                InputPorts = new[] { "engine_torque" },
                OutputPorts = new[] { "phase_voltage" }
            };
        }

        [Fact]
        public void BuildPythonSimulationFile_CreatesFile()
        {
            string tempFile = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.py");
            try
            {
                var builder = new PythonSimulationMainBuilder
                {
                    FilePath = tempFile,
                    Components = new List<ComponentDescriptor> { CreateSimpleDescriptor() },
                    Connections = new List<Connection>(),
                    ErrorHandler = (_, __) => { }
                };

                builder.BuildPythonSimulationFile();

                Assert.True(File.Exists(tempFile));
                string content = File.ReadAllText(tempFile);
                Assert.Contains("import numpy as np", content);
                Assert.Contains("def main():", content);
                Assert.Contains("ThreePhaseGenerator", content);
            }
            finally
            {
                if (File.Exists(tempFile)) File.Delete(tempFile);
            }
        }

        [Fact]
        public void BuildPythonSimulationFile_WithoutFilePath_Throws()
        {
            var builder = new PythonSimulationMainBuilder
            {
                FilePath = null,
                Components = new List<ComponentDescriptor>(),
                Connections = new List<Connection>()
            };
            Assert.Throws<InvalidOperationException>(() => builder.BuildPythonSimulationFile());
        }

        [Fact]
        public void BuildPythonSimulationFile_WithoutComponents_Throws()
        {
            var builder = new PythonSimulationMainBuilder
            {
                FilePath = "dummy.py",
                Components = null,
                Connections = new List<Connection>()
            };
            Assert.Throws<InvalidOperationException>(() => builder.BuildPythonSimulationFile());
        }

        [Fact]
        public void BuildPythonSimulationFile_WithoutConnections_Throws()
        {
            var builder = new PythonSimulationMainBuilder
            {
                FilePath = "dummy.py",
                Components = new List<ComponentDescriptor>(),
                Connections = null
            };
            Assert.Throws<InvalidOperationException>(() => builder.BuildPythonSimulationFile());
        }
    }
}
