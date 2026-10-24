#nullable enable
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PowerSimBuilder.Physics;

/// <summary>Download configuration for FmuDownloader.</summary>
public sealed record DownloadOptions
{
    public int TimeoutSeconds { get; init; } = 30;
    public string ProxyHost { get; init; } = "";
    public int ProxyPort { get; init; } = 0;
}

/// <summary>Result of an FMU download attempt.</summary>
public sealed record DownloadResult(
    bool Success,
    string? ActualUrl,
    long BytesDownloaded,
    string? ErrorMessage)
{
    public static DownloadResult Ok(string url, long bytes) => new(true, url, bytes, null);
    public static DownloadResult Fail(string url, string error) => new(false, url, 0, error);
}

/// <summary>
/// Downloads an FMU from a URL. Supports custom timeout and HTTP proxy.
/// All errors are returned as DownloadResult.Fail (no exceptions escape).
/// </summary>
public sealed class FmuDownloader : IDisposable
{
    private readonly HttpClient _http;

    public FmuDownloader(DownloadOptions options)
        : this(options, null)
    {
    }

    public FmuDownloader(DownloadOptions options, HttpMessageHandler? handler)
    {
        var httpHandler = handler ?? CreateDefaultHandler(options);
        _http = new HttpClient(httpHandler)
        {
            Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds))
        };
    }

    public static HttpMessageHandler CreateDefaultHandler(DownloadOptions options)
    {
        var handler = new HttpClientHandler();
        if (!string.IsNullOrEmpty(options.ProxyHost) && options.ProxyPort > 0)
        {
            handler.Proxy = new WebProxy(options.ProxyHost, options.ProxyPort);
            handler.UseProxy = true;
        }
        return handler;
    }

    public async Task<DownloadResult> DownloadAsync(
        string url,
        string destPath,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return DownloadResult.Fail(url ?? "", "URL is required.");
        if (string.IsNullOrWhiteSpace(destPath))
            return DownloadResult.Fail(url, "Destination path is required.");

        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                return DownloadResult.Fail(url, $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");

            var total = response.Content.Headers.ContentLength ?? -1;
            long received = 0;
            var buffer = new byte[81920];

            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            await using (var input = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var output = File.Create(destPath))
            {
                int n;
                while ((n = await input.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                {
                    await output.WriteAsync(buffer, 0, n, ct).ConfigureAwait(false);
                    received += n;
                    if (total > 0)
                        progress?.Report((int)(received * 100 / total));
                }
            }

            return DownloadResult.Ok(url, received);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            TryDelete(destPath);
            return DownloadResult.Fail(url, "Timed out.");
        }
        catch (OperationCanceledException)
        {
            TryDelete(destPath);
            return DownloadResult.Fail(url, "Cancelled.");
        }
        catch (HttpRequestException ex)
        {
            TryDelete(destPath);
            return DownloadResult.Fail(url, ex.Message);
        }
        catch (Exception ex)
        {
            TryDelete(destPath);
            return DownloadResult.Fail(url, ex.Message);
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public void Dispose() => _http.Dispose();
}
