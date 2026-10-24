#nullable enable
using System;
using System.Collections.Generic;

namespace PowerSimBuilder.Physics;

/// <summary>One downloadable item from a catalog or GitHub release.</summary>
public sealed record CatalogItem(
    string Name,
    string Url,
    long? SizeBytes = null,
    string? License = null,
    string? Description = null);

/// <summary>Result of loading a catalog from JSON or GitHub API.</summary>
public sealed record CatalogLoadResult(
    bool Success,
    IReadOnlyList<CatalogItem> Items,
    string? ErrorMessage)
{
    public static CatalogLoadResult Ok(IReadOnlyList<CatalogItem> items)
        => new(true, items, null);

    public static CatalogLoadResult Fail(string message)
        => new(false, Array.Empty<CatalogItem>(), message);
}

/// <summary>Source type detected from a URL.</summary>
public enum CatalogSourceKind { Json, GitHub, Unknown }

/// <summary>Normalized URL + detected source kind.</summary>
public sealed record CatalogSourceInfo(CatalogSourceKind Kind, string NormalizedUrl);