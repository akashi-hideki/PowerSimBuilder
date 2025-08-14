using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    public class ComponentDefinitionManager
    {
        private const string DefinitionFileName = "ComponentDefinitions.xml";
        private readonly string _definitionFilePath;

        // 変更: DefinitionsをComponentDescriptorのリストとして保持
        public List<ComponentDescriptor> Definitions { get; private set; } = new List<ComponentDescriptor>();

        /// <summary>
        /// 定義が変更されたときに発生するイベント
        /// </summary>
        public event EventHandler DefinitionsChanged;

        public ComponentDefinitionManager()
        {
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string definesDirectory = Path.Combine(appDirectory, "defines");
            _definitionFilePath = Path.Combine(definesDirectory, DefinitionFileName);

            if (!Directory.Exists(definesDirectory))
            {
                Directory.CreateDirectory(definesDirectory);
            }
        }

        public void LoadDefinitions()
        {
            List<ComponentDefinition> xmlDefinitions = null;

            if (File.Exists(_definitionFilePath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(List<ComponentDefinition>),
                        new XmlRootAttribute("ComponentDefinitions"));

                    using (FileStream stream = new FileStream(_definitionFilePath, FileMode.Open))
                    {
                        xmlDefinitions = (List<ComponentDefinition>)serializer.Deserialize(stream);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"定義ファイルの読み込みエラー: {ex.Message}");
                    xmlDefinitions = null;
                }
            }

            // ファイルが読み込めなかった場合はデフォルト定義を使用
            if (xmlDefinitions == null)
            {
                xmlDefinitions = DefaultComponentDefinitions.Definitions;
            }

            // ComponentDefinition → ComponentDescriptor に変換（ディープコピー）
            Definitions = ConvertToDescriptors(xmlDefinitions);
            OnDefinitionsChanged();
        }

        public void SaveDefinitions()
        {
            try
            {
                // ComponentDescriptor → ComponentDefinition に変換（ディープコピー）
                var xmlDefinitions = ConvertToDefinitions(Definitions);

                XmlSerializer serializer = new XmlSerializer(typeof(List<ComponentDefinition>),
                    new XmlRootAttribute("ComponentDefinitions"));

                using (FileStream stream = new FileStream(_definitionFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, xmlDefinitions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"定義ファイルの保存エラー: {ex.Message}");
            }
        }

        // 変換メソッド: ComponentDefinition → ComponentDescriptor (ディープコピー)
        private List<ComponentDescriptor> ConvertToDescriptors(List<ComponentDefinition> source)
        {
            return source.Select(def => new ComponentDescriptor
            {
                Name = def.Name,
                Type = def.Type,
                Category = def.Category,
                // アイコンはここでは生成しない（UIで必要に応じて生成）
                Parameters = def.Parameters.Select(p => new ParameterDescriptor
                {
                    Name = p.Name,
                    Type = p.Type,
                    DefaultValue = ConvertValue(p.DefaultValue, p.Type) // 型変換
                }).ToList(),
                InputPorts = def.InputPorts?.ToArray() ?? Array.Empty<string>(),
                OutputPorts = def.OutputPorts?.ToArray() ?? Array.Empty<string>()
            }).ToList();
        }

        // 変換メソッド: ComponentDescriptor → ComponentDefinition (ディープコピー)
        private List<ComponentDefinition> ConvertToDefinitions(List<ComponentDescriptor> source)
        {
            return source.Select(desc => new ComponentDefinition
            {
                Name = desc.Name,
                Type = desc.Type,
                Category = desc.Category,
                // IconColor はここでは設定しない（元のComponentDefinitionにはIconColorがあったが、ComponentDescriptorにはないので、適切にマッピングする必要がある。今回はデフォルト色を割り当てる）
                IconColor = "Gray", // 仮の値。必要に応じて変更。
                Parameters = desc.Parameters.Select(p => new ParameterDefinition
                {
                    Name = p.Name,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue?.ToString() ?? "",
                    Unit = "" // Unitは保存されないため空欄（元の実装に合わせる）
                }).ToList(),
                InputPorts = desc.InputPorts?.ToList() ?? new List<string>(),
                OutputPorts = desc.OutputPorts?.ToList() ?? new List<string>()
            }).ToList();
        }

        // 値の型変換
        private object ConvertValue(string value, string type)
        {
            if (string.IsNullOrEmpty(value)) return null;
            
            try
            {
                switch (type?.ToLower())
                {
                    case "double": return double.Parse(value);
                    case "int": return int.Parse(value);
                    case "bool": return bool.Parse(value);
                    default: return value;
                }
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        /// タイプから定義を取得
        /// </summary>
        public ComponentDescriptor GetDefinition(string type)
        {
            return Definitions.Find(d => 
                d.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        }

        public void AddDefinition(ComponentDescriptor definition)
        {
            if (!Definitions.Exists(d => d.Type == definition.Type))
            {
                Definitions.Add(definition);
                OnDefinitionsChanged();
            }
        }

        private void OnDefinitionsChanged()
        {
            DefinitionsChanged?.Invoke(this, EventArgs.Empty);
        }

        // ディープコピーメソッド
        public List<ComponentDescriptor> DeepCopyDefinitions()
        {
            return Definitions.Select(desc => new ComponentDescriptor
            {
                Name = desc.Name,
                Type = desc.Type,
                Category = desc.Category,
                Parameters = desc.Parameters.Select(p => new ParameterDescriptor
                {
                    Name = p.Name,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue
                }).ToList(),
                InputPorts = desc.InputPorts?.ToArray(),
                OutputPorts = desc.OutputPorts?.ToArray()
            }).ToList();
        }
    }
}
