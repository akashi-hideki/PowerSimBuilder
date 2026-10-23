#nullable enable
using System.Collections.Generic;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsSnippetBuilderModelInfoTests
{
    private static FmuInfo SampleInfo() => new FmuInfo(
        ModelName: "PMSM",
        FmiVersion: "2.0",
        FmiType: "CoSimulation",
        Inputs: new[] { "u" },
        Outputs: new[] { "y" },
        Parameters: new List<FmuParameter>(),
        Timing: null);

    [Fact]
    public void InsertionBlock_HasModelAndFmuFileLines()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/source/model.fmu");

        Assert.Contains("# model     : PMSM", code);
        Assert.Contains("# fmu_file  : model.fmu", code);
    }

    [Fact]
    public void InsertionBlock_FmuFileHandlesNestedPath()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/v2/source/my_model.fmu");

        Assert.Contains("# fmu_file  : my_model.fmu", code);
    }
}