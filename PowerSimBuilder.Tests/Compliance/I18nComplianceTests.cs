using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace PowerSimBuilder.Tests.Compliance;

/// <summary>
/// i18n compliance regression tests.
///
/// T1: en.json and zh.json have identical key sets
/// T2: Every I18n.T("...") reference has a key in both JSONs
/// T3: Every Form subclasses sets its title via I18n.T()
///
/// Adding a new Form without I18n.T() will fail T3.
/// Adding a new I18n.T() key to only one JSON will fail T1 or T2.
/// </summary>
public class I18nComplianceTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    /// <summary>
    /// v1.3.0 baseline: this many legacy Forms still have hardcoded titles.
    /// New Forms added without I18n.T() will exceed this budget and fail T3.
    /// As legacy forms are i18n-ified, LOWER this number (never raise it).
    /// </summary>
    private const int LegacyFormTitleViolationBudget = 20;

    // ========================================================================
    // T1: en.json and zh.json must have identical key sets
    // ========================================================================
    [Fact]
    public void T1_EnAndZh_HaveSameKeySet()
    {
        var en = LoadJson(Path.Combine(RepoRoot, "Resources", "i18n", "en.json"));
        var zh = LoadJson(Path.Combine(RepoRoot, "Resources", "i18n", "zh.json"));

        var onlyEn = en.Keys.Except(zh.Keys, StringComparer.Ordinal).OrderBy(k => k).ToList();
        var onlyZh = zh.Keys.Except(en.Keys, StringComparer.Ordinal).OrderBy(k => k).ToList();

        var issues = new List<string>();
        if (onlyEn.Count > 0)
            issues.Add($"Only in en.json ({onlyEn.Count}): {string.Join(", ", onlyEn)}");
        if (onlyZh.Count > 0)
            issues.Add($"Only in zh.json ({onlyZh.Count}): {string.Join(", ", onlyZh)}");

        Assert.True(issues.Count == 0,
            "en.json and zh.json have different keys:\n" + string.Join("\n", issues));
    }

    // ========================================================================
    // T2: Every I18n.T("...") reference must exist in both JSONs
    // ========================================================================
    [Fact]
    public void T2_AllI18nKeyReferences_ExistInBothJson()
    {
        var en = LoadJson(Path.Combine(RepoRoot, "Resources", "i18n", "en.json"));
        var zh = LoadJson(Path.Combine(RepoRoot, "Resources", "i18n", "zh.json"));

        var used = ScanI18nKeyReferences();
        Assert.True(used.Count > 50,
            $"Only {used.Count} I18n.T() references found. Is RepoRoot correct? ({RepoRoot})");

        var missingEn = used.Where(k => !en.ContainsKey(k)).OrderBy(k => k).ToList();
        var missingZh = used.Where(k => !zh.ContainsKey(k)).OrderBy(k => k).ToList();

        var issues = new List<string>();
        if (missingEn.Count > 0)
            issues.Add($"Referenced but missing in en.json ({missingEn.Count}): {string.Join(", ", missingEn)}");
        if (missingZh.Count > 0)
            issues.Add($"Referenced but missing in zh.json ({missingZh.Count}): {string.Join(", ", missingZh)}");

        Assert.True(issues.Count == 0,
            "I18n.T() references without corresponding JSON keys:\n" + string.Join("\n", issues));
    }

    // ========================================================================
    // T3: Every Form must set its title via I18n.T()
    // ========================================================================
    [Fact]
    public void T3_AllForms_UseI18nForTitle()
    {
        var violations = FindFormTitleViolations();

        Assert.True(violations.Count <= LegacyFormTitleViolationBudget,
            $"{violations.Count} form(s) need I18n.T() title " +
            $"(budget: {LegacyFormTitleViolationBudget}).\n\n" +
            "If NEW forms appear in the list: add I18n.T(\"...\") for Text.\n" +
            "If you i18n-ified an existing form: LOWER LegacyFormTitleViolationBudget.\n\n" +
            "Violations:\n" + string.Join("\n", violations));
    }

    // ========================================================================
    // Helpers
    // ========================================================================

    private static Dictionary<string, string> LoadJson(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException($"i18n file not found: {path}");
        var json = File.ReadAllText(path, Encoding.UTF8);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
            ?? throw new InvalidDataException($"Failed to parse {path}");
    }

    private static HashSet<string> ScanI18nKeyReferences()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var pattern = new Regex(@"I18n\.T\(\s*""([^""]+)""", RegexOptions.Compiled);

        foreach (var file in Directory.GetFiles(RepoRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (IsExcludedPath(file)) continue;
            foreach (Match m in pattern.Matches(File.ReadAllText(file)))
            {
                var key = m.Groups[1].Value;
                // Filter out comment-like references ("...", "...Title...", etc.)
                if (key.Length < 3) continue;
                if (!Regex.IsMatch(key, @"[A-Za-z]")) continue;
                keys.Add(key);
            }
        }
        return keys;
    }

    private static List<string> FindFormTitleViolations()
    {
        var violations = new List<string>();
        var files = Directory.GetFiles(RepoRoot, "*.cs", SearchOption.AllDirectories)
            .Where(f => !IsExcludedPath(f))
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(LooksLikeFormSource);

        foreach (var file in files)
        {
            if (FormHasI18nTitle(file)) continue;
            violations.Add($"  - {Path.GetFileName(file)}");
        }
        return violations;
    }

    private static bool FormHasI18nTitle(string csPath)
    {
        var designerPath = csPath.Substring(0, csPath.Length - 3) + ".Designer.cs";
        foreach (var file in new[] { csPath, designerPath })
        {
            if (!File.Exists(file)) continue;
            var content = File.ReadAllText(file);
            // Direct assignment
            if (Regex.IsMatch(content, @"Text\s*=\s*I18n\.T\(")) return true;
            // Template assignment (Text = string.Format(I18n.T(...)))
            if (Regex.IsMatch(content, @"Text\s*=\s*string\.Format\s*\(\s*I18n\.T\(")) return true;
            // Title-suffix key convention (I18n.T("MainForm.Title") or I18n.T("X.TitleTemplate"))
            if (Regex.IsMatch(content, @"I18n\.T\(\s*""[^""]*Title[^""]*""")) return true;
        }
        return false;
    }

    private static bool LooksLikeFormSource(string file)
    {
        try
        {
            using var reader = new StreamReader(file, Encoding.UTF8);
            var buf = new char[Math.Min(4000, 65536)];
            int n = reader.Read(buf, 0, buf.Length);
            var head = new string(buf, 0, n);
            if (head.Contains(" : Form") || head.Contains(": Form") ||
                head.Contains("System.Windows.Forms.Form") ||
                head.Contains(" : UserControl") || head.Contains(": UserControl"))
                return true;
            return false;
        }
        catch { return false; }
    }

    private static bool IsExcludedPath(string path)
    {
        return path.Contains(@"\bin\") ||
               path.Contains(@"\obj\") ||
               path.Contains(@"\.git\") ||
               path.Contains(@"\.vs\") ||
               path.Contains(@"\.mypy_cache\") ||
               path.Contains(@"\.pytest_cache\") ||
               path.Contains(@"\.ruff_cache\") ||
               path.Contains(@"\node_modules\") ||
               path.Contains(@"\previou_files\") ||
               path.Contains(@"\tmp\") ||
               path.Contains(@"\PowerSimBuilder.Tests\");
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PowerSimBuilder.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException(
            "Could not find repo root (PowerSimBuilder.sln) from " + AppContext.BaseDirectory);
    }
}
