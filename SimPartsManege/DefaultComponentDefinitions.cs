using System.Collections.Generic;

namespace PowerSimBuilder
{
    public static class DefaultComponentDefinitions
    {
        public static readonly List<ComponentDefinition> Definitions = new List<ComponentDefinition>
        {
        new ComponentDefinition
        {
            Name = "電圧形インバータ",
            Type = "VoltageSourceInverter",
            Category = "電源",
            IconColor = "Red",
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition { Name = "V_dc", Type = "double", DefaultValue = "300", Unit = "V" },
                new ParameterDefinition { Name = "f_rated", Type = "double", DefaultValue = "60", Unit = "Hz" }
            },
            InputPorts = new List<string>(),
            OutputPorts = new List<string> { "Output" }
        },
        new ComponentDefinition
        {
            Name = "誘導電動機",
            Type = "InductionMotor",
            Category = "負荷",
            IconColor = "Blue",
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition { Name = "R1", Type = "double", DefaultValue = "0.15", Unit = "Ω" },
                new ParameterDefinition { Name = "R2", Type = "double", DefaultValue = "0.25", Unit = "Ω" },
                new ParameterDefinition { Name = "Lm", Type = "double", DefaultValue = "0.008", Unit = "H" },
                new ParameterDefinition { Name = "J", Type = "double", DefaultValue = "0.02", Unit = "kg·m²" },
                new ParameterDefinition { Name = "B", Type = "double", DefaultValue = "0.005", Unit = "N·m·s/rad" },
                new ParameterDefinition { Name = "poles", Type = "int", DefaultValue = "4", Unit = "" }
            },
            InputPorts = new List<string> { "VoltageInput" },
            OutputPorts = new List<string> { "SpeedOutput", "CurrentOutput" }
        },
        new ComponentDefinition
        {
            Name = "故障監視システム",
            Type = "FaultMonitor",
            Category = "監視",
            IconColor = "Orange",
            Parameters = new List<ParameterDefinition>
            {
                new ParameterDefinition {
                    Name = "Conditions",
                    Type = "List<FaultCondition>",
                    DefaultValue = "",
                    Unit = ""
                }
            },
            InputPorts = new List<string> { "Input1", "Input2" },
            OutputPorts = new List<string>()
        }
    };
    }
}