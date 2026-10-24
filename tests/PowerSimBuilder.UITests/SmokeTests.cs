using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests;

[Trait("Category", "UI")]
public sealed class SmokeTests
{
    private static string TempPath(string name) => Path.Combine(Path.GetTempPath(), name);
    private static void Log(string msg)
    {
        try { File.AppendAllText(TempPath("psb_ui_test.log"),
            DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + Environment.NewLine); } catch { }
    }

    private static Window WaitForEditor(UIA3Automation automation, int seconds, int processId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        bool dumpedOnce = false;

        while (DateTime.UtcNow < deadline)
        {
            // Strategy 1: AutomationId
            var editor = automation.GetDesktop().FindFirstChild(cf => cf.ByAutomationId("PythonEditorForm"))?.AsWindow();
            if (editor != null) { Log("editor via AutomationId"); return editor; }

            // Strategy 2: Name exact
            editor = automation.GetDesktop().FindFirstChild(cf => cf.ByName("Python Code Editor"))?.AsWindow();
            if (editor != null) { Log("editor via Name exact"); return editor; }

            // Strategy 3: Win32 EnumWindows for modal dialogs (UIA3 skips ShowInTaskbar=false)
            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, lParam) =>
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != (uint)processId) return true;
                if (!IsWindowVisible(hwnd)) return true;
                var sb = new StringBuilder(512);
                GetWindowText(hwnd, sb, 512);
                var title = sb.ToString();
                if (!dumpedOnce) { Log("  WIN32: hwnd=" + hwnd.ToInt64() + " title='" + title + "'"); }
                if (!string.IsNullOrEmpty(title) && title.Contains("Python"))
                {
                    found = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            dumpedOnce = true;

            if (found != IntPtr.Zero)
            {
                Log("FOUND editor by Win32: hwnd=" + found.ToInt64());
                try
                {
                    var el = automation.FromHandle(found);
                    if (el != null) return el.AsWindow();
                }
                catch (Exception ex) { Log("FromHandle err: " + ex.Message); }
            }

            System.Threading.Thread.Sleep(500);
        }

        Log("=== WaitForEditor TIMEOUT ===");
        return null;
    }
    private static Window WaitForWindowByTitleContaining(UIA3Automation automation, int seconds, int processId, string titleContains)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, lParam) =>
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != (uint)processId) return true;
                if (!IsWindowVisible(hwnd)) return true;
                var sb = new StringBuilder(512);
                GetWindowText(hwnd, sb, 512);
                var title = sb.ToString();
                if (!string.IsNullOrEmpty(title) && title.Contains(titleContains))
                {
                    found = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);

            if (found != IntPtr.Zero)
            {
                Log("FOUND by Win32 title='" + titleContains + "' hwnd=" + found.ToInt64());
                try
                {
                    var el = automation.FromHandle(found);
                    if (el != null) return el.AsWindow();
                }
                catch (Exception ex) { Log("FromHandle err: " + ex.Message); }
            }
            System.Threading.Thread.Sleep(500);
        }
        Log("WaitForWindowByTitleContaining TIMEOUT: '" + titleContains + "'");
        return null;
    }

    // === Win32 interop ===
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [Fact]
    public void LaunchApp_ShowsMainWindow()
    {
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath);
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
        Assert.NotNull(window);
        Assert.Equal("MainForm", window.Properties.AutomationId.ValueOrDefault);
        Assert.False(string.IsNullOrWhiteSpace(window.Title));
    }

    [Fact]
    public void DoubleClick_OpensEditor()
    {
        Log("DoubleClick START");
        var fixture = TestPaths.TestFixturePath("with-component.json");
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath,
            "--test-project \"" + fixture + "\" --test-open-editor firstOrderLag1");
        try
        {
            using var automation = new UIA3Automation();
            var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
            Assert.NotNull(mainWindow);
            Log("mainWindow acquired");

            var editor = WaitForEditor(automation, 15, app.ProcessId);
            Log("editor found: " + (editor != null));
            Assert.NotNull(editor);
            Log("DoubleClick PASS");
        }
        finally { try { app.Kill(); } catch { } }
    }

    [Fact]
    public void AddPhysics_InsertsCode()
    {
        Log("AddPhysics START");
        var fixture = TestPaths.TestFixturePath("with-component.json");
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath,
            "--test-project \"" + fixture + "\" --test-open-editor firstOrderLag1");
        try
        {
            using var automation = new UIA3Automation();
            var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
            Assert.NotNull(mainWindow);

            var editor = WaitForEditor(automation, 15, app.ProcessId);
            Assert.NotNull(editor);
            Log("editor found for AddPhysics");

            var btnPhysics = Retry.WhileNull(
                () => editor.FindFirstDescendant(cf => cf.ByAutomationId("btnPhysics"))
                    ?? editor.FindFirstDescendant(cf => cf.ByName("btnPhysics")),
                TimeSpan.FromSeconds(5)).Result;
            Assert.NotNull(btnPhysics);
            btnPhysics.Click();
            Log("btnPhysics clicked");

            var picker = WaitForWindowByTitleContaining(automation, 10, app.ProcessId, "Add Physics Model");
            Assert.NotNull(picker);
            Log("picker opened");

            var listView = picker.FindFirstDescendant(cf => cf.ByAutomationId("ListViewModels"))
                ?? picker.FindFirstDescendant(cf => cf.ByName("ListViewModels"));
            Assert.NotNull(listView);

            AutomationElement firstItem = null;
            var itemDeadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < itemDeadline && firstItem == null)
            {
                firstItem = listView.FindFirstDescendant(cf => cf.ByControlType(ControlType.ListItem));
                if (firstItem == null) System.Threading.Thread.Sleep(500);
            }
            Assert.NotNull(firstItem);
            firstItem.Click();
            Log("first item selected");

            var btnInsert = picker.FindFirstDescendant(cf => cf.ByAutomationId("BtnInsert"))
                ?? picker.FindFirstDescendant(cf => cf.ByName("Insert"));
            Assert.NotNull(btnInsert);
            btnInsert.Click();
            Log("insert clicked");

            var textBox = Retry.WhileNull(
                () => editor.FindFirstDescendant(cf => cf.ByAutomationId("TextBoxCode"))
                    ?? editor.FindFirstDescendant(cf => cf.ByName("TextBoxCode")),
                TimeSpan.FromSeconds(5)).Result;
            Assert.NotNull(textBox);
            var text = textBox.AsTextBox().Text;
            Assert.Contains("# === physics:", text);
            Assert.Contains("(auto-inserted)", text);
            Log("AddPhysics PASS");
        }
        finally { try { app.Kill(); } catch { } }
    }
    [Fact]
    public void Picker_ShowsImportModelButton()
    {
        Log("PickerImportButton START");
        var fixture = TestPaths.TestFixturePath("with-component.json");
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath,
            "--test-project \"" + fixture + "\" --test-open-editor firstOrderLag1");
        try
        {
            using var automation = new UIA3Automation();
            var editor = WaitForEditor(automation, 20, app.ProcessId);
            Assert.NotNull(editor);
            Log("editor opened");

            var btnPhysics = Retry.WhileNull(
                () => editor.FindFirstDescendant(cf => cf.ByAutomationId("btnPhysics"))
                    ?? editor.FindFirstDescendant(cf => cf.ByName("btnPhysics")),
                TimeSpan.FromSeconds(5)).Result;
            Assert.NotNull(btnPhysics);
            btnPhysics.Click();
            Log("btnPhysics clicked");

            var picker = WaitForWindowByTitleContaining(automation, 10, app.ProcessId, "Physics Model");
            if (picker == null)
                picker = WaitForWindowByTitleContaining(automation, 5, app.ProcessId, "\u7269\u7406\u6a21\u578b");
            Assert.NotNull(picker);
            Log("picker opened");

            var btnImport = Retry.WhileNull(
                () => picker.FindFirstDescendant(cf => cf.ByAutomationId("BtnImportModel")),
                TimeSpan.FromSeconds(5)).Result;
            Assert.NotNull(btnImport);
            Log("BtnImportModel found");
        }
        finally { try { app.Kill(); } catch { } }
    }

    [Fact]
    public void ImportButton_OpensPhysicsImportForm()
    {
        Log("ImportButton Opens Form START");
        var fixture = TestPaths.TestFixturePath("with-component.json");
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath,
            "--test-project \"" + fixture + "\" --test-open-editor firstOrderLag1");
        try
        {
            using var automation = new UIA3Automation();
            var editor = WaitForEditor(automation, 20, app.ProcessId);
            Assert.NotNull(editor);

            var btnPhysics = Retry.WhileNull(
                () => editor.FindFirstDescendant(cf => cf.ByAutomationId("btnPhysics"))
                    ?? editor.FindFirstDescendant(cf => cf.ByName("btnPhysics")),
                TimeSpan.FromSeconds(5)).Result;
            Assert.NotNull(btnPhysics);
            btnPhysics.Click();

            var picker = WaitForWindowByTitleContaining(automation, 10, app.ProcessId, "Physics Model");
            if (picker == null)
                picker = WaitForWindowByTitleContaining(automation, 5, app.ProcessId, "\u7269\u7406\u6a21\u578b");
            Assert.NotNull(picker);

            var btnImport = picker.FindFirstDescendant(cf => cf.ByAutomationId("BtnImportModel"));
            Assert.NotNull(btnImport);
            btnImport.Click();
            Log("BtnImportModel clicked");

            var importForm = WaitForWindowByTitleContaining(automation, 10, app.ProcessId, "Import Physics Model");
            if (importForm == null)
                importForm = WaitForWindowByTitleContaining(automation, 5, app.ProcessId, "\u5bfc\u5165\u7269\u7406\u6a21\u578b");
            Assert.NotNull(importForm);
            Log("PhysicsImportForm opened");
        }
        finally { try { app.Kill(); } catch { } }
    }
}
