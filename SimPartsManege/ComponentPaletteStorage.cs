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
        private const string LogCategory = "ComponentPaletteStorage";
        
        public static List<ComponentDescriptor> LoadOrCreateDefault()
        {
            string filePath = GetFilePath();
            
            if (File.Exists(filePath))
            {
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"Loading descriptors from: {filePath}");
                var loadedData = LoadFromFile(filePath);
                LogDescriptors("Loaded", loadedData);
                return loadedData;
            }
            
            LogcatTraceHelper.Instance.SimpleMessage(LogCategory, "File not found. Loading default descriptors.");
            var defaultData = ComponentDescriptorDictionary.Descriptors.Values.ToList();
            SaveToFile(defaultData, filePath);
            LogDescriptors("Saved Default", defaultData);
            return defaultData;
        }

        public static void SaveDescriptors(List<ComponentDescriptor> descriptors)
        {
            string filePath = GetFilePath();
            LogDescriptors("Saving", descriptors);
            SaveToFile(descriptors, filePath);
            LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"Descriptors saved to: {filePath}");
        }

        private static string GetFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string palettesDir = Path.Combine(appDir, PalettesFolder);
            
            if (!Directory.Exists(palettesDir))
            {
                Directory.CreateDirectory(palettesDir);
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"Created directory: {palettesDir}");
            }
            
            return Path.Combine(palettesDir, FileName);
        }

        private static List<ComponentDescriptor> LoadFromFile(string filePath)
        {
            try
            {
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
                LogcatTraceHelper.Instance.LogError(LogCategory, $"Load failed: {ex.Message}");
                return ComponentDescriptorDictionary.Descriptors.Values.ToList();
            }
        }

        private static void SaveToFile(List<ComponentDescriptor> descriptors, string filePath)
        {
            try
            {
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
                LogcatTraceHelper.Instance.LogError(LogCategory, $"Save failed: {ex.Message}");
            }
        }

        private static List<ComponentDescriptor> ResetIconProperties(List<ComponentDescriptor> components)
        {
            foreach (var component in components)
            {
                component.Icon = null;
            }
            return components;
        }

        // デバッグ用: コンポーネントリストの内容をログ出力
        private static void LogDescriptors(string operation, List<ComponentDescriptor> descriptors)
        {
            if (descriptors == null)
            {
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"{operation}: Descriptors is null");
                return;
            }

            LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"{operation} Descriptor Count: {descriptors.Count}");
            
            foreach (var component in descriptors)
            {
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"Component: {component.Name} ({component.Type})");
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"  Category: {component.Category}");
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"  InputPorts: {string.Join(", ", component.InputPorts)}");
                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"  OutputPorts: {string.Join(", ", component.OutputPorts)}");
                
                if (component.Parameters != null)
                {
                    LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"  Parameters: {component.Parameters.Count}");
                    foreach (var param in component.Parameters)
                    {
                        LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"    {param.Name}: {param.DefaultValue} ({param.Type})");
                    }
                }
                else
                {
                    LogcatTraceHelper.Instance.SimpleMessage(LogCategory, "  Parameters: null");
                }
#if false
                if (component is FaultMonitorDescriptor faultMonitor)
                {
                    if (faultMonitor.Conditions != null)
                    {
                        LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"  Conditions: {faultMonitor.Conditions.Count}");
                        foreach (var condition in faultMonitor.Conditions)
                        {
                            LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"    Condition: {condition.ComponentName}.{condition.StateName}");
                            LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"      Type: {condition.ConditionType}");
                            
                            if (condition.Parameters != null && condition.Parameters.Count > 0)
                            {
                                LogcatTraceHelper.Instance.SimpleMessage(LogCategory, "      Parameters:");
                                foreach (var kvp in condition.Parameters)
                                {
                                    LogcatTraceHelper.Instance.SimpleMessage(LogCategory, $"        {kvp.Key}: {kvp.Value}");
                                }
                            }
                        }
                    }
                }
#endif
            }
        }
    }
}