using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class FmuDownloaderTests
{
    private const string TestUrl = "https://example.com/model.fmu";

    private sealed class MockHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
        public MockHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
            => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => _handler(request, ct);
    }

    private static FmuDownloader CreateWithHandler(MockHandler handler)
        => new FmuDownloader(new DownloadOptions(), handler);

    private static HttpResponseMessage Ok(byte[] body, string contentType = "application/octet-stream")
    {
        var content = new ByteArrayContent(body);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
    }

    private static string TempFmuPath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "psb_fmu_dl_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "test.fmu");
    }

    [Fact]
    public async Task DownloadAsync_Success_WritesFile()
    {
        var payload = new byte[] { 1, 2, 3, 4, 5 };
        using var dl = CreateWithHandler(new MockHandler((req, ct) => Task.FromResult(Ok(payload))));
        var dest = TempFmuPath();

        var result = await dl.DownloadAsync(TestUrl, dest);

        Assert.True(result.Success);
        Assert.Equal(payload.Length, result.BytesDownloaded);
        Assert.True(File.Exists(dest));
        Assert.Equal(payload, File.ReadAllBytes(dest));
    }

    [Fact]
    public async Task DownloadAsync_404_ReturnsError()
    {
        using var dl = CreateWithHandler(new MockHandler((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))));
        var dest = TempFmuPath();

        var result = await dl.DownloadAsync(TestUrl, dest);

        Assert.False(result.Success);
        Assert.Contains("404", result.ErrorMessage);
        Assert.False(File.Exists(dest));
    }

    [Fact]
    public async Task DownloadAsync_Timeout_ReturnsTimeoutError()
    {
        var options = new DownloadOptions { TimeoutSeconds = 1 };
        using var dl = new FmuDownloader(options, new MockHandler(async (req, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return Ok(new byte[] { 1 });
        }));
        var dest = TempFmuPath();

        var result = await dl.DownloadAsync(TestUrl, dest);

        Assert.False(result.Success);
        Assert.Contains("Timed out", result.ErrorMessage);
    }

    [Fact]
    public async Task DownloadAsync_Cancelled_ReturnsCancelledError()
    {
        using var dl = CreateWithHandler(new MockHandler(async (req, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return Ok(new byte[] { 1 });
        }));
        var dest = TempFmuPath();

        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var result = await dl.DownloadAsync(TestUrl, dest, null, cts.Token);

        Assert.False(result.Success);
        Assert.Contains("Cancelled", result.ErrorMessage);
    }

    [Fact]
    public void CreateDefaultHandler_NoProxy_DoesNotUseProxy()
    {
        var handler = FmuDownloader.CreateDefaultHandler(new DownloadOptions());
        var hch = Assert.IsType<HttpClientHandler>(handler);
        // Default HttpClientHandler.UseProxy is true (uses system proxy), but Proxy is null (system default)
        Assert.Null(hch.Proxy);
    }

    [Fact]
    public void CreateDefaultHandler_WithProxy_ConfiguresProxy()
    {
        var options = new DownloadOptions { ProxyHost = "proxy.local", ProxyPort = 8080 };
        var handler = FmuDownloader.CreateDefaultHandler(options);
        var hch = Assert.IsType<HttpClientHandler>(handler);
        Assert.True(hch.UseProxy);
        Assert.NotNull(hch.Proxy);
        var wp = Assert.IsType<WebProxy>(hch.Proxy);
        Assert.Equal(new Uri("http://proxy.local:8080"), wp.Address);
    }
}