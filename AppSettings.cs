using System;
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
                var json = JsonSerializer.Serialize(new { language = Language });
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
                if (two == "zh") return "zh";
                if (two == "ja") return "ja";
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
