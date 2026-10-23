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
            new("R", "0.5", "Real"),
            new("Ld", "0.001", "Real"),
        },
        Timing: new FmuTiming(0.0, 1.0, 0.001),
        InputTypes: new Dictionary<string, string>
        {
            ["u_d"] = "Real",
            ["u_q"] = "Real",
        },
        OutputTypes: new Dictionary<string, string>
        {
            ["i_d"] = "Real",
            ["i_q"] = "Real",
            ["torque"] = "Real",
        });

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
    public void InsertionBlock_RealInputsHaveTypeAnnotations()
    {
        var code = PhysicsSnippetBuilder.BuildInsertionBlock(
            SampleInfo(), "PMSM/model.fmu");

        Assert.Contains("# inputs    : u_d:Real, u_q:Real", code);
        Assert.Contains("# outputs   : i_d:Real, i_q:Real, torque:Real", code);
        Assert.Contains("self.pmsm.enter_initialization_mode()", code);
        Assert.Contains("self.pmsm.exit_initialization_mode()", code);
        Assert.Contains("self._pmsm_time = 0.0", code);
    }

    [Fact]
    public void UpdateBlock_RealInputsUseFloatConversion()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(SampleInfo());

        Assert.Contains("self.pmsm.set(\"u_d\", float(self.inputs.get(\"u_d\", 0.0)))", code);
        Assert.Contains("self.pmsm.set(\"u_q\", float(self.inputs.get(\"u_q\", 0.0)))", code);
        Assert.Contains("self.outputs[\"i_d\"] = float(self.pmsm.get(\"i_d\"))", code);
        Assert.Contains("self.outputs[\"torque\"] = float(self.pmsm.get(\"torque\"))", code);
    }

    [Fact]
    public void UpdateBlock_MixedTypesUseCorrectConversions()
    {
        var info = new FmuInfo(
            ModelName: "Mixed",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new[] { "r", "i", "b", "s" },
            Outputs: new[] { "r_out", "i_out", "b_out", "s_out" },
            Parameters: new List<FmuParameter>(),
            Timing: null,
            InputTypes: new Dictionary<string, string>
            {
                ["r"] = "Real",
                ["i"] = "Integer",
                ["b"] = "Boolean",
                ["s"] = "String",
            },
            OutputTypes: new Dictionary<string, string>
            {
                ["r_out"] = "Real",
                ["i_out"] = "Integer",
                ["b_out"] = "Boolean",
                ["s_out"] = "String",
            });

        var code = PhysicsSnippetBuilder.BuildUpdateBlock(info);

        Assert.Contains("self.mixed.set(\"r\", float(self.inputs.get(\"r\", 0.0)))", code);
        Assert.Contains("self.mixed.set(\"i\", int(self.inputs.get(\"i\", 0)))", code);
        Assert.Contains("self.mixed.set(\"b\", bool(self.inputs.get(\"b\", False)))", code);
        Assert.Contains("self.mixed.set(\"s\", str(self.inputs.get(\"s\", \"\")))", code);
        Assert.Contains("self.outputs[\"r_out\"] = float(self.mixed.get(\"r_out\"))", code);
        Assert.Contains("self.outputs[\"i_out\"] = int(self.mixed.get(\"i_out\"))", code);
        Assert.Contains("self.outputs[\"b_out\"] = bool(self.mixed.get(\"b_out\"))", code);
        Assert.Contains("self.outputs[\"s_out\"] = str(self.mixed.get(\"s_out\"))", code);
    }

    [Fact]
    public void UpdateBlock_EnumBecomesTodo()
    {
        var info = new FmuInfo(
            ModelName: "EnumModel",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new[] { "mode" },
            Outputs: new[] { "state" },
            Parameters: new List<FmuParameter>(),
            Timing: null,
            InputTypes: new Dictionary<string, string> { ["mode"] = "Enumeration" },
            OutputTypes: new Dictionary<string, string> { ["state"] = "Enumeration" });

        var code = PhysicsSnippetBuilder.BuildUpdateBlock(info);

        Assert.Contains("# TODO: set(\"mode\", <Enumeration>)", code);
        Assert.Contains("# TODO: self.outputs[\"state\"] = self.enummodel.get(\"state\")", code);
    }

    [Fact]
    public void UpdateBlock_NoTypes_FallsBackToReal()
    {
        // Legacy FmuInfo without InputTypes/OutputTypes should behave like all-Real.
        var info = new FmuInfo(
            ModelName: "Legacy",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new[] { "x" },
            Outputs: new[] { "y" },
            Parameters: new List<FmuParameter>(),
            Timing: null);

        var code = PhysicsSnippetBuilder.BuildUpdateBlock(info);

        Assert.Contains("self.legacy.set(\"x\", float(self.inputs.get(\"x\", 0.0)))", code);
        Assert.Contains("self.outputs[\"y\"] = float(self.legacy.get(\"y\"))", code);
    }

    [Fact]
    public void InsertionBlock_NonRealParameterBecomesTodo()
    {
        var info = new FmuInfo(
            ModelName: "PMSM",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new string[0],
            Outputs: new string[0],
            Parameters: new List<FmuParameter> { new("Mode", "# TODO", "Enumeration") },
            Timing: null);

        var code = PhysicsSnippetBuilder.BuildInsertionBlock(info, "X/model.fmu");

        Assert.Contains("# TODO: set(\"Mode\", <value>)  # Enumeration parameter", code);
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
    }

    [Fact]
    public void UpdateBlock_UsesCustomIndent()
    {
        var code = PhysicsSnippetBuilder.BuildUpdateBlock(SampleInfo(), "    ");

        Assert.Contains("    # === physics: PMSM (auto-inserted in update) ===", code);
        Assert.Contains("    self.pmsm.doStep(", code);
    }
}