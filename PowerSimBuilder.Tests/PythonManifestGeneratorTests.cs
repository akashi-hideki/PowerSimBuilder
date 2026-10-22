#nullable enable
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PythonManifestGeneratorTests
{
    [Fact]
    public void BuildReadme_ContainsHeaderAndPorts()
    {
        var vars = new List<FmuVariable>
        {
            new("V_dc", 1, "input", "Real"),
            new("omega_m", 2, "output", "Real"),
        };
        var md = PythonManifestGenerator.BuildReadme(
            "PMSM", @"C:\tmp\PMSM", vars, "CoSimulation");

        Assert.Contains("# PMSM (Physics Model)", md);
        Assert.Contains("| input | V_dc | V_dc |", md);
        Assert.Contains("| output | omega_m | omega_m |", md);
        Assert.Contains("fmu.inputs[\"V_dc\"] = 1.0", md);
    }

    [Fact]
    public void BuildReadme_HandlesNoPorts()
    {
        var vars = new List<FmuVariable>();
        var md = PythonManifestGenerator.BuildReadme(
            "Empty", @"C:\tmp\Empty", vars, "CoSimulation");

        Assert.Contains("| _(none)_ |", md);
        Assert.Contains("# no inputs defined", md);
        Assert.Contains("no outputs defined", md);
    }

    [Fact]
    public void BuildPythonSample_ContainsLoadAndStep()
    {
        var vars = new List<FmuVariable>
        {
            new("u", 1, "input", "Real"),
            new("y", 2, "output", "Real"),
        };
        var sample = PythonManifestGenerator.BuildPythonSample(@"C:\tmp\Demo", vars);

        Assert.Contains("from python_runtime.physics import load_manifest, FmuAdapter", sample);
        Assert.Contains("manifest = load_manifest(r\"C:\\tmp\\Demo\\model.yaml\")", sample);
        Assert.Contains("fmu.step(0.01)", sample);
        Assert.Contains("fmu.close()", sample);
    }

    [Fact]
    public void BuildReadme_RejectsEmptyName()
    {
        var vars = new List<FmuVariable>();
        Assert.Throws<System.ArgumentException>(() =>
            PythonManifestGenerator.BuildReadme("", @"C:\tmp", vars, "CoSimulation"));
    }
}