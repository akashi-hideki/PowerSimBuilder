#nullable enable
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsInsertionHighlighterTests
{
    private const string Sample =
        "class Foo:\n" +
        "    def __init__(self):\n" +
        "        self.x = 0\n" +
        "        # === physics: PMSM (auto-inserted) ===\n" +
        "        # source: ...\n" +
        "        self.pmsm = load_fmu(...)\n" +
        "        # === /physics: PMSM ===\n" +
        "        self.y = 0\n" +
        "\n" +
        "    def update(self, t, dt):\n" +
        "        # === physics: PMSM (auto-inserted in update) ===\n" +
        "        self.pmsm.doStep(self._pmsm_time, dt)\n" +
        "        # === /physics: PMSM ===\n" +
        "        pass\n";

    [Fact]
    public void FindBlock_ReturnsInitBounds()
    {
        var b = PhysicsInsertionHighlighter.FindBlock(Sample, "PMSM");

        Assert.NotNull(b);
        Assert.Equal(4, b!.StartLine);
        Assert.Equal(7, b.EndLine);
    }

    [Fact]
    public void FindUpdateBlock_ReturnsUpdateBounds()
    {
        var b = PhysicsInsertionHighlighter.FindUpdateBlock(Sample, "PMSM");

        Assert.NotNull(b);
        Assert.Equal(11, b!.StartLine);
        Assert.Equal(13, b.EndLine);
    }

    [Fact]
    public void FindBlock_DoesNotMatchUpdateMarker()
    {
        // When only update marker exists, FindBlock must return null.
        const string onlyUpdate =
            "class Foo:\n" +
            "    def update(self):\n" +
            "        # === physics: PMSM (auto-inserted in update) ===\n" +
            "        pass\n" +
            "        # === /physics: PMSM ===\n";
        Assert.Null(PhysicsInsertionHighlighter.FindBlock(onlyUpdate, "PMSM"));
    }

    [Fact]
    public void FindUpdateBlock_DoesNotMatchInitMarker()
    {
        // When only init marker exists, FindUpdateBlock must return null.
        const string onlyInit =
            "class Foo:\n" +
            "    def __init__(self):\n" +
            "        # === physics: PMSM (auto-inserted) ===\n" +
            "        pass\n" +
            "        # === /physics: PMSM ===\n";
        Assert.Null(PhysicsInsertionHighlighter.FindUpdateBlock(onlyInit, "PMSM"));
    }

    [Fact]
    public void FindBlock_NullCode_ReturnsNull()
    {
        Assert.Null(PhysicsInsertionHighlighter.FindBlock(null, "PMSM"));
    }

    [Fact]
    public void FindBlock_NullModelName_ReturnsNull()
    {
        Assert.Null(PhysicsInsertionHighlighter.FindBlock(Sample, null));
    }

    [Fact]
    public void FindBlock_ModelNotFound_ReturnsNull()
    {
        Assert.Null(PhysicsInsertionHighlighter.FindBlock(Sample, "Dahlquist"));
    }

    [Fact]
    public void FindBlock_CRLF_Works()
    {
        var crlf = Sample.Replace("\n", "\r\n");
        var b = PhysicsInsertionHighlighter.FindBlock(crlf, "PMSM");

        Assert.NotNull(b);
        Assert.Equal(4, b!.StartLine);
        Assert.Equal(7, b.EndLine);
    }

    [Fact]
    public void FindUpdateBlock_CRLF_Works()
    {
        var crlf = Sample.Replace("\n", "\r\n");
        var b = PhysicsInsertionHighlighter.FindUpdateBlock(crlf, "PMSM");

        Assert.NotNull(b);
        Assert.Equal(11, b!.StartLine);
        Assert.Equal(13, b.EndLine);
    }
}