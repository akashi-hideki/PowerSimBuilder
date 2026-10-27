using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Xunit;

namespace PowerSimBuilder.UITests;

[Trait("Category", "UI")]
public sealed class ProjectRoundTripTests
{
    private static string FixturePath =>
        TestPaths.TestFixturePath("fmu_project.psbproj");

    [Fact]
    public void Fixture_Exists_And_HasFmuInfo()
    {
        Assert.True(File.Exists(FixturePath),
            $"Fixture missing: {FixturePath}. Run tmp\\create_fixture_psbproj.py first.");

        using var zip = ZipFile.OpenRead(FixturePath);
        var entry = zip.GetEntry("project.json");
        Assert.NotNull(entry);

        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());

        Assert.Equal("2.0", doc.RootElement.GetProperty("schemaVersion").GetString());
        var comps = doc.RootElement.GetProperty("components");
        Assert.True(comps.GetArrayLength() > 0);

        var comp0 = comps[0];
        var physics = comp0.GetProperty("physicsModelPath").GetString();
        Assert.False(string.IsNullOrEmpty(physics), "physicsModelPath must not be empty");
        Assert.Contains("Dahlquist", physics);
        Assert.Equal("CoSimulation", comp0.GetProperty("fmiType").GetString());
    }

    [Fact]
    public void Load_Fixture_Then_Save_PreservesFmuInfo()
    {
        Assert.True(File.Exists(FixturePath), "Fixture missing");

        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_rt_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var outputPath = Path.Combine(tmpDir, "roundtrip.psbproj");
            var (exitCode, log) = RunHeadless("--test-load-save", FixturePath, outputPath);

            Assert.True(File.Exists(outputPath),
                $"Archive not created. exit={exitCode} log=\n{log}");
            Assert.Contains("LOAD_SAVE_OK", log);

            using var zip = ZipFile.OpenRead(outputPath);
            var entry = zip.GetEntry("project.json");
            Assert.NotNull(entry);

            using var stream = entry!.Open();
            using var reader = new StreamReader(stream);
            var doc = JsonDocument.Parse(reader.ReadToEnd());

            var comps = doc.RootElement.GetProperty("components");
            Assert.True(comps.GetArrayLength() > 0, "Components must be preserved");

            var comp0 = comps[0];
            Assert.Equal("plant", comp0.GetProperty("instanceName").GetString());

            var physics = comp0.GetProperty("physicsModelPath").GetString();
            Assert.False(string.IsNullOrEmpty(physics), "physicsModelPath must be preserved");
            Assert.Contains("Dahlquist", physics);
            Assert.Equal("CoSimulation", comp0.GetProperty("fmiType").GetString());
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void DoubleRoundTrip_IsIdempotent()
    {
        Assert.True(File.Exists(FixturePath), "Fixture missing");

        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_rt2_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var path1 = Path.Combine(tmpDir, "rt1.psbproj");
            var path2 = Path.Combine(tmpDir, "rt2.psbproj");

            var (exit1, log1) = RunHeadless("--test-load-save", FixturePath, path1);
            Assert.True(File.Exists(path1), $"First round-trip failed. exit={exit1} log=\n{log1}");

            var (exit2, log2) = RunHeadless("--test-load-save", path1, path2);
            Assert.True(File.Exists(path2), $"Second round-trip failed. exit={exit2} log=\n{log2}");

            Assert.Equal(ExtractCoreJson(path1), ExtractCoreJson(path2));
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    private static string ExtractCoreJson(string archivePath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.GetEntry("project.json");
        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());

        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();
        writer.WriteString("schemaVersion", doc.RootElement.GetProperty("schemaVersion").GetString());
        writer.WriteStartArray("components");
        foreach (var c in doc.RootElement.GetProperty("components").EnumerateArray())
        {
            writer.WriteStartObject();
            writer.WriteString("instanceName", c.GetProperty("instanceName").GetString());
            writer.WriteString("physicsModelPath", c.GetProperty("physicsModelPath").GetString());
            writer.WriteString("fmiType", c.GetProperty("fmiType").GetString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    [Fact]
    public void CreateFmuProject_SavesFmuInfo()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_create_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var outputPath = Path.Combine(tmpDir, "created.psbproj");
            var (exitCode, log) = RunHeadless("--test-create-fmu-project", outputPath);

            Assert.True(File.Exists(outputPath),
                $"Archive not created. exit={exitCode} log=\n{log}");
            Assert.Contains("CREATE_SAVE_OK", log);

            using var zip = ZipFile.OpenRead(outputPath);
            var entry = zip.GetEntry("project.json");
            Assert.NotNull(entry);

            using var stream = entry!.Open();
            using var reader = new StreamReader(stream);
            var doc = JsonDocument.Parse(reader.ReadToEnd());

            var comps = doc.RootElement.GetProperty("components");
            Assert.Equal(1, comps.GetArrayLength());

            var comp0 = comps[0];
            var instanceName = comp0.GetProperty("instanceName").GetString();
            Assert.False(string.IsNullOrEmpty(instanceName), "instanceName must be set");

            var physics = comp0.GetProperty("physicsModelPath").GetString();
            Assert.False(string.IsNullOrEmpty(physics), "physicsModelPath must be set");
            Assert.Contains("Dahlquist", physics);
            Assert.Equal("CoSimulation", comp0.GetProperty("fmiType").GetString());
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void CreateFmuProject_ThenRoundTrip_PreservesPhysics()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_create_rt_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var createdPath = Path.Combine(tmpDir, "created.psbproj");
            var (exit1, log1) = RunHeadless("--test-create-fmu-project", createdPath);
            Assert.True(File.Exists(createdPath), $"Create failed. log=\n{log1}");

            var roundtripPath = Path.Combine(tmpDir, "roundtrip.psbproj");
            var (exit2, log2) = RunHeadless("--test-load-save", createdPath, roundtripPath);
            Assert.True(File.Exists(roundtripPath), $"Round-trip failed. log=\n{log2}");

            Assert.Equal(
                ExtractPhysicsInfo(createdPath),
                ExtractPhysicsInfo(roundtripPath));
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    private static string ExtractPhysicsInfo(string archivePath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.GetEntry("project.json");
        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());

        using var ms = new MemoryStream();
        using var writer = new Utf8JsonWriter(ms, new JsonWriterOptions { Indented = true });
        writer.WriteStartArray();
        foreach (var c in doc.RootElement.GetProperty("components").EnumerateArray())
        {
            writer.WriteStartObject();
            writer.WriteString("instanceName", c.GetProperty("instanceName").GetString());
            writer.WriteString("physicsModelPath", c.GetProperty("physicsModelPath").GetString());
            writer.WriteString("fmiType", c.GetProperty("fmiType").GetString());
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.Flush();
        return System.Text.Encoding.UTF8.GetString(ms.ToArray());
    }

    private static (int ExitCode, string Log) RunHeadless(params string[] args)
    {
        var logPath = Path.Combine(Path.GetTempPath(), "PowerSimBuilder_TestSave.log");
        try { File.Delete(logPath); } catch { }

        var psi = new ProcessStartInfo
        {
            FileName = TestPaths.AppExePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath(),
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi);
        if (proc == null) throw new InvalidOperationException("Failed to start exe");

        if (!proc.WaitForExit(60_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            return (-1, "timeout");
        }

        string log = "";
        try { log = File.ReadAllText(logPath); } catch { }

        return (proc.ExitCode, log);
    }
}