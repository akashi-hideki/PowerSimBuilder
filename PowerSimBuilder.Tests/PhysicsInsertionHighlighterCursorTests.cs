#nullable enable
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsInsertionHighlighterCursorTests
{
    private const string Sample =
        "class Foo:\n" +
        "    def __init__(self):\n" +
        "        # === physics: PMSM (auto-inserted) ===\n" +
        "        self.pmsm = load_fmu(...)\n" +
        "        # === /physics: PMSM ===\n" +
        "\n" +
        "    def update(self, t, dt):\n" +
        "        # === physics: PMSM (auto-inserted in update) ===\n" +
        "        self.pmsm.doStep(...)\n" +
        "        # === /physics: PMSM ===\n";

    [Fact]
    public void FindBlockAtPosition_InsideInit_ReturnsInitBlock()
    {
        int pos = Sample.IndexOf("self.pmsm = load_fmu");
        var hit = PhysicsInsertionHighlighter.FindBlockAtPosition(Sample, pos);

        Assert.NotNull(hit);
        Assert.Equal("PMSM", hit!.ModelName);
        Assert.Equal("init", hit.Kind);
        Assert.Equal(3, hit.StartLine);
        Assert.Equal(5, hit.EndLine);
    }

    [Fact]
    public void FindBlockAtPosition_InsideUpdate_ReturnsUpdateBlock()
    {
        int pos = Sample.IndexOf("self.pmsm.doStep");
        var hit = PhysicsInsertionHighlighter.FindBlockAtPosition(Sample, pos);

        Assert.NotNull(hit);
        Assert.Equal("PMSM", hit!.ModelName);
        Assert.Equal("update", hit.Kind);
        Assert.Equal(8, hit.StartLine);
        Assert.Equal(10, hit.EndLine);
    }

    [Fact]
    public void FindBlockAtPosition_Outside_ReturnsNull()
    {
        int pos = Sample.IndexOf("class Foo");
        var hit = PhysicsInsertionHighlighter.FindBlockAtPosition(Sample, pos);
        Assert.Null(hit);
    }

    [Fact]
    public void FindBlockAtPosition_NullCode_ReturnsNull()
    {
        Assert.Null(PhysicsInsertionHighlighter.FindBlockAtPosition(null, 0));
    }

    [Fact]
    public void FindBlockAtPosition_NegativePos_ReturnsNull()
    {
        Assert.Null(PhysicsInsertionHighlighter.FindBlockAtPosition(Sample, -1));
    }

    [Fact]
    public void FindBlockAtPosition_PastEnd_ReturnsNull()
    {
        Assert.Null(PhysicsInsertionHighlighter.FindBlockAtPosition(Sample, Sample.Length + 10));
    }
}