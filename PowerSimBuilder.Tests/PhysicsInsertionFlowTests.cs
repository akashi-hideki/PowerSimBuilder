#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsInsertionFlowTests
{
    private const string Template =
@"class Foo:
    def __init__(self):
        self.x = 0

    def update(self, t, dt):
        pass
";

    private static string WriteFmu(string dir, string modelName,
        string[] inputs, string[] outputs)
    {
        var sourceDir = Path.Combine(dir, "source");
        Directory.CreateDirectory(sourceDir);
        var fmuPath = Path.Combine(sourceDir, "model.fmu");
        if (File.Exists(fmuPath)) File.Delete(fmuPath);

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

        using var fs = new FileStream(fmuPath, FileMode.Create, FileAccess.Write);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("modelDescription.xml");
        using var es = entry.Open();
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        es.Write(bytes, 0, bytes.Length);

        return fmuPath;
    }

    private static string CreateTempDir()
    {
        var p = Path.Combine(Path.GetTempPath(), "psb_flow_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(p);
        return p;
    }

    private static PhysicsModelEntry BuildEntry(string dir, string modelName,
        params string[] inputs)
    {
        var fmu = WriteFmu(dir, modelName, inputs, new[] { "y" });
        var info = FmuInfoReader.Read(fmu);
        var relPath = Path.GetFileName(dir) + "/source/model.fmu";
        return new PhysicsModelEntry(modelName, dir, fmu, relPath, info);
    }

    [Theory]
    [InlineData("ControllerBox", false)]
    [InlineData("FirstOrderLag", true)]
    [InlineData("PMSM", true)]
    [InlineData("controllerbox", true)] // case-sensitive on purpose
    [InlineData(null, true)]
    public void IsPhysicsTarget_MatchesControllerBoxOnly(string? type, bool expected)
    {
        Assert.Equal(expected, PhysicsInsertionFlow.IsPhysicsTarget(type));
    }

    [Fact]
    public void TryInsert_ValidEntry_ReturnsInserted()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildEntry(Path.Combine(dir, "PMSM"), "PMSM", "u");

            var r = PhysicsInsertionFlow.TryInsert(Template, entry);

            Assert.True(r.Succeeded);
            Assert.Equal(InsertOutcome.Inserted, r.Outcome);
            Assert.NotNull(r.NewCode);
            Assert.Contains("# === physics: PMSM (auto-inserted) ===", r.NewCode);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TryInsert_AlreadyExists_ReportsOutcome()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildEntry(Path.Combine(dir, "PMSM"), "PMSM", "u");
            var first = PhysicsInsertionFlow.TryInsert(Template, entry);
            Assert.True(first.Succeeded);

            var second = PhysicsInsertionFlow.TryInsert(first.NewCode, entry);

            Assert.False(second.Succeeded);
            Assert.Equal(InsertOutcome.AlreadyExists, second.Outcome);
            Assert.Null(second.ErrorMessage);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TryInsert_NullCode_Fails()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildEntry(Path.Combine(dir, "PMSM"), "PMSM", "u");
            var r = PhysicsInsertionFlow.TryInsert(null, entry);
            Assert.False(r.Succeeded);
            Assert.NotNull(r.ErrorMessage);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TryInsert_NullEntry_Fails()
    {
        var r = PhysicsInsertionFlow.TryInsert(Template, null);
        Assert.False(r.Succeeded);
        Assert.NotNull(r.ErrorMessage);
    }

    [Fact]
    public void TryInsert_MissingFmu_Fails()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildEntry(Path.Combine(dir, "PMSM"), "PMSM", "u");
            File.Delete(entry.FmuPath);

            var r = PhysicsInsertionFlow.TryInsert(Template, entry);

            Assert.False(r.Succeeded);
            Assert.NotNull(r.ErrorMessage);
            Assert.Contains("FMU file not found", r.ErrorMessage!);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TryInsert_ModelExchange_Fails()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildEntry(Path.Combine(dir, "PMSM"), "PMSM", "u");
            var meInfo = entry.Info with { FmiType = "ModelExchange" };
            var meEntry = entry with { Info = meInfo };

            var r = PhysicsInsertionFlow.TryInsert(Template, meEntry);

            Assert.False(r.Succeeded);
            Assert.Contains("CoSimulation", r.ErrorMessage!);
        }
        finally { Directory.Delete(dir, true); }
    }

    [Fact]
    public void TryInsert_NoInit_Fails()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildEntry(Path.Combine(dir, "PMSM"), "PMSM", "u");
            const string noInit = "class Bar:\n    pass\n";

            var r = PhysicsInsertionFlow.TryInsert(noInit, entry);

            Assert.False(r.Succeeded);
            Assert.Equal(InsertOutcome.NoInitFound, r.Outcome);
        }
        finally { Directory.Delete(dir, true); }
    }
}