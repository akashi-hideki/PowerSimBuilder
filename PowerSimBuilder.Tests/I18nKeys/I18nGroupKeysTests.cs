using System;
using System.IO;
using System.Text.Json;
using Xunit;

namespace PowerSimBuilder.Tests.I18nKeys
{
    /// <summary>
    /// M5: multi-language keys for Save/Load Group exist in en.json and zh.json.
    /// </summary>
    public class I18nGroupKeysTests
    {
        private static string FindRepoRoot()
        {
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            while (!string.IsNullOrEmpty(dir))
            {
                if (File.Exists(Path.Combine(dir, "PowerSimBuilder.sln"))) return dir;
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            throw new InvalidOperationException("repo root not found");
        }

        private static string ReadKey(string lang, string key)
        {
            var root = FindRepoRoot();
            var path = Path.Combine(root, "Resources", "i18n", lang + ".json");
            Assert.True(File.Exists(path), "i18n file missing: " + path);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(doc.RootElement.TryGetProperty(key, out var val),
                $"key missing: {key} in {lang}.json");
            return val.GetString() ?? "";
        }

        [Theory]
        [InlineData("en", "MainForm.BtnSaveGroup", "Save Group...")]
        [InlineData("en", "MainForm.BtnLoadGroup", "Load Group...")]
        [InlineData("zh", "MainForm.BtnSaveGroup", "保存组...")]
        [InlineData("zh", "MainForm.BtnLoadGroup", "加载组...")]
        [InlineData("en", "MainForm.SaveGroupDialogTitle", "Save Selection as Group")]
        [InlineData("zh", "MainForm.SaveGroupDialogTitle", "保存选定组件为组")]
        public void Key_HasExpectedValue(string lang, string key, string expected)
        {
            Assert.Equal(expected, ReadKey(lang, key));
        }

        [Theory]
        [InlineData("en")]
        [InlineData("zh")]
        public void AllGroupKeys_PresentInLanguage(string lang)
        {
            string[] keys =
            {
                "MainForm.BtnSaveGroup",
                "MainForm.BtnLoadGroup",
                "MainForm.SaveGroupDialogTitle",
                "MainForm.LoadGroupDialogTitle",
                "MainForm.MsgSaveGroupNoSelection",
                "MainForm.MsgSaveGroupOk",
                "MainForm.MsgLoadGroupOk",
            };
            foreach (var k in keys)
            {
                var v = ReadKey(lang, k);
                Assert.False(string.IsNullOrWhiteSpace(v), $"{k} empty in {lang}.json");
            }
        }
    }
}
