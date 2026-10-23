#nullable enable
using System.Collections.Generic;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsSnippetBuilderMappingTests
{
    private static FmuInfo SampleInfo() => new FmuInfo(
        ModelName: "PMSM",
        FmiVersion: "2.0",
        FmiType: "CoSimulation",
        Inputs: new[] { "u_d", "u_q" },
        Outputs: new[] { "i_d" },
        Parameters: new List<FmuParameter>(),
        Timing: new FmuTiming(0.0, 1.0, 0.001),
        InputTypes: new Dictionary<string, string>
        {
            ["u_d"] = "Real",
            ["u_q"] = "Real",
        },
        OutputTypes: new Dictionary<string, string> { ["i_d"] = "Real" });

    private static PhysicsPortMapping SampleMapping() => new PhysicsPortMapping(
        new Dictionary<string, string> { ["u_d"] = "u_d_input" },
        new Dictionary<string, string> { ["i_d"] = "i_d_output" });

    [Fact]
    public void UpdateBlock_UsesComponentPortNames_ForMappedInputs()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(
            SampleInfo(), mapping: SampleMapping());

        Assert.Contains("self.pmsm.set(\"u_d\", float(self.inputs.get(\"u_d_input\", 0.0)))", code);
        // Unmapped input keeps FMU name
        Assert.Contains("self.pmsm.set(\"u_q\", float(self.inputs.get(\"u_q\", 0.0)))", code);
    }

    [Fact]
    public void UpdateBlock_UsesComponentPortNames_ForMappedOutputs()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(
            SampleInfo(), mapping: SampleMapping());

        Assert.Contains("self.outputs[\"i_d_output\"] = float(self.pmsm.get(\"i_d\"))", code);
    }

    [Fact]
    public void UpdateBlock_EmitsTimingWarning_WhenStepSizePresent()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(SampleInfo());

        Assert.Contains("# timing check: FMU stepSize=0.001", code);
        Assert.Contains("if abs(dt - 0.001) > 1e-9:", code);
        Assert.Contains("warnings.warn(", code);
        Assert.Contains("RuntimeWarning", code);
    }

    [Fact]
    public void UpdateBlock_NoTiming_NoWarning()
    {
        var info = new FmuInfo(
            ModelName: "Dahlquist",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new string[0],
            Outputs: new[] { "x" },
            Parameters: new List<FmuParameter>(),
            Timing: null);

        var code = PhysicsSnippetBuilder.BuildUpdateBlock(info);

        Assert.DoesNotContain("warnings.warn", code);
    }

    [Fact]
    public void InsertionBlock_ShowsArrowForMappedPorts()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/model.fmu", mapping: SampleMapping());

        Assert.Contains("# inputs    : u_d:Real->u_d_input, u_q:Real", code);
        Assert.Contains("# outputs   : i_d:Real->i_d_output", code);
    }
}