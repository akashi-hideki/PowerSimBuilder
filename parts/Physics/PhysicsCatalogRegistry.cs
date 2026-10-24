#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PowerSimBuilder.Physics;

/// <summary>
/// Loads a catalog of physics models from a JSON file.
/// Schema:
/// {
///   "version": 1,
///   "models": [
///     { "name": "...", "url": "...", "sizeBytes": 123, "license": "...", "description": "..." }
///   ]
/// }
/// </summary>
public static class PhysicsCatalogRegistry
{
    public static CatalogLoadResult LoadFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return CatalogLoadResult.Fail("Catalog JSON is empty.");

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                return CatalogLoadResult.Fail("Catalog root is not an object.");
            if (!root.TryGetProperty("models", out var modelsProp)
                || modelsProp.ValueKind != JsonValueKind.Array)
                return CatalogLoadResult.Fail("Catalog has no 'models' array.");

            var items = new List<CatalogItem>();
            foreach (var m in modelsProp.EnumerateArray())
            {
                if (m.ValueKind != JsonValueKind.Object) continue;
                var name = TryGetString(m, "name");
                var url = TryGetString(m, "url");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(url)) continue;

                long? size = null;
                if (m.TryGetProperty("sizeBytes", out var sp)
                    && sp.ValueKind == JsonValueKind.Number
                    && sp.TryGetInt64(out var sv) && sv > 0)
                    size = sv;

                items.Add(new CatalogItem(
                    name,
                    url,
                    size,
                    TryGetString(m, "license"),
                    TryGetString(m, "description")));
            }
            return CatalogLoadResult.Ok(items);
        }
        catch (JsonException ex)
        {
            return CatalogLoadResult.Fail("Invalid catalog JSON: " + ex.Message);
        }
    }

    public static async Task<CatalogLoadResult> LoadFromUrlAsync(
        string? url,
        HttpMessageHandler? handler = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            return CatalogLoadResult.Fail("URL is required.");

        using var http = handler == null
            ? new HttpClient { Timeout = TimeSpan.FromSeconds(30) }
            : new HttpClient(handler);
        try
        {
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                return CatalogLoadResult.Fail(
                    $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
            var text = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            return LoadFromJson(text);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return CatalogLoadResult.Fail("Timed out.");
        }
        catch (HttpRequestException ex)
        {
            return CatalogLoadResult.Fail(ex.Message);
        }
        catch (Exception ex)
        {
            return CatalogLoadResult.Fail(ex.Message);
        }
    }

    private static string? TryGetString(JsonElement el, string name)
    {
        if (el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String)
        {
            var s = p.GetString();
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }
        return null;
    }
}