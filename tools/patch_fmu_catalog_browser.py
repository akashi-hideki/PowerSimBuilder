#!/usr/bin/env python3
"""
patch_fmu_catalog_browser.py
FMU Catalog Browser (v1.11.0) - apply source patches to PowerSimBuilder.

Place at  tools\\patch_fmu_catalog_browser.py  and run from the repo root:
    python tools\\patch_fmu_catalog_browser.py --dry-run     # preview only
    python tools\\patch_fmu_catalog_browser.py               # apply

Properties
  * Idempotent : an edit whose marker is already present is skipped.
  * Atomic     : if any C# edit fails, NOTHING is written.
  * Backup     : originals are copied to tmp\\patch_backup_<timestamp>\\ first.
  * Encoding   : BOM and CRLF/LF of each file are preserved.
  * JSON edits (i18n / catalog_sources) are best-effort: on failure they only warn.
"""
from __future__ import annotations
import argparse, datetime, json, re, shutil, sys
from pathlib import Path


class PatchError(Exception):
    pass


# ---------------------------------------------------------------- helpers
def read_text(path: Path):
    raw = path.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    crlf = "\r\n" in text
    return text.replace("\r\n", "\n"), bom, crlf


def write_text(path: Path, text: str, bom: bool, crlf: bool):
    if crlf:
        text = text.replace("\n", "\r\n")
    data = text.encode("utf-8")
    if bom:
        data = b"\xef\xbb\xbf" + data
    path.write_bytes(data)


def replace_once(text, old, new, label):
    n = text.count(old)
    if n != 1:
        raise PatchError(f"{label}: anchor found {n} time(s), expected 1")
    return text.replace(old, new)


def regex_once(text, pattern, new, label, flags=0):
    ms = list(re.finditer(pattern, text, flags))
    if len(ms) != 1:
        raise PatchError(f"{label}: pattern matched {len(ms)} time(s), expected 1")
    s, e = ms[0].span()
    return text[:s] + new + text[e:]


# ---------------------------------------------------------------- AppSettings.cs
def appsettings_props(t):
    pat = (r"[ \t]*// FMU Catalog Browser settings \(v1\.11\.0\)\n"
           r"(?:[ \t]*public static string (?:GitHubApiKey|GitHubProxyUrl|FmuOutputDir|LastCatalogSource)\b[^\n]*\n)+")
    new = ('\n        // FMU Catalog Browser (v1.11.0)\n'
           '        public static string GitHubApiKey { get; set; } = "";\n')
    return regex_once(t, pat, new, "AppSettings props", re.M)


def appsettings_load(t):
    old = ("if (!string.IsNullOrEmpty(upm)) UserPhysicsModelsRoot = upm;\n"
           "                }\n")
    new = old + '''                // FMU Catalog Browser (v1.11.0)
                if (root.TryGetProperty("githubApiKey", out var gkProp)
                    && gkProp.ValueKind == JsonValueKind.String)
                {
                    GitHubApiKey = gkProp.GetString() ?? "";
                }
'''
    return replace_once(t, old, new, "AppSettings Load()")


def appsettings_save(t):
    old = 'userPhysicsModelsRoot = UserPhysicsModelsRoot ?? ""\n'
    new = ('userPhysicsModelsRoot = UserPhysicsModelsRoot ?? "",\n'
           '                    githubApiKey = GitHubApiKey ?? ""\n')
    return replace_once(t, old, new, "AppSettings Save()")


# ---------------------------------------------------------------- GitHubCatalogClient.cs
def client_decl(t):
    return replace_once(t, "public class GitHubCatalogClient\n",
                        "public class GitHubCatalogClient : IDisposable\n", "client class decl")


def client_ctor(t):
    pat = r'public GitHubCatalogClient\(string apiKey = ""\)\s*\{.*?ConfigureHeaders\(\);\n        \}\n'
    new = '''public GitHubCatalogClient(string apiKey = "", DownloadOptions options = null)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient(FmuDownloader.CreateDefaultHandler(options ?? new DownloadOptions()))
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            ConfigureHeaders();
        }

        public void Dispose() => _httpClient.Dispose();
'''
    return regex_once(t, pat, new, "client ctor", re.S)


def client_json(t):
    pat = (r"var content = await response\.Content\.ReadAsStringAsync\(\);\s*"
           r"return JsonDocument\.Parse\(content\)\.RootElement;")
    new = ("var content = await response.Content.ReadAsStringAsync();\n"
           "                using var doc = JsonDocument.Parse(content);\n"
           "                return doc.RootElement.Clone();")
    return regex_once(t, pat, new, "client JsonDocument", re.S)


# ---------------------------------------------------------------- PhysicsImportForm.CatalogTab.cs
def tab_field(t):
    old = "    private ComboBox? _cmbCatalogSource;\n"
    return replace_once(t, old, old + "    private TextBox? _txtGitHubApiKey;\n", "tab field")


def tab_ui(t):
    old = "        bottom.Controls.Add(_btnCatalogDownload);\n"
    new = old + '''
        var txtKey = new TextBox
        {
            Width = 220,
            UseSystemPasswordChar = true,
            Name = "TxtGitHubApiKey",
            Text = AppSettings.GitHubApiKey ?? ""
        };
        _txtGitHubApiKey = txtKey;
        var chkShowKey = new CheckBox
        {
            Text = I18n.T("PhysicsCatalog.ChkShowKey"),
            AutoSize = true,
            Name = "ChkShowGitHubApiKey",
            Anchor = AnchorStyles.Left
        };
        chkShowKey.CheckedChanged += (s, e) => txtKey.UseSystemPasswordChar = !chkShowKey.Checked;
        var lblKey = new Label
        {
            Text = I18n.T("PhysicsCatalog.LblApiKey"),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        };
        bottom.Controls.Add(chkShowKey);
        bottom.Controls.Add(txtKey);
        bottom.Controls.Add(lblKey);
'''
    return replace_once(t, old, new, "tab UI")


def tab_switch(t):
    old = '''            var source = PhysicsCatalogSource.Detect(url);
            CatalogLoadResult result;
            switch (source.Kind)
            {
                case CatalogSourceKind.GitHub:
                    using (var gh = new GitHubReleaseClient())
                        result = await gh.ListFmuAssetsAsync(url);
                    break;
                case CatalogSourceKind.Json:
                    result = await PhysicsCatalogRegistry.LoadFromUrlAsync(url);
                    break;
                default:
                    result = CatalogLoadResult.Fail(
                        "URL must end with .json or be a GitHub repository URL.");
                    break;
            }
'''
    new = '''            CatalogLoadResult result;
            if (url.StartsWith("github:", StringComparison.OrdinalIgnoreCase))
            {
                result = await LoadGitHubTreeAsync(url);
            }
            else
            {
                var source = PhysicsCatalogSource.Detect(url);
                switch (source.Kind)
                {
                    case CatalogSourceKind.GitHub:
                        using (var gh = new GitHubReleaseClient())
                            result = await gh.ListFmuAssetsAsync(url);
                        break;
                    case CatalogSourceKind.Json:
                        result = await PhysicsCatalogRegistry.LoadFromUrlAsync(url);
                        break;
                    default:
                        result = CatalogLoadResult.Fail(
                            "URL must end with .json, be a GitHub repository URL, or start with github:");
                        break;
                }
            }
'''
    return replace_once(t, old, new, "tab load switch")


def tab_helper(t):
    anchor = "    private async Task DownloadSelectedAsync()\n"
    new = '''    /// <summary>
    /// "github:{owner/repo}|{fmi version}|{cs|me}|{platform}" -> FMU list via GitHub Contents API.
    /// </summary>
    private async Task<CatalogLoadResult> LoadGitHubTreeAsync(string spec)
    {
        var parts = spec.Substring("github:".Length).Split('|');
        if (parts.Length != 4)
            return CatalogLoadResult.Fail("Invalid source: " + spec);

        var apiKey = _txtGitHubApiKey?.Text?.Trim() ?? "";
        if (apiKey != AppSettings.GitHubApiKey)
        {
            AppSettings.GitHubApiKey = apiKey;
            AppSettings.Save();
        }

        var options = new DownloadOptions
        {
            TimeoutSeconds = AppSettings.DownloadTimeoutSeconds,
            ProxyHost = AppSettings.ProxyHost ?? "",
            ProxyPort = AppSettings.ProxyPort
        };

        try
        {
            using var client = new GitHubCatalogClient(apiKey, options);
            // Continuations resume on the UI thread, so the label can be updated directly.
            var models = await client.FetchAsync(parts[0], parts[1], parts[2], parts[3],
                msg => { if (_lblCatalogStatus != null) _lblCatalogStatus.Text = msg; });

            var items = models
                .Select(m => new CatalogItem(m.Name, m.DownloadUrl, null, null, m.Source))
                .ToList();
            return CatalogLoadResult.Ok(items);
        }
        catch (Exception ex)
        {
            return CatalogLoadResult.Fail(ex.Message);
        }
    }

'''
    return replace_once(t, anchor, new + anchor, "tab helper")


def tab_hook(t):
    old = "    public ComboBox? Test_CmbCatalogSource => _cmbCatalogSource;\n"
    return replace_once(t, old, old + "    public TextBox? Test_TxtGitHubApiKey => _txtGitHubApiKey;\n", "tab test hook")


# ---------------------------------------------------------------- JSON (best effort)
def add_i18n(items):
    def fn(text):
        lines = text.split("\n")
        idx = next((i for i, l in enumerate(lines)
                    if l.lstrip().startswith('"PhysicsCatalog.BtnLoad"')), None)
        if idx is None:
            raise PatchError('anchor key "PhysicsCatalog.BtnLoad" not found (nested or different layout?)')
        indent = re.match(r"\s*", lines[idx]).group(0)
        new = [f"{indent}{json.dumps(k, ensure_ascii=False)}: {json.dumps(v, ensure_ascii=False)}"
               for k, v in items]
        if lines[idx].rstrip().endswith(","):
            new = [l + "," for l in new]
        else:
            lines[idx] = lines[idx].rstrip() + ","
            new = [l + "," for l in new[:-1]] + [new[-1]]
        lines[idx + 1:idx + 1] = new
        out = "\n".join(lines)
        json.loads(out)  # validate
        return out
    return fn


def add_source(text):
    m = re.search(r'"sources"\s*:\s*\[', text)
    if not m:
        raise PatchError('"sources" array not found')
    entry = ('{ "id": "fmi-xc-2-cs-win64", "label": "FMI Cross Check (2.0 / CoSim / win64)", '
             '"url": "github:modelica/fmi-cross-check|2.0|cs|win64" }')
    rest = text[m.end():]
    ws = re.match(r"\s*", rest).group(0) or "\n    "
    if rest.lstrip().startswith("]"):
        out = text[:m.end()] + ws + entry + rest
    else:
        out = text[:m.end()] + ws + entry + "," + rest
    json.loads(out)
    return out


# ---------------------------------------------------------------- plan
# (relative path, label, "done" marker, function, hard?)
STEPS = [
    ("AppSettings.cs", "properties", "// FMU Catalog Browser (v1.11.0)\n        public static string GitHubApiKey", appsettings_props, True),
    ("AppSettings.cs", "Load()", 'out var gkProp', appsettings_load, True),
    ("AppSettings.cs", "Save()", "githubApiKey = GitHubApiKey", appsettings_save, True),
    ("parts/Physics/GitHubCatalogClient.cs", "class decl", ": IDisposable", client_decl, True),
    ("parts/Physics/GitHubCatalogClient.cs", "ctor + Dispose", "DownloadOptions options = null", client_ctor, True),
    ("parts/Physics/GitHubCatalogClient.cs", "JsonDocument dispose", "doc.RootElement.Clone()", client_json, True),
    ("parts/PhysicsImportForm.CatalogTab.cs", "field", "_txtGitHubApiKey;", tab_field, True),
    ("parts/PhysicsImportForm.CatalogTab.cs", "API key UI", '"TxtGitHubApiKey"', tab_ui, True),
    ("parts/PhysicsImportForm.CatalogTab.cs", "load switch", "LoadGitHubTreeAsync(url)", tab_switch, True),
    ("parts/PhysicsImportForm.CatalogTab.cs", "helper method", "Task<CatalogLoadResult> LoadGitHubTreeAsync", tab_helper, True),
    ("parts/PhysicsImportForm.CatalogTab.cs", "test hook", "Test_TxtGitHubApiKey", tab_hook, True),
    ("Resources/i18n/en.json", "i18n en", '"PhysicsCatalog.LblApiKey"',
     add_i18n([("PhysicsCatalog.LblApiKey", "GitHub API Key:"), ("PhysicsCatalog.ChkShowKey", "Show")]), False),
    ("Resources/i18n/zh.json", "i18n zh", '"PhysicsCatalog.LblApiKey"',
     add_i18n([("PhysicsCatalog.LblApiKey", "GitHub API \u5bc6\u94a5:"), ("PhysicsCatalog.ChkShowKey", "\u663e\u793a")]), False),
    ("Resources/catalog_sources.json", "catalog source preset", "fmi-xc-2-cs-win64", add_source, False),
]
DELETE = ["parts/Physics/FmuCatalogInstaller.cs"]


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default=None, help="repo root (default: parent of this script's folder)")
    ap.add_argument("--dry-run", action="store_true")
    args = ap.parse_args()
    root = Path(args.root).resolve() if args.root else Path(__file__).resolve().parent.parent

    files = {}      # rel -> [text, bom, crlf, original_text]
    results = []    # (status, rel, label, detail)
    hard_failed = False

    for rel, label, marker, fn, hard in STEPS:
        p = root / rel
        if not p.exists():
            msg = "file not found"
            results.append(("FAIL" if hard else "WARN", rel, label, msg))
            hard_failed |= hard
            continue
        if rel not in files:
            t, bom, crlf = read_text(p)
            files[rel] = [t, bom, crlf, t]
        cur = files[rel][0]
        if marker in cur:
            results.append(("SKIP", rel, label, "already applied"))
            continue
        try:
            files[rel][0] = fn(cur)
            results.append(("APPLY", rel, label, ""))
        except (PatchError, ValueError) as ex:
            results.append(("FAIL" if hard else "WARN", rel, label, str(ex)))
            hard_failed |= hard

    for rel in DELETE:
        p = root / rel
        results.append(("DELETE" if p.exists() else "SKIP", rel, "remove obsolete file",
                        "" if p.exists() else "not present"))

    print(f"Root: {root}\n")
    for st, rel, label, detail in results:
        print(f"  [{st:6}] {rel}  ({label}) {detail}")

    if hard_failed:
        print("\nABORTED: a required edit failed. No files were changed.")
        print("Send me the FAIL line(s) above together with the current file.")
        return 1

    changed = [rel for rel, (t, b, c, o) in files.items() if t != o]
    to_delete = [r for r in DELETE if (root / r).exists()]

    if args.dry_run:
        print(f"\nDRY RUN: {len(changed)} file(s) would change, {len(to_delete)} would be deleted.")
        return 0

    if changed or to_delete:
        stamp = datetime.datetime.now().strftime("%Y%m%d_%H%M%S")
        bdir = root / "tmp" / f"patch_backup_{stamp}"
        for rel in changed + to_delete:
            dst = bdir / rel
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(root / rel, dst)
        print(f"\nBackup: {bdir}")
        for rel in changed:
            t, bom, crlf, _ = files[rel]
            write_text(root / rel, t, bom, crlf)
        for rel in to_delete:
            (root / rel).unlink()

    # verify
    bad = []
    for rel, label, marker, fn, hard in STEPS:
        p = root / rel
        if p.exists() and marker not in read_text(p)[0]:
            (bad if hard else []).append(f"{rel} ({label})")
    if bad:
        print("VERIFY FAILED for: " + ", ".join(bad))
        return 2

    warns = [r for r in results if r[0] == "WARN"]
    print(f"\nDone: {len(changed)} file(s) changed, {len(to_delete)} deleted.")
    if warns:
        print("\nManual steps needed (JSON files could not be patched automatically):")
        print('  en.json : "PhysicsCatalog.LblApiKey": "GitHub API Key:", "PhysicsCatalog.ChkShowKey": "Show"')
        print('  zh.json : "PhysicsCatalog.LblApiKey": "GitHub API 密钥:", "PhysicsCatalog.ChkShowKey": "显示"')
        print('  Resources/catalog_sources.json : add { "id": "fmi-xc-2-cs-win64", "label": '
              '"FMI Cross Check (2.0 / CoSim / win64)", "url": "github:modelica/fmi-cross-check|2.0|cs|win64" }')
    print("\nNext: dotnet build -c Debug ; dotnet test PowerSimBuilder.Tests\\PowerSimBuilder.Tests.csproj")
    return 0


if __name__ == "__main__":
    sys.exit(main())
