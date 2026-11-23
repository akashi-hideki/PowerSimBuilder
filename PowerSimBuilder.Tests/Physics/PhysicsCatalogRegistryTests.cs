#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

/// <summary>
/// Unit tests for PhysicsCatalogRegistry (local file path support) and
/// PhysicsCatalogSource (URL kind detection).
/// </summary>
public class PhysicsCatalogRegistryTests
{
    // ── LoadFromJson ──────────────────────────────────────────────

    [Fact]
    public void LoadFromJson_ValidJson_ReturnsItems()
    {
        var json = @"{""version"":1,""models"":[
            {""name"":""BouncingBall"",""url"":""https://example.com/BouncingBall.fmu"",""sizeBytes"":1024,""license"":""MIT"",""description"":""FMI 2.0 / cs / win64 / Test""},
            {""name"":""Rectifier"",""url"":""https://example.com/Rectifier.fmu"",""sizeBytes"":0,""license"":""BSD"",""description"":""FMI 2.0 / cs / win64 / Test""}
        ]}";

        var result = PhysicsCatalogRegistry.LoadFromJson(json);

        Assert.True(result.Success);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("BouncingBall", result.Items[0].Name);
        Assert.Equal("https://example.com/BouncingBall.fmu", result.Items[0].Url);
        Assert.Equal(1024, result.Items[0].SizeBytes);
        Assert.Equal("MIT", result.Items[0].License);
        Assert.Equal("Rectifier", result.Items[1].Name);
        // sizeBytes == 0 should be treated as null
        Assert.Null(result.Items[1].SizeBytes);
    }

    [Fact]
    public void LoadFromJson_EmptyString_ReturnsFail()
    {
        var result = PhysicsCatalogRegistry.LoadFromJson("");
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public void LoadFromJson_Whitespace_ReturnsFail()
    {
        var result = PhysicsCatalogRegistry.LoadFromJson("   ");
        Assert.False(result.Success);
    }

    [Fact]
    public void LoadFromJson_InvalidJson_ReturnsFail()
    {
        var result = PhysicsCatalogRegistry.LoadFromJson("{ not valid json ]");
        Assert.False(result.Success);
        Assert.Contains("Invalid catalog JSON", result.ErrorMessage ?? "");
    }

    [Fact]
    public void LoadFromJson_NoModelsArray_ReturnsFail()
    {
        var result = PhysicsCatalogRegistry.LoadFromJson(@"{""version"":1,""foo"":""bar""}");
        Assert.False(result.Success);
        Assert.Contains("models", result.ErrorMessage ?? "");
    }

    [Fact]
    public void LoadFromJson_ModelsWithoutNameOrUrl_AreSkipped()
    {
        var json = @"{""version"":1,""models"":[
            {""name"":""Good"",""url"":""https://example.com/good.fmu""},
            {""name"":""NoUrl"",""url"":""""},
            {""name"":"""",""url"":""https://example.com/noname.fmu""},
            {""url"":""https://example.com/noname2.fmu""}
        ]}";

        var result = PhysicsCatalogRegistry.LoadFromJson(json);
        Assert.True(result.Success);
        Assert.Single(result.Items);
        Assert.Equal("Good", result.Items[0].Name);
    }

    [Fact]
    public void LoadFromJson_EmptyModelsArray_ReturnsEmptySuccess()
    {
        var json = @"{""version"":1,""models"":[]}";
        var result = PhysicsCatalogRegistry.LoadFromJson(json);
        Assert.True(result.Success);
        Assert.Empty(result.Items);
    }

    // ── LoadFromUrlAsync (local path) ────────────────────────────

    [Fact]
    public async Task LoadFromUrlAsync_LocalPath_ReturnsItems()
    {
        // Arrange: write a test catalog to a subdirectory of AppContext.BaseDirectory
        var baseDir = AppContext.BaseDirectory;
        var testDir = Path.Combine(baseDir, "test_catalog_tmp");
        var testFile = Path.Combine(testDir, "local_test.json");
        var json = @"{""version"":1,""models"":[
            {""name"":""LocalModel"",""url"":""https://example.com/local.fmu"",""description"":""Local test""}
        ]}";
        Directory.CreateDirectory(testDir);
        await File.WriteAllTextAsync(testFile, json);

        try
        {
            // Act: use a relative path with forward slashes (as catalog_sources.json would)
            var relativePath = "test_catalog_tmp/local_test.json";
            var result = await PhysicsCatalogRegistry.LoadFromUrlAsync(relativePath);

            // Assert
            Assert.True(result.Success);
            Assert.Single(result.Items);
            Assert.Equal("LocalModel", result.Items[0].Name);
            Assert.Equal("https://example.com/local.fmu", result.Items[0].Url);
        }
        finally
        {
            if (File.Exists(testFile)) File.Delete(testFile);
            if (Directory.Exists(testDir)) Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadFromUrlAsync_LocalPathWithBackslashes_ReturnsItems()
    {
        // Arrange
        var baseDir = AppContext.BaseDirectory;
        var testDir = Path.Combine(baseDir, "test_catalog_bs");
        var testFile = Path.Combine(testDir, "bs_test.json");
        var json = @"{""version"":1,""models"":[
            {""name"":""BsModel"",""url"":""https://example.com/bs.fmu""}
        ]}";
        Directory.CreateDirectory(testDir);
        await File.WriteAllTextAsync(testFile, json);

        try
        {
            // Act: use backslashes (Windows-style relative path)
            var relativePath = Path.Combine("test_catalog_bs", "bs_test.json");
            var result = await PhysicsCatalogRegistry.LoadFromUrlAsync(relativePath);

            Assert.True(result.Success);
            Assert.Single(result.Items);
        }
        finally
        {
            if (File.Exists(testFile)) File.Delete(testFile);
            if (Directory.Exists(testDir)) Directory.Delete(testDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadFromUrlAsync_LocalPathNotFound_ReturnsFail()
    {
        var result = await PhysicsCatalogRegistry.LoadFromUrlAsync("nonexistent_dir/nonexistent_catalog.json");
        Assert.False(result.Success);
        Assert.Contains("not found", result.ErrorMessage ?? "");
    }

    [Fact]
    public async Task LoadFromUrlAsync_EmptyUrl_ReturnsFail()
    {
        var result = await PhysicsCatalogRegistry.LoadFromUrlAsync("");
        Assert.False(result.Success);
    }

    [Fact]
    public async Task LoadFromUrlAsync_NullUrl_ReturnsFail()
    {
        var result = await PhysicsCatalogRegistry.LoadFromUrlAsync(null);
        Assert.False(result.Success);
    }

    // ── PhysicsCatalogSource.Detect ──────────────────────────────

    [Fact]
    public void Detect_LocalJsonPath_ReturnsJsonKind()
    {
        // This is the key test: the bundled catalog relative path must be
        // detected as Json so it gets routed to LoadFromUrlAsync.
        var result = PhysicsCatalogSource.Detect("catalog/fmu_catalog_fmi2_cs_win64.json");
        Assert.Equal(CatalogSourceKind.Json, result.Kind);
    }

    [Fact]
    public void Detect_HttpJsonUrl_ReturnsJsonKind()
    {
        var result = PhysicsCatalogSource.Detect("https://example.com/catalog.json");
        Assert.Equal(CatalogSourceKind.Json, result.Kind);
    }

    [Fact]
    public void Detect_LocalJsonPathWithQuery_ReturnsJsonKind()
    {
        var result = PhysicsCatalogSource.Detect("catalog/test.json?v=1");
        Assert.Equal(CatalogSourceKind.Json, result.Kind);
    }

    [Fact]
    public void Detect_GitHubRepoUrl_ReturnsGitHubKind()
    {
        var result = PhysicsCatalogSource.Detect("https://github.com/modelica/Reference-FMUs");
        Assert.Equal(CatalogSourceKind.GitHub, result.Kind);
    }

    [Fact]
    public void Detect_GitHubRepoUrlWithReleases_ReturnsGitHubKind()
    {
        var result = PhysicsCatalogSource.Detect("https://github.com/modelica/Reference-FMUs/releases");
        Assert.Equal(CatalogSourceKind.GitHub, result.Kind);
    }

    [Fact]
    public void Detect_EmptyUrl_ReturnsUnknown()
    {
        var result = PhysicsCatalogSource.Detect("");
        Assert.Equal(CatalogSourceKind.Unknown, result.Kind);
    }

    [Fact]
    public void Detect_NullUrl_ReturnsUnknown()
    {
        var result = PhysicsCatalogSource.Detect(null);
        Assert.Equal(CatalogSourceKind.Unknown, result.Kind);
    }

    [Fact]
    public void Detect_NonJsonUrl_ReturnsUnknown()
    {
        var result = PhysicsCatalogSource.Detect("https://example.com/readme.txt");
        Assert.Equal(CatalogSourceKind.Unknown, result.Kind);
    }
}
