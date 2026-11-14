using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace PowerSimBuilder
{
    /// <summary>
    /// Wraps <c>python -m python_runtime.psblog --json-out</c>.
    /// Trap #49: JSON is written to a temp file (not stdout pipe).
    /// </summary>
    public static class PsblogParser
    {
        public static PsblogTraceDto Parse(string psblogPath, bool includeClassifications = false, int timeoutMs = 30000)
        {
            if (string.IsNullOrWhiteSpace(psblogPath))
                throw new PsblogParseException("psblog path is empty");
            if (!File.Exists(psblogPath))
                throw new PsblogParseException($"psblog file not found: {psblogPath}");

            var fullPath = Path.GetFullPath(psblogPath);

            // Bug fix: search repo root from the app base directory, not
            // from the psblog file location. User logs typically live
            // outside the repo; walking up from them never finds src/python_runtime.
            var repoRoot = FindRepoRoot();

            var tmpJson = Path.Combine(
                Path.GetTempPath(), $"psb_{Guid.NewGuid():N}.json");
            try
            {
                var psi = new ProcessStartInfo("python")
                {
                    Arguments =
                        $"-m python_runtime.psblog \"{fullPath}\" --json-out \"{tmpJson}\""
                        + (includeClassifications ? " --classify" : ""),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };

                // Only set PYTHONPATH if the repo's src/ exists.
                // Installed package mode (pip install) provides python_runtime
                // on sys.path already; PYTHONPATH is unnecessary.
                if (repoRoot != null)
                {
                    var srcDir = Path.Combine(repoRoot, "src");
                    if (Directory.Exists(srcDir))
                        psi.EnvironmentVariables["PYTHONPATH"] = srcDir;
                }
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                using var p = Process.Start(psi);
                if (p == null)
                    throw new PsblogParseException("failed to start python");

                var stdoutSb = new StringBuilder();
                var stderrSb = new StringBuilder();
                p.OutputDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        stdoutSb.AppendLine(e.Data);
                };
                p.ErrorDataReceived += (_, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        stderrSb.AppendLine(e.Data);
                };
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                if (!p.WaitForExit(timeoutMs))
                {
                    try { p.Kill(); } catch { /* ignore */ }
                    throw new PsblogParseException(
                        $"python timed out after {timeoutMs} ms");
                }

                // The only fatal condition: python -m python_runtime.psblog
                // exited non-zero (parser error, import error, etc).
                if (p.ExitCode != 0)
                    throw new PsblogParseException(
                        $"psblog parser failed (exit {p.ExitCode}).\n"
                        + $"stderr:\n{stderrSb}\nstdout:\n{stdoutSb}");

                if (!File.Exists(tmpJson))
                    throw new PsblogParseException(
                        "python did not produce JSON output");

                var json = File.ReadAllText(tmpJson, Encoding.UTF8);
                var dto = JsonSerializer.Deserialize<PsblogTraceDto>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return dto ?? throw new PsblogParseException(
                    "failed to deserialize JSON");
            }
            finally
            {
                if (File.Exists(tmpJson))
                {
                    try { File.Delete(tmpJson); } catch { /* ignore */ }
                }
            }
        }

        /// <summary>
        /// Locate the repo root (containing <c>src/python_runtime</c>) by walking up
        /// from the application base directory, then from the current directory.
        /// Returns <c>null</c> when not found (installed-package mode); the caller
        /// continues and lets python report errors.
        /// </summary>
        private static string? FindRepoRoot()
        {
            foreach (var seed in new[]
                     {
                         AppContext.BaseDirectory,
                         Directory.GetCurrentDirectory(),
                     })
            {
                if (string.IsNullOrEmpty(seed))
                    continue;
                DirectoryInfo? dir;
                try { dir = new DirectoryInfo(seed); }
                catch { continue; }
                while (dir != null)
                {
                    if (Directory.Exists(
                            Path.Combine(dir.FullName, "src", "python_runtime")))
                        return dir.FullName;
                    dir = dir.Parent;
                }
            }
            return null;
        }
    }

    public class PsblogParseException : Exception
    {
        public PsblogParseException(string message) : base(message) { }
    }
}