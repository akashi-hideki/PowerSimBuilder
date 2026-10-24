#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Lists .fmu assets from the latest GitHub release of a repository.
/// Requires the repo to publish FMUs as release assets (not zipped).
/// </summary>
public sealed class GitHubReleaseClient : IDisposable
{
    private static readonly Regex RepoPattern = new Regex(
        @"^https?://github\.com/(?<user>[^/]+)/(?<repo>[^/]+?)(?:\.git)?(?:/releases(?:/.*)?)?/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HttpClient _http;

    public GitHubReleaseClient() : this(null) { }

    public GitHubReleaseClient(HttpMessageHandler? handler)
    {
        _http = handler == null
            ? new HttpClient { Timeout = TimeSpan.FromSeconds(30) }
            : new HttpClient(handler);
        _http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "PowerSimBuilder/1.x");
    }

    /// <summary>
    /// Parses a GitHub repo URL into (user, repo).
    /// Accepts: https://github.com/u/r  and  /releases[/latest|/tag/X]
    /// </summary>
    public static bool TryParseRepo(string? url, out string user, out string repo)
    {
        user = ""; repo = "";
        if (string.IsNullOrWhiteSpace(url)) return false;
        var m = RepoPattern.Match(url.Trim());
        if (!m.Success) return false;
        user = m.Groups["user"].Value;
        repo = m.Groups["repo"].Value;
        if (repo.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
            repo = repo.Substring(0, repo.Length - 4);
        return user.Length > 0 && repo.Length > 0;
    }

    public async Task<CatalogLoadResult> ListFmuAssetsAsync(
        string? repoUrl,
        CancellationToken ct = default)
    {
        if (!TryParseRepo(repoUrl, out var user, out var repo))
            return CatalogLoadResult.Fail("Not a GitHub repository URL.");

        var apiUrl = $"https://api.github.com/repos/{user}/{repo}/releases/latest";
        try
        {
            using var response = await _http.GetAsync(apiUrl, ct).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                if ((int)response.StatusCode == 403
                    && response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining)
                    && remaining.FirstOrDefault() == "0")
                {
                    return CatalogLoadResult.Fail("GitHub API rate limit exceeded.");
                }
                return CatalogLoadResult.Fail(
                    $"GitHub API: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            }

            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(text);
            if (!doc.RootElement.TryGetProperty("assets", out var assets)
                || assets.ValueKind != JsonValueKind.Array)
                return CatalogLoadResult.Fail("No assets in latest release.");

            var items = new List<CatalogItem>();
            foreach (var a in assets.EnumerateArray())
            {
                if (a.ValueKind != JsonValueKind.Object) continue;
                var name = GetString(a, "name");
                var url = GetString(a, "browser_download_url");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url)) continue;
                if (!name.EndsWith(".fmu", StringComparison.OrdinalIgnoreCase)) continue;

                long? size = null;
                if (a.TryGetProperty("size", out var sp)
                    && sp.ValueKind == JsonValueKind.Number
                    && sp.TryGetInt64(out var sv) && sv > 0)
                    size = sv;

                items.Add(new CatalogItem(
                    name,
                    url,
                    size,
                    null,
                    GetString(a, "label")));
            }
            return CatalogLoadResult.Ok(items);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return CatalogLoadResult.Fail("Timed out.");
        }
        catch (HttpRequestException ex)
        {
            return CatalogLoadResult.Fail(ex.Message);
        }
        catch (JsonException ex)
        {
            return CatalogLoadResult.Fail("Invalid GitHub API response: " + ex.Message);
        }
        catch (Exception ex)
        {
            return CatalogLoadResult.Fail(ex.Message);
        }
    }

    private static string? GetString(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        return null;
    }

    public void Dispose() => _http.Dispose();
}