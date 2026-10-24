#nullable enable
using System;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Detects catalog source type from a user-provided URL.
/// JSON catalog: any URL ending with .json
/// GitHub:      https://github.com/{user}/{repo}[/releases...]
/// </summary>
public static class PhysicsCatalogSource
{
    private static readonly Regex GithubPattern = new Regex(
        @"^https?://github\.com/(?<user>[^/]+)/(?<repo>[^/]+?)(?:\.git)?(?:/releases(?:/.*)?)?/?$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static CatalogSourceInfo Detect(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return new CatalogSourceInfo(CatalogSourceKind.Unknown, "");

        var trimmed = url.Trim();
        if (GithubPattern.IsMatch(trimmed))
            return new CatalogSourceInfo(CatalogSourceKind.GitHub, trimmed);
        if (trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains(".json?", StringComparison.OrdinalIgnoreCase))
            return new CatalogSourceInfo(CatalogSourceKind.Json, trimmed);
        return new CatalogSourceInfo(CatalogSourceKind.Unknown, trimmed);
    }
}