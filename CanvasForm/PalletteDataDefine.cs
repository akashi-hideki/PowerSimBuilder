using System;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネント記述子クラス
    /// </summary>
    [Serializable]
    public class ComponentDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }

        [XmlIgnore] // XMLシリアライズから除外
        public Image Icon { get; set; }

        public string Category { get; set; }
        public List<ParameterDescriptor> Parameters { get; set; } = new List<ParameterDescriptor>();
        public string[] InputPorts { get; set; } = Array.Empty<string>();
        public string[] OutputPorts { get; set; } = Array.Empty<string>();

        public override string ToString()
        {
            return $"{Name} ({Type})";
        }

        // FMU-MVP1: physics model integration
        public string PhysicsModelPath { get; set; }
        public string FmiType { get; set; }
    }

    /// <summary>
    /// パラメータ記述子クラス
    /// </summary>
    [Serializable]
    public class ParameterDescriptor
    {
        public string Name { get; set; }
        public string Type { get; set; }

        // シリアライズ用のプロキシプロパティ
        [XmlElement("DefaultValue")]
        public SerializableValue DefaultValueWrapper
        {
            get => new SerializableValue { Value = DefaultValue };
            set => DefaultValue = value?.GetValue();
        }

        [XmlIgnore]
        public object DefaultValue { get; set; }

        public override string ToString()
        {
            return $"{Name}: {DefaultValue} ({Type})";
        }
    }

    /// <summary>
    /// 故障条件クラス
    /// </summary>
    [Serializable]
    public class FaultCondition
    {
        public string ComponentName { get; set; }
        public string StateName { get; set; }
        public string ConditionType { get; set; }

        // シリアライズ用のプロキシプロパティ
        [XmlArray("Parameters")]
        [XmlArrayItem("Param")]
        public List<SerializableKeyValuePair> SerializableParameters
        {
            get
            {
                var list = new List<SerializableKeyValuePair>();
                foreach (var kvp in Parameters)
                {
                    list.Add(new SerializableKeyValuePair
                    {
                        Key = kvp.Key,
                        Value = new SerializableValue { Value = kvp.Value }
                    });
                }
                return list;
            }
            set
            {
                Parameters.Clear();
                if (value == null) return;

                foreach (var item in value)
                {
                    if (item != null && item.Value != null)
                    {
                        Parameters[item.Key] = item.Value.GetValue();
                    }
                }
            }
        }

        [XmlIgnore]
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
    }

    // シリアライズ可能なKey-Valueペア
    [Serializable]
    public class SerializableKeyValuePair
    {
        [XmlAttribute("Key")]
        public string Key { get; set; }

        public SerializableValue Value { get; set; }
    }

    // シリアライズ可能な値オブジェクト
    [Serializable]
    public class SerializableValue
    {
        [XmlText]
        public string StringValue { get; set; }

        [XmlAttribute("Type")]
        public string TypeName { get; set; }

        [XmlIgnore]
        public object Value
        {
            set
            {
                if (value == null)
                {
                    StringValue = null;
                    TypeName = null;
                    return;
                }

                StringValue = value.ToString();
                TypeName = value.GetType().FullName;
            }
        }

        public object GetValue()
        {
            if (string.IsNullOrEmpty(TypeName))
                return StringValue;

            // C# 7.3 互換のswitch文
            switch (TypeName)
            {
                case "System.Int32":
                    if (int.TryParse(StringValue, out int intVal))
                        return intVal;
                    break;

                case "System.Double":
                    if (double.TryParse(StringValue, out double dblVal))
                        return dblVal;
                    break;

                case "System.Boolean":
                    if (bool.TryParse(StringValue, out bool boolVal))
                        return boolVal;
                    break;
            }

            return StringValue;
        }
    }
}