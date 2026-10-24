using System;
using System.IO;

namespace PowerSimBuilder.UITests;

/// <summary>
/// Resolves paths to the built PowerSimBuilder.exe and test fixtures.
/// Set PSB_APP_EXE env var to override (used by CI).
/// </summary>
public static class TestPaths
{
    public static string AppExePath
    {
        get
        {
            var env = Environment.GetEnvironmentVariable("PSB_APP_EXE");
            if (!string.IsNullOrEmpty(env) && File.Exists(env))
                return env;

            // 1. Copied to test bin by ProjectReference
            var local = Path.Combine(AppContext.BaseDirectory, "PowerSimBuilder.exe");
            if (File.Exists(local))
                return local;

            // 2. Fallback: locate repo root and use bin/<Config>/net10.0-windows
            var config = AppContext.BaseDirectory.Contains("Release") ? "Release" : "Debug";
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                dir = dir.Parent;

            if (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "bin", config, "net10.0-windows", "PowerSimBuilder.exe");
                if (File.Exists(candidate))
                    return candidate;
            }

            throw new FileNotFoundException(
                "PowerSimBuilder.exe not found. Build the solution first or set PSB_APP_EXE.");
        }
    }

    public static string ProjectRoot
    {
        get
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                dir = dir.Parent;
            return dir?.FullName ?? throw new InvalidOperationException("Project root not found.");
        }
    }

    public static string TestFixturePath(string name)
        => Path.Combine(ProjectRoot, "tests", "PowerSimBuilder.UITests", "Fixtures", name);
}
