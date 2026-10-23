#nullable enable
using System.Collections.Generic;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsSnippetBuilderTests
{
    private static FmuInfo SampleInfo() => new FmuInfo(
        ModelName: "PMSM",
        FmiVersion: "2.0",
        FmiType: "CoSimulation",
        Inputs: new[] { "u_d", "u_q" },
        Outputs: new[] { "i_d", "i_q", "torque" },
        Parameters: new List<FmuParameter>
        {
            new("R", "0.5"),
            new("Ld", "0.001"),
        },
        Timing: new FmuTiming(0.0, 1.0, 0.001));

    [Fact]
    public void Helper_ContainsFunctionAndSearchLogic()
    {
        var code = PhysicsSnippetBuilder.BuildHelper();

        Assert.Contains("def " + PhysicsSnippetBuilder.HelperFunctionName, code);
        Assert.Contains("user_physics_models", code);
        Assert.Contains("Path(__file__)", code);
        Assert.Contains("FileNotFoundError", code);
    }

    [Fact]
    public void InsertionBlock_HasMarkersCommentsAndLoadFmu()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/model.fmu");

        Assert.Contains("# === physics: PMSM (auto-inserted) ===", code);
        Assert.Contains("# === /physics: PMSM ===", code);
        Assert.Contains("# source    : user_physics_models/PMSM/model.fmu", code);
        Assert.Contains("# fmi       : 2.0 (CoSimulation)", code);
        Assert.Contains("# inputs    : u_d, u_q", code);
        Assert.Contains("# outputs   : i_d, i_q, torque", code);
        Assert.Contains("# parameters: R=0.5, Ld=0.001", code);
        Assert.Contains("# timing    : start=0, stop=1, step=0.001", code);
        Assert.Contains("self.pmsm = load_fmu(" + PhysicsSnippetBuilder.HelperFunctionName, code);
        Assert.Contains("self.pmsm.set(\"R\", 0.5)", code);
        Assert.Contains("self.pmsm.set(\"Ld\", 0.001)", code);
    }

    [Fact]
    public void InsertionBlock_HasEnterExitInitializationMode()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/model.fmu");

        Assert.Contains("self.pmsm.enter_initialization_mode()", code);
        Assert.Contains("self.pmsm.exit_initialization_mode()", code);
        Assert.Contains("self._pmsm_time = 0.0", code);
    }

    [Fact]
    public void InsertionBlock_UsesCustomIndent()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/model.fmu", "    ");

        Assert.Contains("    # === physics: PMSM (auto-inserted) ===", code);
        Assert.Contains("    self.pmsm = load_fmu(", code);
    }

    [Fact]
    public void InsertionBlock_NonRealParameterBecomesTodo()
    {
        var info = SampleInfo() with
        {
            Parameters = new List<FmuParameter>
            {
                new("Mode", "# TODO"),
            }
        };

        var code = PhysicsSnippetBuilder.BuildInsertionBlock(info, "X/model.fmu");

        Assert.Contains("# TODO: set(\"Mode\", <value>)", code);
        Assert.DoesNotContain("self.x.set(\"Mode\"", code);
    }

    [Fact]
    public void InsertionBlock_NoParametersNoTiming()
    {
        var info = new FmuInfo(
            ModelName: "Dahlquist",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new string[0],
            Outputs: new[] { "x" },
            Parameters: new List<FmuParameter>(),
            Timing: null);

        var code = PhysicsSnippetBuilder.BuildInsertionBlock(info, "Dahlquist/model.fmu");

        Assert.Contains("# parameters: (none)", code);
        Assert.Contains("# timing    : (none)", code);
        Assert.DoesNotContain(".set(", code);
    }

    [Fact]
    public void UpdateBlock_HasMarkersAndStepLogic()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(SampleInfo());

        Assert.Contains("# === physics: PMSM (auto-inserted in update) ===", code);
        Assert.Contains("# === /physics: PMSM ===", code);
        Assert.Contains("self.pmsm.set(\"u_d\", self.inputs.get(\"u_d\", 0.0))", code);
        Assert.Contains("self.pmsm.set(\"u_q\", self.inputs.get(\"u_q\", 0.0))", code);
        Assert.Contains("self.pmsm.doStep(self._pmsm_time, dt)", code);
        Assert.Contains("self._pmsm_time += dt", code);
        Assert.Contains("self.outputs[\"i_d\"] = self.pmsm.get(\"i_d\")", code);
        Assert.Contains("self.outputs[\"i_q\"] = self.pmsm.get(\"i_q\")", code);
        Assert.Contains("self.outputs[\"torque\"] = self.pmsm.get(\"torque\")", code);
    }

    [Fact]
    public void UpdateBlock_NoInputsOutputs_StillSteps()
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

        Assert.DoesNotContain(".set(", code);
        Assert.Contains("self.dahlquist.doStep(self._dahlquist_time, dt)", code);
        Assert.Contains("self.outputs[\"x\"] = self.dahlquist.get(\"x\")", code);
    }

    [Fact]
    public void UpdateBlock_UsesCustomIndent()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(SampleInfo(), "    ");

        Assert.Contains("    # === physics: PMSM (auto-inserted in update) ===", code);
        Assert.Contains("    self.pmsm.doStep(", code);
    }
}