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

    [Fact]
    public void CreateTwoComponentProject_PreservesConnections()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_2comp_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var createdPath = Path.Combine(tmpDir, "created.psbproj");
            var (exit1, log1) = RunHeadless("--test-create-two-comp-project", createdPath);
            Assert.True(File.Exists(createdPath), $"Create failed. log=\n{log1}");

            // Round-trip
            var roundtripPath = Path.Combine(tmpDir, "roundtrip.psbproj");
            var (exit2, log2) = RunHeadless("--test-load-save", createdPath, roundtripPath);
            Assert.True(File.Exists(roundtripPath), $"Round-trip failed. log=\n{log2}");

            // Verify original: 2 components, 1 connection
            using (var zip = ZipFile.OpenRead(createdPath))
            {
                var entry = zip.GetEntry("project.json");
                using var stream = entry!.Open();
                using var reader = new StreamReader(stream);
                var doc = JsonDocument.Parse(reader.ReadToEnd());

                var comps = doc.RootElement.GetProperty("components");
                Assert.Equal(2, comps.GetArrayLength());
                Assert.Equal("gain", comps[0].GetProperty("instanceName").GetString());
                Assert.Equal("first order lag", comps[1].GetProperty("instanceName").GetString());

                var conns = doc.RootElement.GetProperty("connections");
                Assert.Equal(1, conns.GetArrayLength());
                Assert.Equal("gain.output", conns[0].GetProperty("from").GetString());
                Assert.Equal("first order lag.input", conns[0].GetProperty("to").GetString());
            }

            // Verify round-trip preserves connections
            using (var zip = ZipFile.OpenRead(roundtripPath))
            {
                var entry = zip.GetEntry("project.json");
                using var stream = entry!.Open();
                using var reader = new StreamReader(stream);
                var doc = JsonDocument.Parse(reader.ReadToEnd());

                var comps = doc.RootElement.GetProperty("components");
                Assert.Equal(2, comps.GetArrayLength());

                var conns = doc.RootElement.GetProperty("connections");
                Assert.Equal(1, conns.GetArrayLength());
                Assert.Equal("gain.output", conns[0].GetProperty("from").GetString());
                Assert.Equal("first order lag.input", conns[0].GetProperty("to").GetString());
            }
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    [Fact]
    public void ParametersRoundTrip_PreservesValues()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_param_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var createdPath = Path.Combine(tmpDir, "created.psbproj");
            var (_, log1) = RunHeadless("--test-create-two-comp-project", createdPath);
            Assert.True(File.Exists(createdPath), $"Create failed. log=\n{log1}");

            var roundtripPath = Path.Combine(tmpDir, "roundtrip.psbproj");
            var (_, log2) = RunHeadless("--test-load-save", createdPath, roundtripPath);
            Assert.True(File.Exists(roundtripPath), $"Round-trip failed. log=\n{log2}");

            AssertParameters(createdPath);
            AssertParameters(roundtripPath);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    private static void AssertParameters(string archivePath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.GetEntry("project.json");
        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());

        var comps = doc.RootElement.GetProperty("components");
        Assert.Equal(2, comps.GetArrayLength());

        var gainParams = comps[0].GetProperty("parameters");
        Assert.True(gainParams.TryGetProperty("gain", out var gainVal));
        Assert.Equal(3.7, AsDouble(gainVal), 2);

        var lagParams = comps[1].GetProperty("parameters");
        Assert.True(lagParams.TryGetProperty("time_constant", out var tcVal));
        Assert.Equal(2.5, AsDouble(tcVal), 2);
        Assert.True(lagParams.TryGetProperty("initial_output", out var ioVal));
        Assert.Equal(0.5, AsDouble(ioVal), 2);
    }

    private static double AsDouble(JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number) return el.GetDouble();
        if (el.ValueKind == JsonValueKind.String)
            return double.Parse(el.GetString()!, System.Globalization.CultureInfo.InvariantCulture);
        return 0.0;
    }

    [Fact]
    public void LocationsRoundTrip_PreservesPositions()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_loc_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var createdPath = Path.Combine(tmpDir, "created.psbproj");
            var (_, log1) = RunHeadless("--test-create-two-comp-project", createdPath);
            Assert.True(File.Exists(createdPath), $"Create failed. log=\n{log1}");

            var roundtripPath = Path.Combine(tmpDir, "roundtrip.psbproj");
            // Use no-arrange variant: ArrangeComponents in load path resets positions,
            // which is a UI behavior separate from data fidelity.
            var (_, log2) = RunHeadless("--test-load-save-no-arrange", createdPath, roundtripPath);
            Assert.True(File.Exists(roundtripPath), $"Round-trip failed. log=\n{log2}");

            AssertLocations(createdPath);
            AssertLocations(roundtripPath);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    private static void AssertLocations(string archivePath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.GetEntry("project.json");
        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());

        var comps = doc.RootElement.GetProperty("components");
        var loc0 = comps[0].GetProperty("location");
        Assert.Equal(123, loc0.GetProperty("x").GetInt32());
        Assert.Equal(456, loc0.GetProperty("y").GetInt32());

        var loc1 = comps[1].GetProperty("location");
        Assert.Equal(789, loc1.GetProperty("x").GetInt32());
        Assert.Equal(321, loc1.GetProperty("y").GetInt32());
    }

    [Fact]
    public void ClosedLoopProject_PreservesTwoConnections()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_cl_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var createdPath = Path.Combine(tmpDir, "created.psbproj");
            var (_, log1) = RunHeadless("--test-create-closed-loop-project", createdPath);
            Assert.True(File.Exists(createdPath), $"Create failed. log=\n{log1}");

            var roundtripPath = Path.Combine(tmpDir, "roundtrip.psbproj");
            var (_, log2) = RunHeadless("--test-load-save", createdPath, roundtripPath);
            Assert.True(File.Exists(roundtripPath), $"Round-trip failed. log=\n{log2}");

            AssertClosedLoop(createdPath);
            AssertClosedLoop(roundtripPath);
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

    private static void AssertClosedLoop(string archivePath)
    {
        using var zip = ZipFile.OpenRead(archivePath);
        var entry = zip.GetEntry("project.json");
        using var stream = entry!.Open();
        using var reader = new StreamReader(stream);
        var doc = JsonDocument.Parse(reader.ReadToEnd());

        var comps = doc.RootElement.GetProperty("components");
        Assert.Equal(2, comps.GetArrayLength());
        Assert.Equal("controller", comps[0].GetProperty("instanceName").GetString());
        Assert.Equal("plant", comps[1].GetProperty("instanceName").GetString());

        var conns = doc.RootElement.GetProperty("connections");
        Assert.Equal(2, conns.GetArrayLength());

        var connList = new[] {
            (conns[0].GetProperty("from").GetString(), conns[0].GetProperty("to").GetString()),
            (conns[1].GetProperty("from").GetString(), conns[1].GetProperty("to").GetString()),
        };
        Assert.Contains(("controller.control", "plant.input"), connList);
        Assert.Contains(("plant.output", "controller.measurement"), connList);
    }

    [Fact]
    public void Archive_IncludesComponentsIfAvailable()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_zip_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var createdPath = Path.Combine(tmpDir, "created.psbproj");
            var (_, log1) = RunHeadless("--test-create-two-comp-project", createdPath);
            Assert.True(File.Exists(createdPath), $"Create failed. log=\n{log1}");

            using var zip = ZipFile.OpenRead(createdPath);
            var entries = zip.Entries.Select(e => e.FullName).ToList();
            var entryList = string.Join(", ", entries);

            Assert.Contains("project.json", entries);

            var componentPy = entries.Where(e => e.StartsWith("components/") && e.EndsWith(".py")).ToList();
            var generatedPy = entries.Where(e => e.StartsWith("generated/")).ToList();

            Console.WriteLine($"=== ZIP entries ({entries.Count}) ===");
            foreach (var e in entries) Console.WriteLine($"  {e}");
            Console.WriteLine($"components/*.py: {componentPy.Count}");
            Console.WriteLine($"generated/*: {generatedPy.Count}");

            if (generatedPy.Count > 0)
            {
                Assert.True(componentPy.Count > 0,
                    $"generated/ has files but components/ is empty: {entryList}");
            }
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
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