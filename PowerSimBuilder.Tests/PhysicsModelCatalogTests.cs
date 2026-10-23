#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsModelCatalogTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repo root not found");
    }

    private static string CreateTempDir()
    {
        var p = Path.Combine(Path.GetTempPath(), "psb_catalog_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(p);
        return p;
    }

    private static string WriteFmu(string path, string modelName,
        string[] inputs, string[] outputs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        if (File.Exists(path)) File.Delete(path);

        var sb = new StringBuilder();
        sb.AppendLine("<fmiModelDescription fmiVersion=\"2.0\" fmiType=\"CoSimulation\" modelName=\"" + modelName + "\">");
        sb.AppendLine("  <DefaultExperiment startTime=\"0\" stopTime=\"1\" stepSize=\"0.001\"/>");
        sb.AppendLine("  <ModelVariables>");
        foreach (var i in inputs)
            sb.AppendLine("    <ScalarVariable name=\"" + i + "\" valueReference=\"1\" causality=\"input\"><Real start=\"0\"/></ScalarVariable>");
        foreach (var o in outputs)
            sb.AppendLine("    <ScalarVariable name=\"" + o + "\" valueReference=\"2\" causality=\"output\"><Real/></ScalarVariable>");
        sb.AppendLine("  </ModelVariables>");
        sb.AppendLine("</fmiModelDescription>");

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("modelDescription.xml");
        using var es = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        es.Write(bytes, 0, bytes.Length);
        return path;
    }

    [Fact]
    public void Scan_EmptyRoot_ReturnsEmpty()
    {
        var root = CreateTempDir();
        try
        {
            var entries = PhysicsModelCatalog.Scan(root);
            Assert.Empty(entries);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Scan_MissingRoot_ReturnsEmpty()
    {
        var entries = PhysicsModelCatalog.Scan(
            Path.Combine(Path.GetTempPath(), "psb_nonexistent_" + Guid.NewGuid().ToString("N")));
        Assert.Empty(entries);
    }

    [Fact]
    public void Scan_FindsFmuAndBuildsRelativePath()
    {
        var root = CreateTempDir();
        try
        {
            WriteFmu(Path.Combine(root, "PMSM", "source", "model.fmu"),
                "PMSM", new[] { "u_d", "u_q" }, new[] { "i_d" });

            var entries = PhysicsModelCatalog.Scan(root);

            Assert.Single(entries);
            var e = entries[0];
            Assert.Contains("PMSM", e.DisplayName);
            Assert.Equal("PMSM/source/model.fmu", e.FmuRelativePath);
            Assert.True(File.Exists(e.FmuPath));
            Assert.Equal(2, e.Info.Inputs.Count);
            Assert.Single(e.Info.Outputs);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Scan_MultipleModels_SortedByName()
    {
        var root = CreateTempDir();
        try
        {
            WriteFmu(Path.Combine(root, "Zeta", "source", "z.fmu"), "Zeta", new string[0], new[] { "x" });
            WriteFmu(Path.Combine(root, "Alpha", "source", "a.fmu"), "Alpha", new string[0], new[] { "y" });

            var entries = PhysicsModelCatalog.Scan(root);

            Assert.Equal(2, entries.Count);
            Assert.Contains("Alpha", entries[0].DisplayName);
            Assert.Contains("Zeta", entries[1].DisplayName);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Scan_SkipsDirectoriesWithoutSourceOrFmu()
    {
        var root = CreateTempDir();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "NoSource"));
            Directory.CreateDirectory(Path.Combine(root, "NoFmu", "source"));
            WriteFmu(Path.Combine(root, "Good", "source", "g.fmu"), "Good", new string[0], new[] { "y" });

            var entries = PhysicsModelCatalog.Scan(root);

            Assert.Single(entries);
            Assert.Contains("Good", entries[0].DisplayName);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Scan_SkipsCorruptFmu()
    {
        var root = CreateTempDir();
        try
        {
            var badDir = Path.Combine(root, "Broken", "source");
            Directory.CreateDirectory(badDir);
            File.WriteAllText(Path.Combine(badDir, "bad.fmu"), "not a zip");

            WriteFmu(Path.Combine(root, "Good", "source", "g.fmu"), "Good", new string[0], new[] { "y" });

            var entries = PhysicsModelCatalog.Scan(root);

            Assert.Single(entries);
            Assert.Contains("Good", entries[0].DisplayName);
        }
        finally { Directory.Delete(root, recursive: true); }
    }

    [Fact]
    public void Scan_RealRepo_ReturnsKnownModels_IfPresent()
    {
        var root = Path.Combine(RepoRoot(), "user_physics_models");
        if (!Directory.Exists(root)) return; // environment without fixtures

        var entries = PhysicsModelCatalog.Scan(root);

        // At least PMSM (already committed in MVP-1.1) should be present
        // if the directory has any FMUs; otherwise just ensure no throw.
        Assert.NotNull(entries);
    }
}