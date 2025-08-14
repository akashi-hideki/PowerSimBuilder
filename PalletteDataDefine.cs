using System;
using System.Collections.Generic;
using System.Drawing;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネント記述子クラス
    /// </summary>
    public class ComponentDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Image Icon { get; set; }
        public string Category { get; set; }
        public List<ParameterDescriptor> Parameters { get; set; } = new List<ParameterDescriptor>();
        public string[] InputPorts { get; set; } = Array.Empty<string>();
        public string[] OutputPorts { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }
    }

    /// <summary>
    /// パラメータ記述子クラス
    /// </summary>
    public class ParameterDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public object DefaultValue { get; set; }

        public override string ToString()
        {
            return $"{Name}: {DefaultValue} ({Type})";
        }
    }

    /// <summary>
    /// 故障条件クラス
    /// </summary>
    public class FaultCondition
    {
        public string ComponentName { get; set; }
        public string StateName { get; set; }
        public string ConditionType { get; set; }
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
    }
}
