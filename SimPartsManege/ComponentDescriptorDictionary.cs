using System.Collections.Generic;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネント記述子の辞書を提供するクラス
    /// </summary>
    public static class ComponentDescriptorDictionary
    {
        /// <summary>
        /// コンポーネント記述子の辞書（読み取り専用）
        /// </summary>
        public static IReadOnlyDictionary<string, ComponentDescriptor> Descriptors { get; } 
            = CreateDefaultDescriptors();

        private static Dictionary<string, ComponentDescriptor> CreateDefaultDescriptors()
        {
            return new Dictionary<string, ComponentDescriptor>
            {
                {
                    "電圧形インバータ", new ComponentDescriptor
                    {
                        Name = "電圧形インバータ",
                        Type = "VoltageSourceInverter",
                        Category = "電源",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "V_dc", Type = "double", DefaultValue = 300 },
                            new ParameterDescriptor { Name = "f_rated", Type = "double", DefaultValue = 60 }
                        },
                        InputPorts = new string[0],
                        OutputPorts = new[] { "Output" }
                    }
                },
                {
                    "誘導電動機", new ComponentDescriptor
                    {
                        Name = "誘導電動機",
                        Type = "InductionMotor",
                        Category = "負荷",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "R1", Type = "double", DefaultValue = 0.15 },
                            new ParameterDescriptor { Name = "R2", Type = "double", DefaultValue = 0.25 },
                            new ParameterDescriptor { Name = "Lm", Type = "double", DefaultValue = 0.008 },
                            new ParameterDescriptor { Name = "J", Type = "double", DefaultValue = 0.02 },
                            new ParameterDescriptor { Name = "B", Type = "double", DefaultValue = 0.005 },
                            new ParameterDescriptor { Name = "poles", Type = "int", DefaultValue = 4 }
                        },
                        InputPorts = new[] { "VoltageInput" },
                        OutputPorts = new[] { "SpeedOutput", "CurrentOutput" }
                    }
                },
                {
                    "機械負荷", new ComponentDescriptor
                    {
                        Name = "機械負荷",
                        Type = "MechanicalLoad",
                        Category = "負荷",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "load_type", Type = "string", DefaultValue = "constant" },
                            new ParameterDescriptor { Name = "torque_coeff", Type = "double", DefaultValue = 0.1 }
                        },
                        InputPorts = new[] { "SpeedInput" },
                        OutputPorts = new[] { "TorqueOutput" }
                    }
                },
                {
                    "故障監視システム", new ComponentDescriptor
                    {
                        Name = "故障監視システム",
                        Type = "FaultMonitor",
                        Category = "監視",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor
                            {
                                Name = "Conditions",
                                Type = "List<FaultCondition>",
                                DefaultValue = new List<FaultCondition>()
                            }
                        },
                        InputPorts = new[] { "Input1", "Input2" },
                        OutputPorts = new string[0]
                    }
                }
            };
        }
    }
}