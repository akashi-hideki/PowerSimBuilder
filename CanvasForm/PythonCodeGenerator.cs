using System.Collections.Generic;
using System.Text;

namespace PowerSimBuilder
{
    /// <summary>
    /// Pythonシミュレーションコード生成クラス
    /// 作成日: 2025-08-13
    /// 注意点: システム構成からPythonシミュレーションコードを生成
    /// </summary>
    public class PythonCodeGenerator
    {
        private int _componentCounter = 1;

        /// <summary>
        /// シミュレーション全体のコードを生成
        /// </summary>
        public string GenerateSimulationCode(IEnumerable<ComponentControl> components, List<Connection> connections)
        {
            var code = new StringBuilder();
            code.AppendLine("from power_system_simulator import *");
            code.AppendLine("import numpy as np");
            code.AppendLine();

            // インスタンス名をリセット
            _componentCounter = 1;
            
            // コンポーネントコード生成
            foreach (var component in components)
            {
                component.InstanceName = $"comp{_componentCounter++}";
                code.AppendLine(GenerateComponentCode(component));
            }
            code.AppendLine();

            // 接続コード生成
            code.AppendLine(GenerateConnectionsCode(connections));
            code.AppendLine();

            // シミュレーション実行コード
            code.AppendLine("def run_simulation():");
            code.AppendLine("    manager = SimulationManager()");

            foreach (var component in components)
            {
                code.AppendLine($"    manager.add_component({component.InstanceName})");
            }

            // 接続を追加
            foreach (var connection in connections)
            {
                code.AppendLine($"    manager.connection_manager.add_connection(" +
                               $"'{connection.StartComponent.InstanceName}', " +
                               $"'{connection.StartPort}', " +
                               $"'{connection.EndComponent.InstanceName}', " +
                               $"'{connection.EndPort}')");
            }

            code.AppendLine("    manager.run_simulation(t_total=0.5, dt=0.001)");
            code.AppendLine();

            return code.ToString();
        }

        /// <summary>
        /// コンポーネント別コード生成
        /// </summary>
        private string GenerateComponentCode(ComponentControl component)
        {
            switch (component.Descriptor.Type)
            {
                case "InductionMotor":
                    return GenerateMotorCode(component);

                case "FaultMonitor":
                    return GenerateFaultMonitorCode(component);

                default:
                    return GenerateDefaultComponentCode(component);
            }
        }

        /// <summary>
        /// モーター用コード生成
        /// </summary>
        private string GenerateMotorCode(ComponentControl motor)
        {
            var code = new StringBuilder();
            code.AppendLine($"{motor.InstanceName} = InductionMotor(");
            code.AppendLine($"    name='{motor.InstanceName}',");
            code.AppendLine($"    params={{");

            bool firstParam = true;
            foreach (var param in motor.Parameters)
            {
                if (!firstParam) code.AppendLine(",");

                if (param.Key == "ThermalResistance" || param.Key == "ThermalCapacity")
                {
                    code.Append($"        '{param.Key}': {param.Value}");
                }
                else
                {
                    code.Append($"        '{param.Key}': {FormatValue(param.Value)}");
                }

                firstParam = false;
            }

            code.AppendLine();
            code.AppendLine($"    }}");
            code.AppendLine($")");
            return code.ToString();
        }

        /// <summary>
        /// 故障監視システム用コード生成
        /// </summary>
        private string GenerateFaultMonitorCode(ComponentControl monitor)
        {
            var code = new StringBuilder();
            code.AppendLine($"{monitor.InstanceName} = FaultMonitor()");

            if (monitor.Parameters.TryGetValue("Conditions", out var conditionsObj) &&
                conditionsObj is List<FaultCondition> conditions)
            {
                foreach (var condition in conditions)
                {
                    code.AppendLine($"{monitor.InstanceName}.add_condition(");
                    code.AppendLine($"    component_name='{condition.ComponentName}',");
                    code.AppendLine($"    state_name='{condition.StateName}',");
                    code.AppendLine($"    condition_type='{condition.ConditionType}'");
                    code.AppendLine($")");
                }
            }

            return code.ToString();
        }

        /// <summary>
        /// デフォルトコンポーネント用コード生成
        /// </summary>
        private string GenerateDefaultComponentCode(ComponentControl component)
        {
            var code = new StringBuilder();
            code.Append($"{component.InstanceName} = {component.Descriptor.Type}(");
            code.Append($"name='{component.InstanceName}'");

            if (component.Parameters.Count > 0)
            {
                code.AppendLine(",");
                code.AppendLine("    params={");

                bool first = true;
                foreach (var param in component.Parameters)
                {
                    if (!first) code.AppendLine(",");
                    code.Append($"        '{param.Key}': {FormatValue(param.Value)}");
                    first = false;
                }

                code.AppendLine();
                code.Append("    }");
            }

            code.AppendLine(")");
            return code.ToString();
        }

        /// <summary>
        /// 接続コード生成
        /// </summary>
        private string GenerateConnectionsCode(List<Connection> connections)
        {
            var code = new StringBuilder();
            foreach (var connection in connections)
            {
                code.AppendLine($"{connection.StartComponent.InstanceName}.connect(" +
                               $"{connection.EndComponent.InstanceName}, " +
                               $"'{connection.StartPort}', '{connection.EndPort}')");
            }
            return code.ToString();
        }

        /// <summary>
        /// 値のフォーマット処理
        /// </summary>
        private string FormatValue(object value)
        {
            if (value is string strValue)
                return $"'{strValue}'";

            return value.ToString();
        }
    }
}