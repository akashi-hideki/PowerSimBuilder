using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class PhysicsCatalogSourceTests
{
    [Theory]
    [InlineData("https://ex.com/catalog.json")]
    [InlineData("http://localhost:8765/models.json")]
    [InlineData("https://ex.com/cat.json?v=1")]
    // Local relative paths (bundled catalog) must also be detected as Json
    [InlineData("catalog/fmu_catalog_fmi2_cs_win64.json")]
    [InlineData("catalog\\fmu_catalog_fmi2_cs_win64.json")]
    [InlineData("test_catalog_tmp/local_test.json")]
    public void Detect_Json(string url)
    {
        var info = PhysicsCatalogSource.Detect(url);
        Assert.Equal(CatalogSourceKind.Json, info.Kind);
    }

    [Theory]
    [InlineData("https://github.com/user/repo")]
    [InlineData("https://github.com/user/repo/releases")]
    [InlineData("https://github.com/user/repo/releases/latest")]
    [InlineData("https://github.com/user/repo/releases/tag/v1.0")]
    [InlineData("https://github.com/user/repo.git")]
    public void Detect_Github(string url)
    {
        var info = PhysicsCatalogSource.Detect(url);
        Assert.Equal(CatalogSourceKind.GitHub, info.Kind);
    }

    [Theory]
    [InlineData("https://example.com/foo")]
    [InlineData("not a url")]
    public void Detect_Unknown(string url)
    {
        var info = PhysicsCatalogSource.Detect(url);
        Assert.Equal(CatalogSourceKind.Unknown, info.Kind);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Detect_NullOrEmpty_ReturnsUnknown(string? url)
    {
        var info = PhysicsCatalogSource.Detect(url);
        Assert.Equal(CatalogSourceKind.Unknown, info.Kind);
    }
}
