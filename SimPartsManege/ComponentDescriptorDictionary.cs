using System.Collections.Generic;

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
                },
                // ============================================================
                // Signal Sources
                // ============================================================
                {
                    "Sine Wave", new ComponentDescriptor
                    {
                        Name = "Sine Wave",
                        Type = "SineWave",
                        Category = "Signal Source",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "amplitude", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "frequency", Type = "double", DefaultValue = 50.0 },
                            new ParameterDescriptor { Name = "phase", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "offset", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "start_time", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "stop_time", Type = "double", DefaultValue = -1.0 }
                        },
                        InputPorts = new string[] { },
                        OutputPorts = new[] { "value", "phase_rad" }
                    }
                },
                {
                    "Sine Wave Sum", new ComponentDescriptor
                    {
                        Name = "Sine Wave Sum",
                        Type = "SineWaveSum",
                        Category = "Signal Source",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "max_channels", Type = "int", DefaultValue = 3 }
                        },
                        InputPorts = new string[] { },
                        OutputPorts = new[] { "value", "channel_1", "channel_2", "channel_3" }
                    }
                },
                {
                    "Three Phase Sine", new ComponentDescriptor
                    {
                        Name = "Three Phase Sine",
                        Type = "ThreePhaseSine",
                        Category = "Signal Source",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "amplitude", Type = "double", DefaultValue = 100.0 },
                            new ParameterDescriptor { Name = "frequency", Type = "double", DefaultValue = 50.0 },
                            new ParameterDescriptor { Name = "phase", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "sequence", Type = "string", DefaultValue = "positive" }
                        },
                        InputPorts = new string[] { },
                        OutputPorts = new[] { "ua", "ub", "uc", "u_alpha", "u_beta" }
                    }
                },
                // ============================================================
                // Thermal
                // ============================================================
                {
                    "Heater Block", new ComponentDescriptor
                    {
                        Name = "Heater Block",
                        Type = "HeaterBlock",
                        Category = "Thermal",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "rated_power", Type = "double", DefaultValue = 1000.0 },
                            new ParameterDescriptor { Name = "rated_voltage", Type = "double", DefaultValue = 200.0 },
                            new ParameterDescriptor { Name = "thermal_resistance", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "thermal_capacity", Type = "double", DefaultValue = 1000.0 },
                            new ParameterDescriptor { Name = "ambient_temperature", Type = "double", DefaultValue = 25.0 },
                            new ParameterDescriptor { Name = "initial_temperature", Type = "double", DefaultValue = 25.0 }
                        },
                        InputPorts = new[] { "voltage" },
                        OutputPorts = new[] { "power", "temperature", "resistance", "current" }
                    }
                },
                {
                    "Thermal Mass", new ComponentDescriptor
                    {
                        Name = "Thermal Mass",
                        Type = "ThermalMass",
                        Category = "Thermal",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "thermal_resistance", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "thermal_capacity", Type = "double", DefaultValue = 1000.0 },
                            new ParameterDescriptor { Name = "ambient_temperature", Type = "double", DefaultValue = 25.0 },
                            new ParameterDescriptor { Name = "initial_temperature", Type = "double", DefaultValue = 25.0 }
                        },
                        InputPorts = new[] { "heat_flow" },
                        OutputPorts = new[] { "temperature", "heat_loss" }
                    }
                },
                // ============================================================
                // Control
                // ============================================================
                {
                    "First Order Lag", new ComponentDescriptor
                    {
                        Name = "First Order Lag",
                        Type = "FirstOrderLag",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "time_constant", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "initial_output", Type = "double", DefaultValue = 0.0 }
                        },
                        InputPorts = new[] { "input" },
                        OutputPorts = new[] { "output" }
                    }
                },
                {
                    "Integrator", new ComponentDescriptor
                    {
                        Name = "Integrator",
                        Type = "Integrator",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "initial_output", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "gain", Type = "double", DefaultValue = 1.0 }
                        },
                        InputPorts = new[] { "input", "reset" },
                        OutputPorts = new[] { "output" }
                    }
                },
                {
                    "Low Pass Filter", new ComponentDescriptor
                    {
                        Name = "Low Pass Filter",
                        Type = "LowPassFilter",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "cutoff_frequency", Type = "double", DefaultValue = 10.0 },
                            new ParameterDescriptor { Name = "alpha", Type = "double", DefaultValue = 0.1 },
                            new ParameterDescriptor { Name = "initial_output", Type = "double", DefaultValue = 0.0 }
                        },
                        InputPorts = new[] { "input" },
                        OutputPorts = new[] { "output" }
                    }
                },
                {
                    "Gain", new ComponentDescriptor
                    {
                        Name = "Gain",
                        Type = "Gain",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "gain", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "bias", Type = "double", DefaultValue = 0.0 }
                        },
                        InputPorts = new[] { "input" },
                        OutputPorts = new[] { "output" }
                    }
                },
                {
                    "Sum", new ComponentDescriptor
                    {
                        Name = "Sum",
                        Type = "Sum",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "input_count", Type = "int", DefaultValue = 3 }
                        },
                        InputPorts = new[] { "input_1", "input_2", "input_3" },
                        OutputPorts = new[] { "output" }
                    }
                },
                {
                    "Clamp", new ComponentDescriptor
                    {
                        Name = "Clamp",
                        Type = "Clamp",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "lower", Type = "double", DefaultValue = -1.0 },
                            new ParameterDescriptor { Name = "upper", Type = "double", DefaultValue = 1.0 }
                        },
                        InputPorts = new[] { "input" },
                        OutputPorts = new[] { "output", "active" }
                    }
                },
                {
                    "Rate Limiter", new ComponentDescriptor
                    {
                        Name = "Rate Limiter",
                        Type = "RateLimiter",
                        Category = "Control",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "rising_rate", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "falling_rate", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "initial_output", Type = "double", DefaultValue = 0.0 }
                        },
                        InputPorts = new[] { "input" },
                        OutputPorts = new[] { "output", "saturated" }
                    }
                },
                // ============================================================
                // Mechanical
                // ============================================================
                {
                    "Mechanical Inertia", new ComponentDescriptor
                    {
                        Name = "Mechanical Inertia",
                        Type = "MechanicalInertia",
                        Category = "Mechanical",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "inertia", Type = "double", DefaultValue = 0.01 },
                            new ParameterDescriptor { Name = "viscous_friction", Type = "double", DefaultValue = 0.0 },
                            new ParameterDescriptor { Name = "initial_speed", Type = "double", DefaultValue = 0.0 }
                        },
                        InputPorts = new[] { "torque_drive", "torque_load" },
                        OutputPorts = new[] { "speed", "speed_rpm", "accel" }
                    }
                },
                // ============================================================
                // Power Electronics
                // ============================================================
                {
                    "PWM 3 Phase", new ComponentDescriptor
                    {
                        Name = "PWM 3 Phase",
                        Type = "PWM3Phase",
                        Category = "Power Electronics",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "amplitude", Type = "double", DefaultValue = 0.9 },
                            new ParameterDescriptor { Name = "frequency", Type = "double", DefaultValue = 50.0 },
                            new ParameterDescriptor { Name = "carrier_frequency", Type = "double", DefaultValue = 10000.0 },
                            new ParameterDescriptor { Name = "common_mode", Type = "bool", DefaultValue = true }
                        },
                        InputPorts = new string[] { },
                        OutputPorts = new[] { "da", "db", "dc", "modulation_index" }
                    }
                },
                // ============================================================
                // Fault Detection
                // ============================================================
                {
                    "Over Current Detector", new ComponentDescriptor
                    {
                        Name = "Over Current Detector",
                        Type = "OverCurrentDetector",
                        Category = "Fault",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "threshold", Type = "double", DefaultValue = 10.0 },
                            new ParameterDescriptor { Name = "hysteresis", Type = "double", DefaultValue = 1.0 },
                            new ParameterDescriptor { Name = "latch", Type = "bool", DefaultValue = true },
                            new ParameterDescriptor { Name = "initial_tripped", Type = "bool", DefaultValue = false }
                        },
                        InputPorts = new[] { "current", "reset" },
                        OutputPorts = new[] { "tripped", "peak" }
                    }
                }
            };
        }
    }
}
