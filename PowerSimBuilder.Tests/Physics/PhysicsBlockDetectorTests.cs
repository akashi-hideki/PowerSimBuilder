using System.Linq;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class PhysicsBlockDetectorTests
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
    public void FindAll_EmptyCode_ReturnsEmpty()
    {
        Assert.Empty(PhysicsBlockDetector.FindAll(""));
        Assert.Empty(PhysicsBlockDetector.FindAll((string?)null));
    }

    [Fact]
    public void FindAll_NoMarkers_ReturnsEmpty()
    {
        Assert.Empty(PhysicsBlockDetector.FindAll("def foo():\n    pass\n"));
    }

    [Fact]
    public void FindAll_OneInitBlock_ReturnsOne()
    {
        var code = "import os\n" + MakeInitBlock("PMSM");
        var blocks = PhysicsBlockDetector.FindAll(code);
        Assert.Single(blocks);
        Assert.Equal("PMSM", blocks[0].ModelName);
        Assert.True(blocks[0].HasInit);
        Assert.False(blocks[0].HasUpdate);
    }

    [Fact]
    public void FindAll_InitAndUpdate_OneModelWithBothBlocks()
    {
        var code = MakeInitBlock("PMSM") + "\n" + MakeUpdateBlock("PMSM");
        var blocks = PhysicsBlockDetector.FindAll(code);
        Assert.Single(blocks);
        Assert.Equal("PMSM", blocks[0].ModelName);
        Assert.True(blocks[0].HasInit);
        Assert.True(blocks[0].HasUpdate);
    }

    [Fact]
    public void FindAll_TwoModels_ReturnsBoth()
    {
        var code = MakeInitBlock("PMSM") + "\n" + MakeInitBlock("Feedthrough");
        var blocks = PhysicsBlockDetector.FindAll(code);
        Assert.Equal(2, blocks.Count);
        Assert.Equal("Feedthrough", blocks[0].ModelName); // alphabetical
        Assert.Equal("PMSM", blocks[1].ModelName);
    }

    [Fact]
    public void FindAll_NameWithSpaces_Parsed()
    {
        var code = MakeInitBlock("First Order Lag");
        var blocks = PhysicsBlockDetector.FindAll(code);
        Assert.Single(blocks);
        Assert.Equal("First Order Lag", blocks[0].ModelName);
    }

    [Fact]
    public void FindAll_UpdateOnly_ReturnsModelWithUpdateOnly()
    {
        var code = MakeUpdateBlock("PMSM");
        var blocks = PhysicsBlockDetector.FindAll(code);
        Assert.Single(blocks);
        Assert.False(blocks[0].HasInit);
        Assert.True(blocks[0].HasUpdate);
    }
}