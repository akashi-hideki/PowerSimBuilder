using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PowerSimBuilder
{
    /// <summary>
    /// Lightweight i18n. Loads Resources/i18n/{lang}.json (embedded).
    /// Default: "en". Future language (e.g., "zh") can be added by
    /// dropping a JSON file and (optionally) a menu item.
    /// </summary>
    public static class I18n
    {
        public const string DefaultLanguage = "en";

        private static string _currentLanguage = DefaultLanguage;
        private static Dictionary<string, string> _strings =
            new Dictionary<string, string>(StringComparer.Ordinal);

        static I18n() { Initialize(); }

        public static string CurrentLanguage => _currentLanguage;

        public static string T(string key)
        {
            if (_strings.TryGetValue(key, out var value))
                return value;
            try
            {
                var log = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "i18n_missing.log");
                File.AppendAllText(log, $"[{DateTime.Now:HH:mm:ss.fff}] {key}\n");
            }
            catch { }
            return $"[{key}]";
        }

        public static string T(string key, params object[] args)
        {
            var template = T(key);
            try { return string.Format(template, args); }
            catch { return template; }
        }

        public static void SetLanguage(string lang)
        {
            if (string.IsNullOrWhiteSpace(lang)) lang = DefaultLanguage;
            _currentLanguage = lang;
            LoadLanguage(lang);
        }

        public static void Initialize()
        {
            SetLanguage(DefaultLanguage);
        }

        private static void LoadLanguage(string lang)
        {
            var dict = new Dictionary<string, string>(StringComparer.Ordinal);
            var asm = typeof(I18n).Assembly;
            var suffix = ".Resources.i18n." + lang + ".json";
            var resourceName = asm.GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (resourceName != null)
            {
                using var stream = asm.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    try
                    {
                        using var reader = new StreamReader(stream);
                        var json = reader.ReadToEnd();
                        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                        if (parsed != null)
                        {
                            foreach (var kv in parsed)
                                dict[kv.Key] = kv.Value ?? "";
                        }
                    }
                    catch { /* fallback to [key] markers */ }
                }
            }
            _strings = dict;
        }
    }
}