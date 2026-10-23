#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PowerSimBuilder.Physics;

public sealed record PhysicsModelEntry(
    string DisplayName,
    string ModelDirectory,
    string FmuPath,
    string FmuRelativePath,
    FmuInfo Info);

public static class PhysicsModelCatalog
{
    public static string DefaultRoot()
    {
        var cwd = Directory.GetCurrentDirectory();
        if (Directory.Exists(Path.Combine(cwd, ".git")) ||
            File.Exists(Path.Combine(cwd, "PowerSimBuilder.csproj")))
            return Path.Combine(cwd, "user_physics_models");
        return Path.Combine(AppContext.BaseDirectory, "user_physics_models");
    }

    public static IReadOnlyList<PhysicsModelEntry> Scan(string userPhysicsRoot)
    {
        var result = new List<PhysicsModelEntry>();
        if (string.IsNullOrWhiteSpace(userPhysicsRoot)) return result;
        if (!Directory.Exists(userPhysicsRoot)) return result;

        var rootFull = Path.GetFullPath(userPhysicsRoot);

        foreach (var dir in Directory.EnumerateDirectories(rootFull))
        {
            var sourceDir = Path.Combine(dir, "source");
            if (!Directory.Exists(sourceDir)) continue;
            var fmuFiles = Directory.EnumerateFiles(sourceDir, "*.fmu").ToList();
            if (fmuFiles.Count == 0) continue;

            foreach (var fmu in fmuFiles)
            {
                FmuInfo info;
                try { info = FmuInfoReader.Read(fmu); }
                catch { continue; }

                var modelDirName = Path.GetFileName(dir);
                var display = string.IsNullOrEmpty(info.ModelName)
                    ? modelDirName
                    : info.ModelName + " (" + modelDirName + ")";

                var relDir = Path.GetRelativePath(rootFull, dir).Replace('\\', '/');
                var relFmu = relDir + "/source/" + Path.GetFileName(fmu);

                result.Add(new PhysicsModelEntry(
                    display, dir, Path.GetFullPath(fmu), relFmu, info));
            }
        }

        return result.OrderBy(e => e.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
    }
}