using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    public class ComponentDefinitionManager
    {
        private const string DefinitionFileName = "ComponentDefinitions.xml";
        private readonly string _definitionFilePath;

        public List<ComponentDefinition> Definitions { get; private set; } = new List<ComponentDefinition>();

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
            if (File.Exists(_definitionFilePath))
            {
                LoadFromXml();
            }
            else
            {
                // デフォルト定義をクローンして使用
                Definitions = CloneDefinitions(DefaultComponentDefinitions.Definitions);
                SaveDefinitions();
            }
        }

        private void LoadFromXml()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ComponentDefinition>),
                    new XmlRootAttribute("ComponentDefinitions"));

                using (FileStream stream = new FileStream(_definitionFilePath, FileMode.Open))
                {
                    Definitions = (List<ComponentDefinition>)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                Definitions = CloneDefinitions(DefaultComponentDefinitions.Definitions);
                Console.WriteLine($"定義ファイルの読み込みエラー: {ex.Message}");
            }
        }
        public void SaveDefinitions()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ComponentDefinition>),
                    new XmlRootAttribute("ComponentDefinitions"));

                using (FileStream stream = new FileStream(_definitionFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, Definitions);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"定義ファイルの保存エラー: {ex.Message}");
            }
        }

        public ComponentDefinition GetDefinition(string type)
        {
            return Definitions.Find(d => d.Type == type);
        }

        public void AddDefinition(ComponentDefinition definition)
        {
            if (!Definitions.Exists(d => d.Type == definition.Type))
            {
                Definitions.Add(definition);
            }
        }
        private List<ComponentDefinition> CloneDefinitions(List<ComponentDefinition> source)
        {
            return source.Select(CloneDefinition).ToList();
        }

        private ComponentDefinition CloneDefinition(ComponentDefinition source)
        {
            return new ComponentDefinition
            {
                Name = source.Name,
                Type = source.Type,
                Category = source.Category,
                IconColor = source.IconColor,
                Parameters = source.Parameters.Select(p => new ParameterDefinition
                {
                    Name = p.Name,
                    Type = p.Type,
                    DefaultValue = p.DefaultValue,
                    Unit = p.Unit
                }).ToList(),
                InputPorts = new List<string>(source.InputPorts),
                OutputPorts = new List<string>(source.OutputPorts)
            };
        }
        public List<string> GetCategories()
        {
            var categories = new List<string>();
            foreach (var definition in Definitions)
            {
                if (!categories.Contains(definition.Category))
                {
                    categories.Add(definition.Category);
                }
            }
            return categories;
        }

        public Dictionary<string, List<ComponentDefinition>> GetDefinitionsByCategory()
        {
            var categorized = new Dictionary<string, List<ComponentDefinition>>();

            foreach (var definition in Definitions)
            {
                if (!categorized.ContainsKey(definition.Category))
                {
                    categorized[definition.Category] = new List<ComponentDefinition>();
                }
                categorized[definition.Category].Add(definition);
            }

            return categorized;
        }
    }
}