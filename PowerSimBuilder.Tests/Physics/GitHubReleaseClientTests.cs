#nullable enable
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class GitHubReleaseClientTests
{
    private const string ValidResponse = @"{
        ""tag_name"": ""v0.0.39"",
        ""assets"": [
            { ""name"": ""Dahlquist.fmu"", ""browser_download_url"": ""https://dl.ex.com/Dahlquist.fmu"", ""size"": 184603 },
            { ""name"": ""Readme.txt"", ""browser_download_url"": ""https://dl.ex.com/Readme.txt"", ""size"": 100 },
            { ""name"": ""Feedthrough.fmu"", ""browser_download_url"": ""https://dl.ex.com/Feedthrough.fmu"", ""size"": 191904 }
        ]
    }";

    [Fact]
    public void TryParseRepo_Valid()
    {
        Assert.True(GitHubReleaseClient.TryParseRepo("https://github.com/modelica/Reference-FMUs", out var u, out var r));
        Assert.Equal("modelica", u);
        Assert.Equal("Reference-FMUs", r);
    }

    [Fact]
    public void TryParseRepo_WithReleases()
    {
        Assert.True(GitHubReleaseClient.TryParseRepo("https://github.com/user/repo/releases/latest", out var u, out var r));
        Assert.Equal("user", u);
        Assert.Equal("repo", r);
    }

    [Fact]
    public void TryParseRepo_WithGitSuffix()
    {
        Assert.True(GitHubReleaseClient.TryParseRepo("https://github.com/user/repo.git", out _, out var r));
        Assert.Equal("repo", r);
    }

    [Fact]
    public void TryParseRepo_NotGitHub()
    {
        Assert.False(GitHubReleaseClient.TryParseRepo("https://example.com/foo", out _, out _));
    }

    [Fact]
    public async Task ListFmuAssets_ReturnsOnlyFmu()
    {
        using var client = new GitHubReleaseClient(new MockHandler(HttpStatusCode.OK, ValidResponse));
        var r = await client.ListFmuAssetsAsync("https://github.com/user/repo");
        Assert.True(r.Success);
        Assert.Equal(2, r.Items.Count);
        Assert.Equal("Dahlquist.fmu", r.Items[0].Name);
        Assert.Equal("Feedthrough.fmu", r.Items[1].Name);
    }

    [Fact]
    public async Task ListFmuAssets_404_ReturnsError()
    {
        using var client = new GitHubReleaseClient(new MockHandler(HttpStatusCode.NotFound));
        var r = await client.ListFmuAssetsAsync("https://github.com/user/repo");
        Assert.False(r.Success);
        Assert.Contains("404", r.ErrorMessage!);
    }

    [Fact]
    public async Task ListFmuAssets_RateLimit_ReturnsError()
    {
        var resp = new HttpResponseMessage((HttpStatusCode)403);
        resp.Headers.Add("X-RateLimit-Remaining", "0");
        using var handler = new FixedHandler(resp);
        using var client = new GitHubReleaseClient(handler);
        var r = await client.ListFmuAssetsAsync("https://github.com/user/repo");
        Assert.False(r.Success);
        Assert.Contains("rate limit", r.ErrorMessage!, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ListFmuAssets_NonGithub_ReturnsError()
    {
        using var client = new GitHubReleaseClient(new MockHandler(HttpStatusCode.OK, ValidResponse));
        var r = await client.ListFmuAssetsAsync("https://example.com/foo");
        Assert.False(r.Success);
    }

    private sealed class MockHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _body;
        public MockHandler(HttpStatusCode code, string body = "") { _code = code; _body = body; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var resp = new HttpResponseMessage(_code);
            if (!string.IsNullOrEmpty(_body))
                resp.Content = new StringContent(_body, Encoding.UTF8, "application/json");
            return Task.FromResult(resp);
        }
    }

    private sealed class FixedHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;
        public FixedHandler(HttpResponseMessage r) { _response = r; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_response);
    }
}