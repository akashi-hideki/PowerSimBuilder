using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsImportFormTests
{
    private static string MakeMinimalFmu(string path, string fmiType = "CoSimulation")
    {
        var xml =
            "<fmiModelDescription fmiVersion=\"2.0\" fmiType=\"" + fmiType + "\">" +
            "<ModelVariables>" +
            "<ScalarVariable name=\"V_dc\" valueReference=\"1\" causality=\"input\"><Real/></ScalarVariable>" +
            "<ScalarVariable name=\"omega_m\" valueReference=\"2\" causality=\"output\"><Real/></ScalarVariable>" +
            "<ScalarVariable name=\"state_x\" valueReference=\"3\" causality=\"local\"><Real/></ScalarVariable>" +
            "</ModelVariables>" +
            "</fmiModelDescription>";
        using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        var entry = zip.CreateEntry("modelDescription.xml");
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write(xml);
        return path;
    }

    [Fact]
    public void ParseFmu_ExtractsVariables()
    {
        var tmp = Path.GetTempFileName() + ".fmu";
        try
        {
            MakeMinimalFmu(tmp);
            var r = PhysicsImportForm.ParseFmu(tmp);
            Assert.Equal("CoSimulation", r.FmiType);
            Assert.Equal(3, r.Variables.Count);
            Assert.Contains(r.Variables, v => v.Name == "V_dc" && v.Causality == "input");
        }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void ParseFmu_RejectsNonFmu()
    {
        var tmp = Path.GetTempFileName();
        File.WriteAllText(tmp, "not a zip");
        try { Assert.ThrowsAny<Exception>(() => PhysicsImportForm.ParseFmu(tmp)); }
        finally { File.Delete(tmp); }
    }

    [Fact]
    public void BuildModelYaml_ProducesExpectedSkeleton()
    {
        var vars = new[]
        {
            new FmuVariable("V_dc", 1, "input", "Real"),
            new FmuVariable("omega_m", 2, "output", "Real"),
        };
        var yaml = PhysicsImportForm.BuildModelYaml("PMSM", "PMSM.fmu", "CoSimulation", vars);
        Assert.Contains("name: PMSM", yaml);
        Assert.Contains("file: source/PMSM.fmu", yaml);
        Assert.Contains("fmi_type: CoSimulation", yaml);
        Assert.Contains("V_dc: V_dc", yaml);
        Assert.Contains("omega_m: omega_m", yaml);
        Assert.DoesNotContain("state_x", yaml);
    }

    [Fact]
    public void BuildModelYaml_RejectsEmptyName()
    {
        Assert.Throws<ArgumentException>(() =>
            PhysicsImportForm.BuildModelYaml("  ", "x.fmu", "CoSimulation",
                new[] { new FmuVariable("u", 1, "input", "Real") }));
    }
}