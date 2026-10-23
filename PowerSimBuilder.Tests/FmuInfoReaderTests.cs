#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class FmuInfoReaderTests
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

    private static string? RefFmu(string name)
    {
        var path = Path.Combine(RepoRoot(), "tmp", "Reference-FMUs", "2.0", name + ".fmu");
        return File.Exists(path) ? path : null;
    }

    [Fact]
    public void Reads_Dahlquist()
    {
        var path = RefFmu("Dahlquist");
        if (path == null) return;

        var info = FmuInfoReader.Read(path);

        Assert.Equal("Dahlquist", info.ModelName);
        Assert.Equal("2.0", info.FmiVersion);
        Assert.Equal("CoSimulation", info.FmiType);
        Assert.Empty(info.Inputs);
        Assert.NotEmpty(info.Outputs);
        Assert.NotNull(info.OutputTypes);
    }

    [Fact]
    public void Reads_Feedthrough()
    {
        var path = RefFmu("Feedthrough");
        if (path == null) return;

        var info = FmuInfoReader.Read(path);

        Assert.Equal("Feedthrough", info.ModelName);
        Assert.NotEmpty(info.Inputs);
        Assert.NotEmpty(info.Outputs);
        Assert.NotNull(info.InputTypes);
        Assert.NotNull(info.OutputTypes);
    }

    [Fact]
    public void Reads_Types_FromSyntheticFmu()
    {
        var path = Path.Combine(Path.GetTempPath(),
            "psb_types_" + Guid.NewGuid().ToString("N") + ".fmu");
        try
        {
            var xml =
                "<fmiModelDescription fmiVersion=\"2.0\" fmiType=\"CoSimulation\" modelName=\"Types\">\n" +
                "  <ModelVariables>\n" +
                "    <ScalarVariable name=\"r_in\" valueReference=\"1\" causality=\"input\"><Real start=\"0\"/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"i_in\" valueReference=\"2\" causality=\"input\"><Integer start=\"0\"/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"b_in\" valueReference=\"3\" causality=\"input\"><Boolean start=\"false\"/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"s_in\" valueReference=\"4\" causality=\"input\"><String start=\"\"/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"r_out\" valueReference=\"5\" causality=\"output\"><Real/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"i_out\" valueReference=\"6\" causality=\"output\"><Integer/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"p_real\" valueReference=\"7\" causality=\"parameter\"><Real start=\"1.5\"/></ScalarVariable>\n" +
                "    <ScalarVariable name=\"p_int\"  valueReference=\"8\" causality=\"parameter\"><Integer start=\"7\"/></ScalarVariable>\n" +
                "  </ModelVariables>\n" +
                "</fmiModelDescription>\n";
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
            {
                var e = zip.CreateEntry("modelDescription.xml");
                using var es = e.Open();
                var bytes = Encoding.UTF8.GetBytes(xml);
                es.Write(bytes, 0, bytes.Length);
            }

            var info = FmuInfoReader.Read(path);

            Assert.Equal("Real",    info.InputTypes!["r_in"]);
            Assert.Equal("Integer", info.InputTypes!["i_in"]);
            Assert.Equal("Boolean", info.InputTypes!["b_in"]);
            Assert.Equal("String",  info.InputTypes!["s_in"]);
            Assert.Equal("Real",    info.OutputTypes!["r_out"]);
            Assert.Equal("Integer", info.OutputTypes!["i_out"]);

            Assert.Contains(info.Parameters, p => p.Name == "p_real" && p.Type == "Real"    && p.StartValue == "1.5");
            Assert.Contains(info.Parameters, p => p.Name == "p_int"  && p.Type == "Integer" && p.StartValue == "7");
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }
}