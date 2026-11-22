#!/usr/bin/env python3
"""
patch_fmu_catalog_v2.py  -  FMU Catalog Browser: fix listing for the FMI Cross-Check layout.

  fmus/{ver}/{type}/{platform}/{Tool}/{ToolVersion}/{Model}/{Model}.fmu

Old code assumed {Tool}/{Model}/files/model.fmu, so tool *versions* were listed as models
(and de-duplicated by name -> only a few dozen entries, with broken URLs).

New FetchAsync: 3 API calls total (repo, contents of fmus/{ver}/{type}, git tree of the platform dir),
complete list, file sizes, correct download URLs, sorted by name then tool.

    python tools\\patch_fmu_catalog_v2.py --dry-run
    python tools\\patch_fmu_catalog_v2.py

Idempotent, atomic (nothing is written if an edit fails), backup in tmp\\patch_backup_<time>\\,
BOM / CRLF preserved.
"""
from __future__ import annotations
import argparse, datetime, re, shutil, sys
from pathlib import Path


class PatchError(Exception):
    pass


def read_text(path):
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    crlf = "\r\n" in text
    return text.replace("\r\n", "\n"), bom, crlf


def write_text(path, text, bom, crlf):
    if crlf:
        text = text.replace("\n", "\r\n")
    data = text.encode("utf-8")
    path.write_bytes((b"\xef\xbb\xbf" if bom else b"") + data)


def replace_once(text, old, new, label):
    n = text.count(old)
    if n != 1:
        raise PatchError(f"{label}: anchor found {n} time(s), expected 1")
    return text.replace(old, new)


# ------------------------------------------------------------------ GitHubCatalogClient.cs
def client_using(t):
    return replace_once(t, "using System.Collections.Generic;\n",
                        "using System.Collections.Generic;\nusing System.Linq;\n", "using System.Linq")


def client_403(t):
    old = '''                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new InvalidOperationException(
                        "GitHub API rate limit exceeded. Set an API key (github.com/settings/tokens).");
                }
'''
    new = '''                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    var rateLimited = response.Headers.TryGetValues("X-RateLimit-Remaining", out var rem)
                                      && rem.FirstOrDefault() == "0";
                    throw new InvalidOperationException(rateLimited
                        ? "GitHub API rate limit exceeded. Set an API key (github.com/settings/tokens)."
                        : "GitHub API error: 403 Forbidden");
                }
'''
    return replace_once(t, old, new, "403 handling")


NEW_FETCH = '''        /// <summary>
        /// Lists FMUs of the FMI Cross-Check layout
        /// fmus/{ver}/{type}/{platform}/{Tool}/{ToolVersion}/{Model}/{Model}.fmu
        /// with a single git-tree request (complete list; 3 API calls in total).
        /// </summary>
        public async Task<List<FmuModel>> FetchAsync(
            string repo,
            string versionFilter,
            string typeFilter,
            string platformFilter,
            Action<string> logAction = null)
        {
            if (string.IsNullOrEmpty(versionFilter) ||
                string.IsNullOrEmpty(typeFilter) ||
                string.IsNullOrEmpty(platformFilter))
            {
                throw new InvalidOperationException("Filters required (version/type/platform)");
            }

            var branch = await GetBranchAsync(repo);
            var parentPath = $"fmus/{versionFilter}/{typeFilter}";
            var basePath = $"{parentPath}/{platformFilter}";
            logAction?.Invoke($"Fetching: {repo} / {basePath} (branch: {branch})");

            // Step 1: SHA of the platform directory
            JsonElement platforms;
            try
            {
                platforms = await GetContentsAsync(repo, parentPath);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("404"))
            {
                throw new InvalidOperationException(
                    $"Path not found: {parentPath}\\nCheck if the repo has this FMI version/type combination.");
            }

            string treeSha = null;
            foreach (var p in platforms.EnumerateArray())
            {
                if (p.GetProperty("type").GetString() == "dir" &&
                    p.GetProperty("name").GetString() == platformFilter)
                {
                    treeSha = p.GetProperty("sha").GetString();
                    break;
                }
            }
            if (string.IsNullOrEmpty(treeSha))
            {
                throw new InvalidOperationException(
                    $"Path not found: {basePath}\\nCheck if the repo has this platform.");
            }

            // Step 2: the whole platform subtree in ONE request
            logAction?.Invoke("Reading directory tree...");
            var tree = await ApiGetAsync(
                $"https://api.github.com/repos/{repo}/git/trees/{treeSha}?recursive=1");
            if (tree.TryGetProperty("truncated", out var tr) && tr.ValueKind == JsonValueKind.True)
            {
                throw new InvalidOperationException(
                    "GitHub returned a truncated tree; the list would be incomplete.");
            }

            var models = new List<FmuModel>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in tree.GetProperty("tree").EnumerateArray())
            {
                if (entry.GetProperty("type").GetString() != "blob") continue;
                var path = entry.GetProperty("path").GetString() ?? "";
                if (!path.EndsWith(".fmu", StringComparison.OrdinalIgnoreCase)) continue;

                var seg = path.Split('/');
                if (seg.Length != 4) continue;          // Tool / Version / Model / Model.fmu
                if (!seen.Add(path)) continue;

                long size = 0;
                if (entry.TryGetProperty("size", out var sz) &&
                    sz.ValueKind == JsonValueKind.Number && sz.TryGetInt64(out var szv))
                {
                    size = szv;
                }

                var encoded = string.Join("/", seg.Select(Uri.EscapeDataString));
                models.Add(new FmuModel
                {
                    Name = seg[2],
                    DownloadUrl = $"https://raw.githubusercontent.com/{repo}/{branch}/{basePath}/{encoded}",
                    FmiVersion = versionFilter,
                    FmiType = typeFilter == "cs" ? "CoSimulation" : "ModelExchange",
                    Platform = platformFilter,
                    SizeBytes = size,
                    Source = $"{seg[0]} {seg[1]}"
                });
            }

            models.Sort((a, b) =>
            {
                var c = string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
                return c != 0 ? c : string.Compare(a.Source, b.Source, StringComparison.OrdinalIgnoreCase);
            });

            logAction?.Invoke($"Found {models.Count} FMU(s)");
            return models;
        }
    }
}'''


def client_fetch(t):
    m = re.search(r"        /// <summary>\n        /// Browse fmus/\{ver\}.*\Z", t, re.S)
    if not m:
        raise PatchError("FetchAsync: start anchor not found")
    tail = t[m.start():]
    ws = tail[len(tail.rstrip()):]
    return t[:m.start()] + NEW_FETCH + ws


# ------------------------------------------------------------------ PhysicsImportForm.CatalogTab.cs
def tab_items(t):
    old = '''            var items = models
                .Select(m => new CatalogItem(m.Name, m.DownloadUrl, null, null, m.Source))
                .ToList();
'''
    new = '''            var items = models
                .Select(m => new CatalogItem(m.Name, m.DownloadUrl,
                    m.SizeBytes > 0 ? m.SizeBytes : (long?)null, null, m.Source))
                .ToList();
'''
    return replace_once(t, old, new, "catalog item size")


STEPS = [
    ("parts/Physics/GitHubCatalogClient.cs", "using System.Linq", "using System.Linq;", client_using),
    ("parts/Physics/GitHubCatalogClient.cs", "403 / rate-limit detection", "X-RateLimit-Remaining", client_403),
    ("parts/Physics/GitHubCatalogClient.cs", "FetchAsync (3-level layout, git tree)", "git/trees/", client_fetch),
    ("parts/PhysicsImportForm.CatalogTab.cs", "show file size", "m.SizeBytes > 0", tab_items),
]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default=None)
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    root = Path(args.root).resolve() if args.root else Path(__file__).resolve().parent.parent

    files, results, failed = {}, [], False
    for rel, label, marker, fn in STEPS:
        p = root / rel
        if not p.exists():
            results.append(("FAIL", rel, label, "file not found")); failed = True; continue
        if rel not in files:
            t, bom, crlf = read_text(p)
            files[rel] = [t, bom, crlf, t]
        cur = files[rel][0]
        if marker in cur:
            results.append(("SKIP", rel, label, "already applied")); continue
        try:
            files[rel][0] = fn(cur)
            results.append(("APPLY", rel, label, ""))
        except PatchError as ex:
            results.append(("FAIL", rel, label, str(ex))); failed = True

    print(f"Root: {root}\n")
    for st, rel, label, d in results:
        print(f"  [{st:5}] {rel}  ({label}) {d}")
    if failed:
        print("\nABORTED: a required edit failed. No files were changed.")
        return 1

    changed = [r for r, (t, b, c, o) in files.items() if t != o]
    if args.dry_run:
        print(f"\nDRY RUN: {len(changed)} file(s) would change.")
        return 0
    if changed:
        bdir = root / "tmp" / f"patch_backup_{datetime.datetime.now():%Y%m%d_%H%M%S}"
        for rel in changed:
            dst = bdir / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(root / rel, dst)
        print(f"\nBackup: {bdir}")
        for rel in changed:
            t, bom, crlf, _ = files[rel]
            write_text(root / rel, t, bom, crlf)
    for rel, label, marker, fn in STEPS:
        if marker not in read_text(root / rel)[0]:
            print(f"VERIFY FAILED: {rel} ({label})")
            return 2
    print(f"\nDone: {len(changed)} file(s) changed.")
    print("Next: dotnet build -c Debug ; dotnet test PowerSimBuilder.Tests\\PowerSimBuilder.Tests.csproj")
    return 0


if __name__ == "__main__":
    sys.exit(main())
