using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace PowerSimBuilder
{
    /// <summary>
    /// User-level settings persisted to
    /// %APPDATA%\PowerSimBuilder\settings.json.
    /// Currently only the UI language is stored.
    /// </summary>
    public static class AppSettings
    {
        private static readonly string _settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PowerSimBuilder", "settings.json");

        public static string Language { get; set; } = "";

        // ---- MVP-β (v1.4.0) download settings ----
        public static int DownloadTimeoutSeconds { get; set; } = 30;
        public static string ProxyHost { get; set; } = "";
        public static int ProxyPort { get; set; } = 0;
        public static List<string> AllowedUrls { get; set; } = new();

        // ---- workspace (v1.8.0) ----
        /// <summary>
        /// Default workspace directory for project save/load.
        /// Defaults to %USERPROFILE%\Documents\PowerSimBuilder\workspace.
        /// </summary>
        public static string WorkspaceRoot { get; set; } = DefaultWorkspaceRoot;

        public static string DefaultWorkspaceRoot => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "PowerSimBuilder", "workspace");

        public static void Load()
        {
            try
            {
                if (!File.Exists(_settingsPath)) return;
                var json = File.ReadAllText(_settingsPath);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out var langProp)
                    && langProp.ValueKind == JsonValueKind.String)
                {
                    Language = langProp.GetString() ?? "";
                }

                // MVP-β (v1.4.0) download settings (backwards compatible)
                var root = doc.RootElement;
                if (root.TryGetProperty("downloadTimeoutSeconds", out var tProp)
                    && tProp.ValueKind == JsonValueKind.Number
                    && tProp.TryGetInt32(out var timeout) && timeout > 0)
                {
                    DownloadTimeoutSeconds = timeout;
                }
                if (root.TryGetProperty("proxyHost", out var phProp)
                    && phProp.ValueKind == JsonValueKind.String)
                {
                    ProxyHost = phProp.GetString() ?? "";
                }
                if (root.TryGetProperty("proxyPort", out var ppProp)
                    && ppProp.ValueKind == JsonValueKind.Number
                    && ppProp.TryGetInt32(out var port) && port >= 0)
                {
                    ProxyPort = port;
                }
                if (root.TryGetProperty("allowedUrls", out var urlsProp)
                    && urlsProp.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<string>();
                    foreach (var item in urlsProp.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String)
                        {
                            var s = item.GetString();
                            if (!string.IsNullOrEmpty(s)) list.Add(s);
                        }
                    }
                    AllowedUrls = list;
                }
                // workspace (v1.8.0)
                if (root.TryGetProperty("workspaceRoot", out var wrProp)
                    && wrProp.ValueKind == JsonValueKind.String)
                {
                    var ws = wrProp.GetString();
                    if (!string.IsNullOrEmpty(ws)) WorkspaceRoot = ws;
                }
            }
            catch { /* best-effort */ }
        }

        public static void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);
                var json = JsonSerializer.Serialize(new
                {
                    language = Language,
                    downloadTimeoutSeconds = DownloadTimeoutSeconds,
                    proxyHost = ProxyHost ?? "",
                    proxyPort = ProxyPort,
                    allowedUrls = AllowedUrls ?? new List<string>(),
                    workspaceRoot = WorkspaceRoot ?? ""
                });
                File.WriteAllText(_settingsPath, json);
            }
            catch { /* best-effort */ }
        }

        /// <summary>
        /// Detects the OS UI language and returns a supported language code
        /// ("en", "zh", or "ja"). Falls back to "en".
        /// </summary>
        public static string DetectOsLanguage()
        {
            try
            {
                var two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                // Only languages with a bundled i18n file are returned here.
                // Japanese is intentionally not returned: the English-only UI
                // policy means "ja" would result in an empty string table.
                if (two == "zh") return "zh";
                return "en";
            }
            catch
            {
                return "en";
            }
        }

        /// <summary>
        /// Returns the language to use at startup:
        /// - user preference if set
        /// - otherwise OS language if supported
        /// - otherwise "en"
        /// </summary>
        public static string ResolveStartupLanguage()
        {
            if (!string.IsNullOrWhiteSpace(Language)) return Language;
            return DetectOsLanguage();
        }
    }
}
