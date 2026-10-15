using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.CodeGen
{
    /// <summary>
    /// Theory tests: verify code generation for all 23 palette components.
    /// </summary>
    public class PythonCodeGenTheoryTests
    {
        public static IEnumerable<object[]> AllComponents =>
            new[]
            {
                new object[] { "Digital Input", "DigitalInput", new[] { "state" }, new[] { "state" } },
                new object[] { "Digital Output", "DigitalOutput", new[] { "command" }, new[] { "state" } },
                new object[] { "Analog Input", "AnalogInput", new[] { "raw" }, new[] { "raw", "scaled", "filtered" } },
                new object[] { "Analog Output", "AnalogOutput", new[] { "value" }, new[] { "voltage" } },
                new object[] { "PID Controller", "PIDController", new[] { "setpoint", "measured" }, new[] { "control" } },
                new object[] { "Timer", "Timer", new[] { "trigger", "reset" }, new[] { "done" } },
                new object[] { "Counter", "Counter", new[] { "clock", "reset" }, new[] { "count" } },
                new object[] { "State Machine", "StateMachine", new[] { "event", "reset" }, new[] { "current_state" } },
                new object[] { "Sine Wave", "SineWave", new string[0], new[] { "value" } },
                new object[] { "Sine Wave Sum", "SineWaveSum", new string[0], new[] { "value" } },
                new object[] { "Three Phase Sine", "ThreePhaseSine", new string[0], new[] { "ua", "ub", "uc" } },
                new object[] { "Heater Block", "HeaterBlock", new[] { "voltage" }, new[] { "power", "temperature" } },
                new object[] { "Thermal Mass", "ThermalMass", new[] { "heat_flow" }, new[] { "temperature" } },
                new object[] { "First Order Lag", "FirstOrderLag", new[] { "input" }, new[] { "output" } },
                new object[] { "Integrator", "Integrator", new[] { "input", "reset" }, new[] { "output" } },
                new object[] { "Low Pass Filter", "LowPassFilter", new[] { "input" }, new[] { "output" } },
                new object[] { "Gain", "Gain", new[] { "input" }, new[] { "output" } },
                new object[] { "Sum", "Sum", new[] { "input_1" }, new[] { "output" } },
                new object[] { "Clamp", "Clamp", new[] { "input" }, new[] { "output", "active" } },
                new object[] { "Rate Limiter", "RateLimiter", new[] { "input" }, new[] { "output" } },
                new object[] { "Mechanical Inertia", "MechanicalInertia", new[] { "torque_drive" }, new[] { "speed" } },
                new object[] { "PWM 3 Phase", "PWM3Phase", new string[0], new[] { "da", "db", "dc" } },
                new object[] { "Over Current Detector", "OverCurrentDetector", new[] { "current" }, new[] { "tripped", "peak" } },
            };

        private static ComponentDescriptor MakeDescriptor(string name, string type, string[] inputs, string[] outputs)
        {
            return new ComponentDescriptor
            {
                Name = name,
                Type = type,
                Category = "Test",
                Parameters = new List<ParameterDescriptor>(),
                InputPorts = inputs,
                OutputPorts = outputs
            };
        }

        [Theory]
        [MemberData(nameof(AllComponents))]
        public void GeneratedCode_ImportsComponentType(string name, string type, string[] inputs, string[] outputs)
        {
            string outFile = Path.Combine(Path.GetTempPath(),
                $"psb_theory_{Guid.NewGuid():N}.py");
            try
            {
                var builder = new PythonSimulationMainBuilder
                {
                    FilePath = outFile,
                    Components = new List<ComponentDescriptor>
                    {
                        MakeDescriptor(name, type, inputs, outputs)
                    },
                    Connections = new List<Connection>(),
                    ErrorHandler = (_, __) => { }
                };

                builder.BuildPythonSimulationFile();

                Assert.True(File.Exists(outFile), $"File not created for {type}");

                string code = File.ReadAllText(outFile, Encoding.UTF8);

                // Component type must appear in generated code
                Assert.Contains(type, code);
                // Imports must reference python_runtime
                Assert.Contains("from python_runtime", code);
                // No legacy import
                Assert.DoesNotContain("from pythonlib", code);
            }
            finally
            {
                if (File.Exists(outFile)) File.Delete(outFile);
            }
        }

        [Theory]
        [MemberData(nameof(AllComponents))]
        public void GeneratedCode_ContainsComponentName(string name, string type, string[] inputs, string[] outputs)
        {
            string outFile = Path.Combine(Path.GetTempPath(),
                $"psb_theory_{Guid.NewGuid():N}.py");
            try
            {
                var builder = new PythonSimulationMainBuilder
                {
                    FilePath = outFile,
                    Components = new List<ComponentDescriptor>
                    {
                        MakeDescriptor(name, type, inputs, outputs)
                    },
                    Connections = new List<Connection>(),
                    ErrorHandler = (_, __) => { }
                };

                builder.BuildPythonSimulationFile();

                string code = File.ReadAllText(outFile, Encoding.UTF8);
                string instanceName = name.ToLower();
                Assert.Contains(instanceName, code);
            }
            finally
            {
                if (File.Exists(outFile)) File.Delete(outFile);
            }
        }

        [Fact]
        public void GeneratedCode_AllComponents_NotEmpty()
        {
            string outFile = Path.Combine(Path.GetTempPath(),
                $"psb_theory_all_{Guid.NewGuid():N}.py");
            try
            {
                var components = new List<ComponentDescriptor>();
                foreach (var row in AllComponents)
                {
                    components.Add(MakeDescriptor(
                        (string)row[0], (string)row[1],
                        (string[])row[2], (string[])row[3]));
                }

                var builder = new PythonSimulationMainBuilder
                {
                    FilePath = outFile,
                    Components = components,
                    Connections = new List<Connection>(),
                    ErrorHandler = (_, __) => { }
                };

                builder.BuildPythonSimulationFile();

                string code = File.ReadAllText(outFile, Encoding.UTF8);
                Assert.Contains("def main():", code);
                Assert.Contains("SimulationData(", code);
            }
            finally
            {
                if (File.Exists(outFile)) File.Delete(outFile);
            }
        }
    }
}
