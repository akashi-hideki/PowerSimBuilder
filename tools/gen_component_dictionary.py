"""Replace ComponentDescriptorDictionary.cs with control-board I/O palette."""
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
TARGET = ROOT / "SimPartsManege" / "ComponentDescriptorDictionary.cs"

NEW_CONTENT = '''using System.Collections.Generic;

namespace PowerSimBuilder
{
    /// <summary>
    /// Provides default component descriptors for the control-board I/O palette.
    /// </summary>
    public static class ComponentDescriptorDictionary
    {
        /// <summary>
        /// Read-only dictionary of component descriptors.
        /// </summary>
        public static IReadOnlyDictionary<string, ComponentDescriptor> Descriptors { get; }
            = CreateDefaultDescriptors();

        private static Dictionary<string, ComponentDescriptor> CreateDefaultDescriptors()
        {
            return new Dictionary<string, ComponentDescriptor>
            {
                {
                    "Digital Input", new ComponentDescriptor
                    {
                        Name = "Digital Input",
                        Type = "DigitalInput",
                        Category = "Digital I/O",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "debounce_ms", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "initial", Type = "int", DefaultValue = 0 }
                        },
                        InputPorts = new[]
                        {
                            "state"
                        },
                        OutputPorts = new[]
                        {
                            "state",
                            "edge_rising",
                            "edge_falling"
                        }
                    }
                },
                {
                    "Digital Output", new ComponentDescriptor
                    {
                        Name = "Digital Output",
                        Type = "DigitalOutput",
                        Category = "Digital I/O",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "active_low", Type = "bool", DefaultValue = false },
                            new ParameterDescriptor { Name = "on_current_ma", Type = "double", DefaultValue = 20.0 }
                        },
                        InputPorts = new[]
                        {
                            "command"
                        },
                        OutputPorts = new[]
                        {
                            "state",
                            "current_draw"
                        }
                    }
                },
                {
                    "Analog Input", new ComponentDescriptor
                    {
                        Name = "Analog Input",
                        Type = "AnalogInput",
                        Category = "Analog I/O",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "v_min", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "v_max", Type = "double", DefaultValue = 5.0 },
                            new ParameterDescriptor { Name = "resolution", Type = "int", DefaultValue = 12 },
                            new ParameterDescriptor { Name = "filter_alpha", Type = "double", DefaultValue = 0.0 }
                        },
                        InputPorts = new[]
                        {
                            "raw"
                        },
                        OutputPorts = new[]
                        {
                            "raw",
                            "scaled",
                            "filtered"
                        }
                    }
                },
                {
                    "Analog Output", new ComponentDescriptor
                    {
                        Name = "Analog Output",
                        Type = "AnalogOutput",
                        Category = "Analog I/O",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "v_ref", Type = "double", DefaultValue = 5.0 },
                            new ParameterDescriptor { Name = "pwm_freq", Type = "double", DefaultValue = 1000.0 },
                            new ParameterDescriptor { Name = "resolution", Type = "int", DefaultValue = 10 }
                        },
                        InputPorts = new[]
                        {
                            "value"
                        },
                        OutputPorts = new[]
                        {
                            "duty",
                            "voltage",
                            "active"
                        }
                    }
                },
                {
                    "PID Controller", new ComponentDescriptor
                    {
                        Name = "PID Controller",
                        Type = "PIDController",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "kp", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "ki", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "kd", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "out_min", Type = "double", DefaultValue = -1e9 },
                            new ParameterDescriptor { Name = "out_max", Type = "double", DefaultValue = 1e9 },
                            new ParameterDescriptor { Name = "setpoint", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "anti_windup", Type = "bool", DefaultValue = false }
                        },
                        InputPorts = new[]
                        {
                            "measured",
                            "setpoint"
                        },
                        OutputPorts = new[]
                        {
                            "control",
                            "error",
                            "p_term",
                            "i_term",
                            "d_term",
                            "saturated"
                        }
                    }
                },
                {
                    "Timer", new ComponentDescriptor
                    {
                        Name = "Timer",
                        Type = "Timer",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "mode", Type = "string", DefaultValue = "delay" },
                            new ParameterDescriptor { Name = "duration", Type = "double", DefaultValue = 1.0 }
                        },
                        InputPorts = new[]
                        {
                            "trigger",
                            "reset"
                        },
                        OutputPorts = new[]
                        {
                            "done",
                            "running",
                            "elapsed"
                        }
                    }
                },
                {
                    "Counter", new ComponentDescriptor
                    {
                        Name = "Counter",
                        Type = "Counter",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "min_count", Type = "int", DefaultValue = 0 },
                            new ParameterDescriptor { Name = "max_count", Type = "int", DefaultValue = 10 },
                            new ParameterDescriptor { Name = "initial", Type = "int", DefaultValue = 0 },
                            new ParameterDescriptor { Name = "direction", Type = "string", DefaultValue = "up" },
                            new ParameterDescriptor { Name = "wrap", Type = "bool", DefaultValue = false }
                        },
                        InputPorts = new[]
                        {
                            "clock",
                            "reset"
                        },
                        OutputPorts = new[]
                        {
                            "count",
                            "overflow",
                            "underflow"
                        }
                    }
                },
                {
                    "State Machine", new ComponentDescriptor
                    {
                        Name = "State Machine",
                        Type = "StateMachine",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "initial_state", Type = "string", DefaultValue = "IDLE" }
                        },
                        InputPorts = new[]
                        {
                            "event",
                            "reset"
                        },
                        OutputPorts = new[]
                        {
                            "current_state",
                            "prev_state",
                            "transitioned"
                        }
                    }
                }
            };
        }
    }
}
'''

if not TARGET.exists():
    print(f"ERROR: {TARGET} not found")
    raise SystemExit(1)

backup = TARGET.with_suffix(".cs.old")
if not backup.exists():
    backup.write_text(TARGET.read_text(encoding="utf-8"), encoding="utf-8")
    print(f"  backup: {backup}")

TARGET.write_text(NEW_CONTENT, encoding="utf-8")
print(f"  + {TARGET} ({len(NEW_CONTENT)} chars)")