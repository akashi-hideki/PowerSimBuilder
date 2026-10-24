#nullable enable
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class PhysicsCatalogRegistryTests
{
    private const string ValidJson = @"{
        ""version"": 1,
        ""models"": [
            { ""name"": ""Dahlquist"", ""url"": ""https://ex.com/Dahlquist.fmu"", ""sizeBytes"": 184603, ""license"": ""BSD-2-Clause"" },
            { ""name"": ""Feedthrough"", ""url"": ""https://ex.com/Feedthrough.fmu"" }
        ]
    }";

    [Fact]
    public void LoadFromJson_Valid_ReturnsModels()
    {
        var r = PhysicsCatalogRegistry.LoadFromJson(ValidJson);
        Assert.True(r.Success);
        Assert.Equal(2, r.Items.Count);
        Assert.Equal("Dahlquist", r.Items[0].Name);
        Assert.Equal(184603, r.Items[0].SizeBytes);
        Assert.Equal("BSD-2-Clause", r.Items[0].License);
        Assert.Null(r.Items[1].SizeBytes);
    }

    [Fact]
    public void LoadFromJson_Empty_ReturnsOkEmpty()
    {
        var r = PhysicsCatalogRegistry.LoadFromJson(@"{ ""models"": [] }");
        Assert.True(r.Success);
        Assert.Empty(r.Items);
    }

    [Fact]
    public void LoadFromJson_NoModels_Fails()
    {
        var r = PhysicsCatalogRegistry.LoadFromJson(@"{ ""version"": 1 }");
        Assert.False(r.Success);
        Assert.Contains("models", r.ErrorMessage!);
    }

    [Fact]
    public void LoadFromJson_InvalidJson_Fails()
    {
        var r = PhysicsCatalogRegistry.LoadFromJson("{ not json");
        Assert.False(r.Success);
    }

    [Fact]
    public void LoadFromJson_Null_Fails()
    {
        var r = PhysicsCatalogRegistry.LoadFromJson(null);
        Assert.False(r.Success);
    }

    [Fact]
    public void LoadFromJson_MissingUrl_SkipsItem()
    {
        var r = PhysicsCatalogRegistry.LoadFromJson(@"{ ""models"": [ { ""name"": ""X"" } ] }");
        Assert.True(r.Success);
        Assert.Empty(r.Items);
    }

    [Fact]
    public async Task LoadFromUrlAsync_404_Fails()
    {
        using var handler = new StaticHandler(HttpStatusCode.NotFound);
        var r = await PhysicsCatalogRegistry.LoadFromUrlAsync("https://ex.com/c.json", handler);
        Assert.False(r.Success);
        Assert.Contains("404", r.ErrorMessage!);
    }

    [Fact]
    public async Task LoadFromUrlAsync_200_ReturnsModels()
    {
        using var handler = new StaticHandler(HttpStatusCode.OK, ValidJson);
        var r = await PhysicsCatalogRegistry.LoadFromUrlAsync("https://ex.com/c.json", handler);
        Assert.True(r.Success);
        Assert.Equal(2, r.Items.Count);
    }

    private sealed class StaticHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _body;
        public StaticHandler(HttpStatusCode code, string body = "") { _code = code; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(_code);
            if (!string.IsNullOrEmpty(_body))
                resp.Content = new StringContent(_body, Encoding.UTF8, "application/json");
            return Task.FromResult(resp);
        }
    }
}