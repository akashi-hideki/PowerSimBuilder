using System.Collections.Generic;
using System.Text;

namespace PowerSimBuilder
{
    public class PythonCodeGenerator
    {
        private int _componentCounter = 1;

        /// <summary>
        /// Pythonコードを生成する
        /// 注意事項: コンポーネントと接続情報からシミュレーションコードを生成
        /// 作成日: 2025-08-10
        /// </summary>
        public string GenerateCode(IEnumerable<ComponentControl> components)
        {
            var code = new StringBuilder();
            code.AppendLine("from power_system_simulator import *");
            code.AppendLine("import numpy as np");
            code.AppendLine("from scipy.integrate import solve_ivp");
            code.AppendLine();

            foreach (var component in components)
            {
                component.InstanceName = $"comp{_componentCounter++}";
                GenerateComponentCode(code, component);
            }
            code.AppendLine();

            GenerateConnectionsCode(code, components);
            code.AppendLine();

            code.AppendLine("def run_simulation():");
            code.AppendLine("    # シミュレーションロジックをここに実装");
            code.AppendLine("    print(\"シミュレーションを開始します\")");
            code.AppendLine();

            return code.ToString();
        }

        /// <summary>
        /// コンポーネントコードを生成する
        /// 注意事項: パラメータ情報を含むインスタンス作成コードを生成
        /// 作成日: 2025-08-10
        /// </summary>
        private void GenerateComponentCode(StringBuilder code, ComponentControl component)
        {
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
        }

        /// <summary>
        /// 接続コードを生成する
        /// 注意事項: コンポーネント間の接続情報をコード化する
        /// 作成日: 2025-08-10
        /// </summary>
        private void GenerateConnectionsCode(StringBuilder code, IEnumerable<ComponentControl> components)
        {
            foreach (var component in components)
            {
                foreach (var connection in component.Connections)
                {
                    if (connection.StartComponent == component && connection.EndComponent != null)
                    {
                        code.AppendLine($"{component.InstanceName}.connect({connection.EndComponent.InstanceName})");
                    }
                }
            }
        }

        /// <summary>
        /// 値をフォーマットする
        /// 注意事項: 文字列型はクォーテーションで囲む
        /// 作成日: 2025-08-10
        /// </summary>
        private string FormatValue(object value)
        {
            if (value is string strValue)
                return $"'{strValue}'";

            return value.ToString();
        }
    }
}