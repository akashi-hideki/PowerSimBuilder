#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PowerSimBuilder;

/// <summary>
/// Scans user_physics_models/*/model.yaml and registers descriptors
/// into whatever ComponentDescriptorDictionary the app exposes (via reflection).
/// </summary>
public static class PhysicsComponentScanner
{
    public static void ScanUserPhysicsModels(string userPhysicsRoot)
    {
        if (!Directory.Exists(userPhysicsRoot)) return;

        var dictType = FindType("ComponentDescriptorDictionary");
        var registerMethods = dictType != null
            ? dictType.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.Name is "Register" or "Add" or "AddOrUpdate" or "Set")
                .Where(m => m.GetParameters().Length == 1)
                .ToList()
            : new List<MethodInfo>();

        var descriptorType = FindType("ComponentDescriptor");

        foreach (var modelDir in Directory.EnumerateDirectories(userPhysicsRoot))
        {
            var manifest = Path.Combine(modelDir, "model.yaml");
            if (!File.Exists(manifest)) continue;
            try
            {
                var desc = ParsePhysicsManifest(manifest, modelDir, descriptorType);
                foreach (var m in registerMethods)
                {
                    var pt = m.GetParameters()[0].ParameterType;
                    if (pt.IsInstanceOfType(desc))
                    {
                        m.Invoke(null, new object[] { desc });
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    "Failed to load physics model " + modelDir + ": " + ex.Message);
            }
        }
    }

    private static object ParsePhysicsManifest(string yamlPath, string modelDir, Type? descriptorType)
    {
        var lines = File.ReadAllLines(yamlPath);
        string name = Path.GetFileName(modelDir);
        string fmiType = "CoSimulation";
        var inputs = new List<string>();
        var outputs = new List<string>();
        string section = "";

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.Length == 0 || line.TrimStart().StartsWith("#")) continue;
            if (!char.IsWhiteSpace(line[0]))
            {
                if (line.StartsWith("name:")) name = line.Substring(5).Trim().Trim('"', '\'');
                section = line.Split(':')[0];
                continue;
            }
            var t = line.TrimStart();
            if (section == "interface")
            {
                if (t.StartsWith("fmi_type:")) fmiType = t.Substring(9).Trim();
                else if (t.StartsWith("inputs:")) section = "inputs";
                else if (t.StartsWith("outputs:")) section = "outputs";
            }
            else if ((section == "inputs" || section == "outputs") && t.Contains(':'))
            {
                var key = t.Split(':')[0].Trim();
                (section == "inputs" ? inputs : outputs).Add(key);
            }
        }

        if (descriptorType == null)
            throw new InvalidOperationException("ComponentDescriptor type not found");

        var obj = Activator.CreateInstance(descriptorType)
            ?? throw new InvalidOperationException("Cannot instantiate ComponentDescriptor");
        SetProp(descriptorType, obj, "Id", "physics:" + name);
        SetProp(descriptorType, obj, "Name", name);
        SetProp(descriptorType, obj, "DisplayName", name);  // if it exists
        SetProp(descriptorType, obj, "Type", "Physics");
        SetProp(descriptorType, obj, "Category", "Physics");
        SetProp(descriptorType, obj, "PhysicsModelPath", modelDir);
        SetProp(descriptorType, obj, "FmiType", fmiType);
        // InputPorts / OutputPorts are string[] on ComponentDescriptor
        SetProp(descriptorType, obj, "InputPorts", inputs.ToArray());
        SetProp(descriptorType, obj, "OutputPorts", outputs.ToArray());
        return obj;
    }

    private static void SetProp(Type t, object obj, string name, object? value)
    {
        var p = t.GetProperty(name);
        if (p != null && p.CanWrite)
        {
            try { p.SetValue(obj, value); } catch { }
        }
    }

    private static Type? FindType(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var found = asm.GetTypes().FirstOrDefault(x => x.Name == name);
                if (found != null) return found;
            }
            catch { }
        }
        return null;
    }
}