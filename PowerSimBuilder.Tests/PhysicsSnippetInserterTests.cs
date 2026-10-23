#nullable enable
using System.Collections.Generic;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsSnippetInserterTests
{
    private const string Template =
        "class Foo:\n" +
        "    def __init__(self, comp_id: str):\n" +
        "        super().__init__(comp_id)\n" +
        "        self.x = 0\n" +
        "\n" +
        "    def update(self, t, dt):\n" +
        "        pass\n";

    private static FmuInfo SampleInfo() => new FmuInfo(
        ModelName: "PMSM",
        FmiVersion: "2.0",
        FmiType: "CoSimulation",
        Inputs: new[] { "u_d", "u_q" },
        Outputs: new[] { "i_d", "i_q" },
        Parameters: new List<FmuParameter> { new("R", "0.5") },
        Timing: new FmuTiming(0.0, 1.0, 0.001));

    private static (string initBlock, string helper, string updateBlock) BuildPieces()
    {
        var info = SampleInfo();
        return (
            PhysicsSnippetBuilder.BuildInsertionBlock(info, "PMSM/model.fmu"),
            PhysicsSnippetBuilder.BuildHelper(),
            PhysicsSnippetBuilder.BuildUpdateBlock(info)
        );
    }

    [Fact]
    public void Insert_AddsBlockHelperAndImport()
    {
        var (initBlock, helper, _) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(
            Template, "PMSM", initBlock, helper);

        Assert.Equal(InsertOutcome.Inserted, outcome);
        Assert.Contains("from fmpy import load_fmu", code);
        Assert.Contains("def " + PhysicsSnippetBuilder.HelperFunctionName, code);
        Assert.Contains("# === physics: PMSM (auto-inserted) ===", code);
        Assert.Contains("self.pmsm = load_fmu(", code);
        Assert.Contains("self.pmsm.set(\"R\", 0.5)", code);
    }

    [Fact]
    public void Insert_WithUpdateBlock_InsertsBoth()
    {
        var (initBlock, helper, updateBlock) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(
            Template, "PMSM", initBlock, helper, updateBlock);

        Assert.Equal(InsertOutcome.Inserted, outcome);
        Assert.Contains("# === physics: PMSM (auto-inserted) ===", code);
        Assert.Contains("# === physics: PMSM (auto-inserted in update) ===", code);
        Assert.Contains("self.pmsm.doStep(self._pmsm_time, dt)", code);
        Assert.Contains("self.outputs[\"i_d\"] = self.pmsm.get(\"i_d\")", code);
    }

    [Fact]
    public void Insert_NoUpdateMethod_ReturnsNoUpdateFound()
    {
        const string src =
            "class Foo:\n" +
            "    def __init__(self):\n" +
            "        pass\n";
        var (initBlock, helper, updateBlock) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(
            src, "PMSM", initBlock, helper, updateBlock);

        Assert.Equal(InsertOutcome.NoUpdateFound, outcome);
        Assert.Equal(src, code);
    }

    [Fact]
    public void Insert_DetectsFourSpaceBodyIndent()
    {
        const string src =
            "class Foo:\n" +
            "  def __init__(self):\n" +
            "    self.x = 0\n" +
            "\n" +
            "  def update(self, t, dt):\n" +
            "    pass\n";
        var (initBlock, helper, _) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(src, "PMSM", initBlock, helper);

        Assert.Equal(InsertOutcome.Inserted, outcome);
        Assert.Contains("\n    # === physics: PMSM (auto-inserted) ===", code);
        Assert.Contains("\n    self.pmsm = load_fmu(", code);
    }

    [Fact]
    public void Insert_Duplicate_ReturnsAlreadyExists()
    {
        var (initBlock, helper, updateBlock) = BuildPieces();
        var (_, first) = PhysicsSnippetInserter.Insert(Template, "PMSM", initBlock, helper, updateBlock);

        var (outcome, code) = PhysicsSnippetInserter.Insert(first, "PMSM", initBlock, helper, updateBlock);

        Assert.Equal(InsertOutcome.AlreadyExists, outcome);
        Assert.Equal(first, code);
    }

    [Fact]
    public void Insert_NoInit_ReturnsNoInitFound()
    {
        const string src = "class Foo:\n    pass\n";
        var (initBlock, helper, _) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(src, "PMSM", initBlock, helper);

        Assert.Equal(InsertOutcome.NoInitFound, outcome);
        Assert.Equal(src, code);
    }

    [Fact]
    public void Insert_DoesNotDuplicateImportOrHelper()
    {
        var (initBlock, helper, updateBlock) = BuildPieces();
        var (_, first) = PhysicsSnippetInserter.Insert(Template, "PMSM", initBlock, helper, updateBlock);

        var info2 = new FmuInfo(
            ModelName: "Dahlquist",
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: new string[0],
            Outputs: new[] { "x" },
            Parameters: new List<FmuParameter>(),
            Timing: null);
        var block2 = PhysicsSnippetBuilder.BuildInsertionBlock(info2, "Dahlquist/model.fmu");
        var update2 = PhysicsSnippetBuilder.BuildUpdateBlock(info2);

        var (outcome, second) = PhysicsSnippetInserter.Insert(first, "Dahlquist", block2, helper, update2);

        Assert.Equal(InsertOutcome.Inserted, outcome);
        Assert.Equal(1, CountOccurrences(second, "from fmpy import load_fmu"));
        Assert.Equal(1, CountOccurrences(second, "def " + PhysicsSnippetBuilder.HelperFunctionName));
        Assert.Contains("# === physics: PMSM (auto-inserted) ===", second);
        Assert.Contains("# === physics: Dahlquist (auto-inserted) ===", second);
        Assert.Contains("# === physics: PMSM (auto-inserted in update) ===", second);
        Assert.Contains("# === physics: Dahlquist (auto-inserted in update) ===", second);
    }

    [Fact]
    public void Insert_PreservesCrlf()
    {
        var crlfTemplate = Template.Replace("\n", "\r\n");
        var (initBlock, helper, _) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(crlfTemplate, "PMSM", initBlock, helper);

        Assert.Equal(InsertOutcome.Inserted, outcome);
        Assert.Contains("\r\n", code);
        var stripped = code.Replace("\r\n", "");
        Assert.DoesNotContain("\n", stripped);
    }

    [Fact]
    public void Insert_IntoGeneratedFirstOrderLagShape()
    {
        const string lag =
            "import math\n" +
            "from typing import Dict, List, Tuple, Any\n" +
            "\n" +
            "class FirstOrderLag(Component):\n" +
            "    \"\"\"First Order Lag\"\"\"\n" +
            "\n" +
            "    def __init__(self, comp_id: str, params = None):\n" +
            "        super().__init__(comp_id, \"FirstOrderLag\")\n" +
            "        self.inputs = { 'input': 0.0 }\n" +
            "        self.outputs = { 'output': 0.0 }\n" +
            "        self._last_update_time = 0.0\n" +
            "\n" +
            "    def update(self, t: float, dt: float) -> None:\n" +
            "        pass\n";
        var (initBlock, helper, updateBlock) = BuildPieces();

        var (outcome, code) = PhysicsSnippetInserter.Insert(lag, "PMSM", initBlock, helper, updateBlock);

        Assert.Equal(InsertOutcome.Inserted, outcome);
        Assert.Contains("\n        # === physics: PMSM (auto-inserted) ===", code);
        Assert.Contains("\n        # === physics: PMSM (auto-inserted in update) ===", code);

        var initIdx = code.IndexOf("# === physics: PMSM (auto-inserted) ===");
        var updateIdx = code.IndexOf("# === physics: PMSM (auto-inserted in update) ===");
        var updateDefIdx = code.IndexOf("def update(self");
        Assert.True(initIdx > 0);
        Assert.True(updateIdx > updateDefIdx);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, System.StringComparison.Ordinal)) >= 0)
        {
            count++; idx += needle.Length;
        }
        return count;
    }
}