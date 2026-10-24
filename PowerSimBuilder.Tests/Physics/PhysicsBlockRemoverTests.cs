using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class PhysicsBlockRemoverTests
{
    private static string MakeInitBlock(string name) =>
        $"# === physics: {name} (auto-inserted) ===\n" +
        $"self.{name.ToLower()} = load_fmu(...)\n" +
        $"# === /physics: {name} ===\n";

    private static string MakeUpdateBlock(string name) =>
        $"# === physics: {name} (auto-inserted in update) ===\n" +
        $"self.{name.ToLower()}.doStep(...)\n" +
        $"# === /physics: {name} ===\n";

    [Fact]
    public void Remove_NullCode_Fails()
    {
        var result = PhysicsBlockRemover.Remove(null, "PMSM");
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void Remove_EmptyName_Fails()
    {
        var result = PhysicsBlockRemover.Remove("code", "");
        Assert.False(result.Success);
    }

    [Fact]
    public void Remove_NoBlocks_Fails()
    {
        var result = PhysicsBlockRemover.Remove("def foo():\n    pass\n", "PMSM");
        Assert.False(result.Success);
    }

    [Fact]
    public void Remove_InitOnly_RemovesBlock()
    {
        var code = "import os\n" + MakeInitBlock("PMSM") + "def foo():\n    pass\n";
        var result = PhysicsBlockRemover.Remove(code, "PMSM");
        Assert.True(result.Success);
        Assert.Equal(1, result.RemovedInitCount);
        Assert.Equal(0, result.RemovedUpdateCount);
        Assert.DoesNotContain("physics: PMSM", result.NewCode);
        Assert.Contains("import os", result.NewCode);
        Assert.Contains("def foo", result.NewCode);
    }

    [Fact]
    public void Remove_InitAndUpdate_RemovesBoth()
    {
        var code = MakeInitBlock("PMSM") + "\n" + MakeUpdateBlock("PMSM");
        var result = PhysicsBlockRemover.Remove(code, "PMSM");
        Assert.True(result.Success);
        Assert.Equal(1, result.RemovedInitCount);
        Assert.Equal(1, result.RemovedUpdateCount);
        Assert.DoesNotContain("physics: PMSM", result.NewCode);
    }

    [Fact]
    public void Remove_TwoModels_OnlyRemovesTarget()
    {
        var code = MakeInitBlock("PMSM") + "\n" + MakeInitBlock("Feedthrough");
        var result = PhysicsBlockRemover.Remove(code, "PMSM");
        Assert.True(result.Success);
        Assert.DoesNotContain("physics: PMSM", result.NewCode);
        Assert.Contains("physics: Feedthrough", result.NewCode);
    }

    [Fact]
    public void Remove_ResultIsReDetectable_OtherBlocksRemain()
    {
        var code = MakeInitBlock("PMSM") + "\n" + MakeInitBlock("Feedthrough");
        var result = PhysicsBlockRemover.Remove(code, "PMSM");
        var remaining = PhysicsBlockDetector.FindAll(result.NewCode);
        Assert.Single(remaining);
        Assert.Equal("Feedthrough", remaining[0].ModelName);
    }
}