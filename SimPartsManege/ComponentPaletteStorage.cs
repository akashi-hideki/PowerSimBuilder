using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    public class ComponentPaletteStorage
    {
        private const string PalettesFolder = "Pallets";
        private const string FileName = "ComponentDescriptors.xml";
        
        public static List<ComponentDescriptor> LoadOrCreateDefault()
        {
            string filePath = GetFilePath();
            
            if (File.Exists(filePath))
            {
                return LoadFromFile(filePath);
            }
            
            // ファイルが存在しない場合はデフォルトデータを取得
            var defaultData = ComponentDescriptorDictionary.Descriptors.Values.ToList();
            SaveToFile(defaultData, filePath);
            return defaultData;
        }

        public static void SaveDescriptors(List<ComponentDescriptor> descriptors)
        {
            string filePath = GetFilePath();
            SaveToFile(descriptors, filePath);
        }

        private static string GetFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string palettesDir = Path.Combine(appDir, PalettesFolder);
            
            if (!Directory.Exists(palettesDir))
            {
                Directory.CreateDirectory(palettesDir);
            }
            
            return Path.Combine(palettesDir, FileName);
        }

        private static List<ComponentDescriptor> LoadFromFile(string filePath)
        {
            try
            {
                // シリアライザーの作成（必要な型を明示的に指定）
                var serializer = new XmlSerializer(
                    typeof(List<ComponentDescriptor>), 
                    new Type[] 
                    {
                        typeof(ParameterDescriptor),
                        typeof(FaultCondition),
                        typeof(SerializableKeyValuePair),
                        typeof(SerializableValue)
                    }
                );
                
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    var loaded = (List<ComponentDescriptor>)serializer.Deserialize(stream);
                    return ResetIconProperties(loaded);
                }
            }
            catch (Exception ex)
            {
                // ロード失敗時はデフォルトデータを返す
                Console.WriteLine($"ロードエラー: {ex.Message}");
                return ComponentDescriptorDictionary.Descriptors.Values.ToList();
            }
        }

        private static void SaveToFile(List<ComponentDescriptor> descriptors, string filePath)
        {
            try
            {
                // シリアライザーの作成（必要な型を明示的に指定）
                var serializer = new XmlSerializer(
                    typeof(List<ComponentDescriptor>), 
                    new Type[] 
                    {
                        typeof(ParameterDescriptor),
                        typeof(FaultCondition),
                        typeof(SerializableKeyValuePair),
                        typeof(SerializableValue)
                    }
                );
                
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(stream, descriptors);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存エラー: {ex.Message}");
            }
        }

        private static List<ComponentDescriptor> ResetIconProperties(List<ComponentDescriptor> components)
        {
            // Iconプロパティを明示的にリセット
            foreach (var component in components)
            {
                component.Icon = null;
            }
            return components;
        }
    }
}