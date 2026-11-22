using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PowerSimBuilder.Physics
{
    /// <summary>
    /// Browses FMU directories using GitHub Contents API (no tree truncation).
    /// Port of Python's GitHubCatalogClient from fmu_catalog_browser.py.
    /// </summary>
    public class GitHubCatalogClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private string _branch;

        public GitHubCatalogClient(string apiKey = "", DownloadOptions options = null)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient(FmuDownloader.CreateDefaultHandler(options ?? new DownloadOptions()))
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            ConfigureHeaders();
        }

        public void Dispose() => _httpClient.Dispose();

        private void ConfigureHeaders()
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PowerSimBuilder-FMU-Browser");
            
            if (!string.IsNullOrEmpty(_apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_apiKey}");
            }
        }

        private async Task<JsonElement> ApiGetAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                
                if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    var rateLimited = response.Headers.TryGetValues("X-RateLimit-Remaining", out var rem)
                                      && rem.FirstOrDefault() == "0";
                    throw new InvalidOperationException(rateLimited
                        ? "GitHub API rate limit exceeded. Set an API key (github.com/settings/tokens)."
                        : "GitHub API error: 403 Forbidden");
                }
                
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"GitHub API error: {(int)response.StatusCode} {response.ReasonPhrase}");
                }
                
                var content = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(content);
                return doc.RootElement.Clone();
            }
            catch (HttpRequestException ex)
            {
                throw new InvalidOperationException($"Network error: {ex.Message}");
            }
        }

        private async Task<string> GetBranchAsync(string repo)
        {
            if (!string.IsNullOrEmpty(_branch))
                return _branch;
            
            var data = await ApiGetAsync($"https://api.github.com/repos/{repo}");
            _branch = data.GetProperty("default_branch").GetString() ?? "main";
            return _branch;
        }

        private async Task<JsonElement> GetContentsAsync(string repo, string path)
        {
            var branch = await GetBranchAsync(repo);
            return await ApiGetAsync($"https://api.github.com/repos/{repo}/contents/{path}?ref={branch}");
        }

        /// <summary>
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
                    $"Path not found: {parentPath}\nCheck if the repo has this FMI version/type combination.");
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
                    $"Path not found: {basePath}\nCheck if the repo has this platform.");
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
}