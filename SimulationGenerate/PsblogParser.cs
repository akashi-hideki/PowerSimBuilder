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
            var repoRoot = FindRepoRoot(Path.GetDirectoryName(fullPath));
            if (repoRoot == null)
                throw new PsblogParseException(
                    "repo root (containing src/python_runtime) not found");

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
                    WorkingDirectory = repoRoot,
                };
                psi.EnvironmentVariables["PYTHONPATH"] =
                    Path.Combine(repoRoot, "src");
                psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                using var p = Process.Start(psi);
                if (p == null)
                    throw new PsblogParseException("failed to start python");

                var stderrSb = new StringBuilder();
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

                if (p.ExitCode != 0)
                    throw new PsblogParseException(
                        $"python exited with code {p.ExitCode}: {stderrSb}");

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

        private static string? FindRepoRoot(string? startDir)
        {
            if (string.IsNullOrEmpty(startDir)) return null;
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "src", "python_runtime")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return null;
        }
    }

    public class PsblogParseException : Exception
    {
        public PsblogParseException(string message) : base(message) { }
    }
}
