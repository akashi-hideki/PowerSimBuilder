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
                // 3相交流発電機（簡素化版）
                {
                    "3相交流発電機", new ComponentDescriptor
                    {
                        Name = "3相交流発電機",
                        Type = "ThreePhaseGenerator",
                        Category = "発電機",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "pole_pairs", Type = "int", DefaultValue = 2 },
                            new ParameterDescriptor { Name = "rated_voltage", Type = "double", DefaultValue = 220.0 },
                            new ParameterDescriptor { Name = "rated_frequency", Type = "double", DefaultValue = 50.0 },
                            new ParameterDescriptor { Name = "rated_rpm", Type = "double", DefaultValue = 1500.0 },
                            new ParameterDescriptor { Name = "rotor_inertia", Type = "double", DefaultValue = 2.5 },
                            new ParameterDescriptor { Name = "operation_mode", Type = "string", DefaultValue = "STANDALONE" }
                        },
                        InputPorts = new[] 
                        { 
                            "engine_torque",          // エンジントルク入力 [Nm]
                            "rotation_speed_input",   // 回転数入力 [rpm]（単独動作モード時）
                            "load_impedance"          // 負荷インピーダンス [Ω]
                        },
                        OutputPorts = new[] 
                        { 
                            "phase_voltage",          // 相電圧 [V]
                            "line_voltage",           // 線間電圧 [V]
                            "output_current",         // 出力電流 [A]
                            "frequency",              // 周波数 [Hz]
                            "rotation_speed",         // 回転数 [rpm]
                            "output_power",           // 出力電力 [W]
                            "load_torque"             // 負荷トルク [Nm]
                        }
                    }
                },
                // 誘導電動機（簡素化版）
                {
                    "誘導電動機", new ComponentDescriptor
                    {
                        Name = "誘導電動機",
                        Type = "InductionMotor",
                        Category = "負荷",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "rated_power", Type = "double", DefaultValue = 5.0 },
                            new ParameterDescriptor { Name = "rated_voltage", Type = "double", DefaultValue = 400.0 },
                            new ParameterDescriptor { Name = "rated_speed", Type = "double", DefaultValue = 1450.0 },
                            new ParameterDescriptor { Name = "pole_pairs", Type = "int", DefaultValue = 2 },
                            new ParameterDescriptor { Name = "inertia", Type = "double", DefaultValue = 0.1 }
                        },
                        InputPorts = new[] 
                        { 
                            "stator_voltage",         // 固定子電圧 [V]
                            "supply_frequency",       // 電源周波数 [Hz]
                            "load_torque"             // 負荷トルク [Nm]
                        },
                        OutputPorts = new[] 
                        { 
                            "rotor_speed",            // 回転子速度 [rpm]
                            "stator_current",         // 固定子電流 [A]
                            "output_torque",          // 出力トルク [Nm]
                            "input_power",            // 入力電力 [W]
                            "output_power",           // 出力電力 [W]
                            "efficiency"              // 効率 [%]
                        }
                    }
                },
                // インバータ（簡素化版）
                {
                    "インバータ", new ComponentDescriptor
                    {
                        Name = "インバータ",
                        Type = "Inverter",
                        Category = "電力変換",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "max_dc_voltage", Type = "double", DefaultValue = 800.0 },
                            new ParameterDescriptor { Name = "max_output_current", Type = "double", DefaultValue = 20.0 },
                            new ParameterDescriptor { Name = "control_method", Type = "string", DefaultValue = "v_f_control" },
                            new ParameterDescriptor { Name = "modulation_method", Type = "string", DefaultValue = "SPWM" }
                        },
                        InputPorts = new[] 
                        { 
                            "dc_voltage",             // DC入力電圧 [V]
                            "frequency_command",      // 周波数指令 [Hz]
                            "voltage_command"         // 電圧指令 [V]
                        },
                        OutputPorts = new[] 
                        { 
                            "ac_voltage",             // AC出力電圧 [V]
                            "ac_current",             // AC出力電流 [A]
                            "dc_current",             // DC入力電流 [A]
                            "efficiency"              // 効率 [%]
                        }
                    }
                },
                // ダイオードブリッジ整流回路（簡素化版）
                {
                    "ダイオードブリッジ整流回路", new ComponentDescriptor
                    {
                        Name = "ダイオードブリッジ整流回路",
                        Type = "DiodeBridge",
                        Category = "電力変換",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "diode_forward_voltage", Type = "double", DefaultValue = 0.7 },
                            new ParameterDescriptor { Name = "bridge_type", Type = "string", DefaultValue = "full_wave" }
                        },
                        InputPorts = new[] 
                        { 
                            "ac_voltage",             // AC入力電圧 [V]
                            "load_resistance"         // 負荷抵抗 [Ω]
                        },
                        OutputPorts = new[] 
                        { 
                            "dc_voltage",             // DC出力電圧 [V]
                            "dc_current",             // DC出力電流 [A]
                            "efficiency"              // 効率 [%]
                        }
                    }
                },
                // LCフィルタ回路
                {
                    "LCフィルタ回路", new ComponentDescriptor
                    {
                        Name = "LCフィルタ回路",
                        Type = "LCFilter",
                        Category = "フィルタ",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "inductance", Type = "double", DefaultValue = 0.01 },
                            new ParameterDescriptor { Name = "capacitance", Type = "double", DefaultValue = 0.001 },
                            new ParameterDescriptor { Name = "filter_type", Type = "string", DefaultValue = "low_pass" }
                        },
                        InputPorts = new[] 
                        { 
                            "input_voltage",          // 入力電圧 [V]
                            "load_impedance"          // 負荷インピーダンス [Ω]
                        },
                        OutputPorts = new[] 
                        { 
                            "output_voltage",         // 出力電圧 [V]
                            "output_current",         // 出力電流 [A]
                            "attenuation"             // 減衰率 [dB]
                        }
                    }
                },
                // 電圧レギュレータ
                {
                    "電圧レギュレータ", new ComponentDescriptor
                    {
                        Name = "電圧レギュレータ",
                        Type = "VoltageRegulator",
                        Category = "電源",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "regulation_type", Type = "string", DefaultValue = "linear" },
                            new ParameterDescriptor { Name = "dropout_voltage", Type = "double", DefaultValue = 2.0 }
                        },
                        InputPorts = new[] 
                        { 
                            "input_voltage",          // 入力電圧 [V]
                            "reference_voltage",      // 基準電圧 [V]
                            "load_current"            // 負荷電流 [A]
                        },
                        OutputPorts = new[] 
                        { 
                            "output_voltage",         // 出力電圧 [V]
                            "regulation_error",       // レギュレーション誤差 [%]
                            "current_limit"           // 電流制限値 [A]
                        }
                    }
                },
                // 機械負荷
                {
                    "機械負荷", new ComponentDescriptor
                    {
                        Name = "機械負荷",
                        Type = "MechanicalLoad",
                        Category = "負荷",
                        Parameters = new List<ParameterDescriptor>
                        {
                            new ParameterDescriptor { Name = "load_type", Type = "string", DefaultValue = "constant" },
                            new ParameterDescriptor { Name = "torque_value", Type = "double", DefaultValue = 10.0 }
                        },
                        InputPorts = new[] 
                        { 
                            "speed_input"             // 速度入力 [rpm]
                        },
                        OutputPorts = new[] 
                        { 
                            "torque_output"           // トルク出力 [Nm]
                        }
                    }
                },
                // 故障監視システム
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
                                Name = "monitored_parameters",
                                Type = "List<string>",
                                DefaultValue = new List<string> { "voltage", "current", "speed" }
                            },
                            new ParameterDescriptor
                            {
                                Name = "thresholds",
                                Type = "Dictionary<string, double>",
                                DefaultValue = new Dictionary<string, double>
                                {
                                    { "over_voltage", 250.0 },
                                    { "over_current", 15.0 },
                                    { "over_speed", 1800.0 }
                                }
                            }
                        },
                        InputPorts = new[] 
                        { 
                            "voltage_input",          // 電圧入力 [V]
                            "current_input",          // 電流入力 [A]
                            "speed_input"             // 回転数入力 [rpm]
                        },
                        OutputPorts = new[] 
                        { 
                            "fault_status",           // 故障状態
                            "fault_message"           // 故障メッセージ
                        }
                    }
                }
            };
        }
    }
}