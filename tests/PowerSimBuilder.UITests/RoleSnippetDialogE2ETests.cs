using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests;

public class RoleSnippetDialogE2ETests
{
    private static FlaUI.Core.Application LaunchWithLog(string args, string logPath, string session)
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestPaths.AppExePath,
            Arguments = args,
            UseShellExecute = false,
            WorkingDirectory = TestPaths.ProjectRoot,
        };
        psi.EnvironmentVariables["PSB_EDIT_LOG"] = logPath;
        psi.EnvironmentVariables["PSB_EDIT_SESSION"] = session;
        return FlaUI.Core.Application.Launch(psi);
    }

    private static readonly System.Collections.Generic.List<string> _seenWindows = new();

    private static Window WaitForWindow(FlaUI.Core.Application app, UIA3Automation automation, int processId, string titleContains, int seconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        lock (_seenWindows) { _seenWindows.Clear(); }
        while (DateTime.UtcNow < deadline)
        {
            // Method 1: app.GetAllTopLevelWindows (standard FlaUI API)
            try
            {
                foreach (var w in app.GetAllTopLevelWindows(automation))
                {
                    try
                    {
                        var name = w.Name ?? "";
                        lock (_seenWindows) { if (!_seenWindows.Contains(name)) _seenWindows.Add(name); }
                        if (name.Contains(titleContains)) return w;
                        // Fallback: any window that contains a TextBoxCode / textEditor child
                        try
                        {
                            var hasEditor = w.FindFirstDescendant(cf => cf.ByAutomationId("textEditor")) != null
                                         || w.FindFirstDescendant(cf => cf.ByAutomationId("TextBoxCode")) != null;
                            if (hasEditor) return w;
                        }
                        catch { }
                    }
                    catch { }
                }
            }
            catch { }

            // Method 2: Desktop children fallback
            try
            {
                foreach (var w in automation.GetDesktop().FindAllChildren())
                {
                    try
                    {
                        var name = w.Name ?? "";
                        lock (_seenWindows) { if (!_seenWindows.Contains(name)) _seenWindows.Add(name); }
                        if (w.Properties.ProcessId.ValueOrDefault == processId
                            && name.Contains(titleContains))
                            return w.AsWindow();
                    }
                    catch { }
                }
            }
            catch { }

            Thread.Sleep(300);
        }
        // Diagnostic dump on timeout
        var diagPath = Path.Combine(Path.GetTempPath(), "psb_e2e_window_dump.txt");
        try
        {
            lock (_seenWindows)
            {
                File.WriteAllLines(diagPath, new[]
                {
                    $"WaitForWindow('{titleContains}') timeout after {seconds}s",
                    $"processId={processId}",
                    $"Seen {_seenWindows.Count} window(s):"
                }.Concat(_seenWindows.Select(s => "  - " + s)));
            }
        }
        catch { }
        return null;
    }

    private static AutomationElement FindByAutomationId(Window root, string automationId, int seconds = 10)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var el = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (el != null) return el;
                el = root.FindFirstDescendant(cf => cf.ByName(automationId));
                if (el != null) return el;
            }
            catch { }
            Thread.Sleep(200);
        }
        return null;
    }

    private static void OpenRoleSnippetsDialog_Unused(Window editor, UIA3Automation automation, int processId)
    {
        var textEditor = editor.FindFirstDescendant(cf => cf.ByAutomationId("textEditor"))
                         ?? editor.FindFirstDescendant(cf => cf.ByControlType(ControlType.Document))
                         ?? editor.FindFirstDescendant(cf => cf.ByControlType(ControlType.Edit));
        Assert.NotNull(textEditor);
        textEditor.RightClick();
        Thread.Sleep(500);

        var menuItem = automation.GetDesktop().FindFirstDescendant(cf => cf.ByAutomationId("menuItemRoleSnippets"))
                       ?? editor.FindFirstDescendant(cf => cf.ByAutomationId("menuItemRoleSnippets"));
        Assert.NotNull(menuItem);
        menuItem.Click();
    }

    [Fact]
    public void RoleSnippet_OwnMode_InsertsToSameFunction()
    {
        var logPath = EditLogReader.NewLogPath("own-e2e");
        var session = "own-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var fixture = Path.Combine(TestPaths.ProjectRoot, "examples", "demos", "oven_control_statable.psbproj");
        Assert.True(File.Exists(fixture), "fixture missing: " + fixture);

        var args = "--test-project \"" + fixture + "\" --test-open-editor \"Controller Box\" --test-open-role-snippets";
        using var app = LaunchWithLog(args, logPath, session);
        try
        {
            using var automation = new UIA3Automation();
            EditLogReader.WaitForEvent(logPath, "OpenEditor", TimeSpan.FromSeconds(25));
            Thread.Sleep(1500);

            var editor = WaitForWindow(app, automation, app.ProcessId, "Python Code Editor", 20);
            Assert.NotNull(editor);

            // CLI flag --test-open-role-snippets handles opening the dialog
            var dlg = WaitForWindow(app, automation, app.ProcessId, "Role Snippets", 15);
            Assert.NotNull(dlg);

            var rbOwn = FindByAutomationId(dlg, "rbSnippetOwn");
            Assert.NotNull(rbOwn);
            rbOwn.Click();
            Thread.Sleep(300);

            var btnInsert = FindByAutomationId(dlg, "btnSnippetInsert");
            Assert.NotNull(btnInsert);
            Assert.True(btnInsert.IsEnabled, "Insert should be enabled after selecting (snippet's own)");
            btnInsert.Click();

            var confirm = WaitForWindow(app, automation, app.ProcessId, "Role Snippets", 10);
            if (confirm != null)
            {
                var yes = confirm.FindFirstDescendant(cf => cf.ByName("Yes"))
                          ?? confirm.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button));
                if (yes != null) yes.Click();
            }

            EditLogReader.WaitForEvent(logPath, "SnippetInsertPlan", TimeSpan.FromSeconds(15));
            var plan = EditLogReader.FirstEvent(logPath, "SnippetInsertPlan");
            Assert.NotNull(plan);
            Assert.Equal("OWN", EditLogReader.GetString(plan!.Value, "mode"));
        }
        finally
        {
            try { app.Kill(); } catch { }
            try { File.Delete(logPath); } catch { }
        }
    }

    [Fact]
    public void RoleSnippet_CrossMode_InsertsToTargetFunction()
    {
        var logPath = EditLogReader.NewLogPath("cross-e2e");
        var session = "cross-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var fixture = Path.Combine(TestPaths.ProjectRoot, "examples", "demos", "oven_control_statable.psbproj");
        Assert.True(File.Exists(fixture), "fixture missing: " + fixture);

        var args = "--test-project \"" + fixture + "\" --test-open-editor \"Controller Box\" --test-open-role-snippets";
        using var app = LaunchWithLog(args, logPath, session);
        try
        {
            using var automation = new UIA3Automation();
            EditLogReader.WaitForEvent(logPath, "OpenEditor", TimeSpan.FromSeconds(25));
            Thread.Sleep(1500);

            var editor = WaitForWindow(app, automation, app.ProcessId, "Python Code Editor", 20);
            Assert.NotNull(editor);

            // CLI flag --test-open-role-snippets handles opening the dialog
            var dlg = WaitForWindow(app, automation, app.ProcessId, "Role Snippets", 15);
            Assert.NotNull(dlg);

            var rbTarget = FindByAutomationId(dlg, "rbTargetFunction");
            Assert.NotNull(rbTarget);
            rbTarget.Click();
            Thread.Sleep(300);

            var tree = FindByAutomationId(dlg, "treeTarget");
            Assert.NotNull(tree);

            var nodes = tree.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem));
            foreach (var n in nodes)
            {
                if ((n.Name ?? "") == "[MwOven]")
                {
                    try { n.AsTreeItem().Expand(); } catch { }
                    Thread.Sleep(400);
                    break;
                }
            }
            nodes = tree.FindAllDescendants(cf => cf.ByControlType(ControlType.TreeItem));
            AutomationElement target = null;
            foreach (var n in nodes)
            {
                if ((n.Name ?? "").Contains("F_SetTopHeaterDuty")) { target = n; break; }
            }
            Assert.NotNull(target);
            target.Click();
            Thread.Sleep(300);

            var btnInsert = FindByAutomationId(dlg, "btnSnippetInsert");
            Assert.NotNull(btnInsert);
            Assert.True(btnInsert.IsEnabled, "Insert should be enabled after selecting target function");
            btnInsert.Click();

            var confirm = WaitForWindow(app, automation, app.ProcessId, "Role Snippets", 10);
            if (confirm != null)
            {
                var yes = confirm.FindFirstDescendant(cf => cf.ByName("Yes"))
                          ?? confirm.FindFirstDescendant(cf => cf.ByControlType(ControlType.Button));
                if (yes != null) yes.Click();
            }

            EditLogReader.WaitForEvent(logPath, "SnippetInsertPlan", TimeSpan.FromSeconds(15));
            var plan = EditLogReader.FirstEvent(logPath, "SnippetInsertPlan");
            Assert.NotNull(plan);
            Assert.Equal("CROSS", EditLogReader.GetString(plan!.Value, "mode"));
        }
        finally
        {
            try { app.Kill(); } catch { }
            try { File.Delete(logPath); } catch { }
        }
    }
}
