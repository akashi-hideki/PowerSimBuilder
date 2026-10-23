#nullable enable
using System;
using System.IO;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class FmuInfoReaderTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Repo root not found");
    }

    private static string? RefFmu(string name)
    {
        var path = Path.Combine(RepoRoot(), "tmp", "Reference-FMUs", "2.0", name + ".fmu");
        return File.Exists(path) ? path : null;
    }

    [Fact]
    public void Reads_Dahlquist()
    {
        var path = RefFmu("Dahlquist");
        if (path == null) return; // fixture missing: skip

        var info = FmuInfoReader.Read(path);

        Assert.Equal("Dahlquist", info.ModelName);
        Assert.Equal("2.0", info.FmiVersion);
        Assert.Equal("CoSimulation", info.FmiType);
        Assert.Empty(info.Inputs);
        Assert.NotEmpty(info.Outputs);
    }

    [Fact]
    public void Reads_Feedthrough()
    {
        var path = RefFmu("Feedthrough");
        if (path == null) return;

        var info = FmuInfoReader.Read(path);

        Assert.Equal("Feedthrough", info.ModelName);
        Assert.NotEmpty(info.Inputs);
        Assert.NotEmpty(info.Outputs);
    }
}