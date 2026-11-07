using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests;

public class RoleSnippetRegisterE2ETests
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

    // --- Trap #15 mitigation: ShowDialog + ShowInTaskbar=false dialogs are not
    // visible to UIA3's desktop tree; use Win32 EnumWindows to locate them. ---
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private static Window WaitForRegisterDialog(FlaUI.Core.Application app, UIA3Automation automation, int processId, int seconds = 15)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            // Method 1: app.GetAllTopLevelWindows (standard FlaUI API)
            try
            {
                foreach (var w in app.GetAllTopLevelWindows(automation))
                {
                    try
                    {
                        if (!(w.Name ?? "").Contains("Register Role Snippet")) continue;
                        var txtLayer = w.FindFirstDescendant(cf => cf.ByAutomationId("TxtLayer"));
                        if (txtLayer != null) return w;
                    }
                    catch { }
                }
            }
            catch { }

            // Method 2: Desktop children
            try
            {
                foreach (var w in automation.GetDesktop().FindAllChildren())
                {
                    try
                    {
                        if (w.Properties.ProcessId.ValueOrDefault != processId) continue;
                        if (!(w.Name ?? "").Contains("Register Role Snippet")) continue;
                        var txtLayer = w.FindFirstDescendant(cf => cf.ByAutomationId("TxtLayer"));
                        if (txtLayer != null) return w.AsWindow();
                    }
                    catch { }
                }
            }
            catch { }

            // Method 3: Win32 EnumWindows (Trap #15 - ShowInTaskbar=false modals)
            IntPtr found = IntPtr.Zero;
            try
            {
                EnumWindows((hwnd, lParam) =>
                {
                    uint pid;
                    GetWindowThreadProcessId(hwnd, out pid);
                    if (pid != (uint)processId) return true;
                    if (!IsWindowVisible(hwnd)) return true;
                    var sb = new StringBuilder(512);
                    GetWindowText(hwnd, sb, 512);
                    var title = sb.ToString();
                    if (string.IsNullOrEmpty(title)) return true;
                    if (!title.Contains("Register Role Snippet")) return true;
                    found = hwnd;
                    return false;
                }, IntPtr.Zero);
            }
            catch { }

            if (found != IntPtr.Zero)
            {
                try
                {
                    var el = automation.FromHandle(found);
                    var win = el?.AsWindow();
                    if (win != null)
                    {
                        var txtLayer = win.FindFirstDescendant(cf => cf.ByAutomationId("TxtLayer"));
                        if (txtLayer != null) return win;
                    }
                }
                catch { }
            }

            Thread.Sleep(300);
        }
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

    [Fact]
    public void RegisterSnippet_NewFunction_CreatesFileAndLogsSuccess()
    {
        var funcName = "F_LogOvFault";
        var layerName = "MwOven";
        var snippetFile = Path.Combine(TestPaths.ProjectRoot, "templates", "role_snippets", layerName + "." + funcName + ".py");
        var indexPath = Path.Combine(TestPaths.ProjectRoot, "templates", "role_snippets", "index.json");

        Assert.False(File.Exists(snippetFile),
            "Pre-existing snippet file must be removed before the test: " + snippetFile);

        var indexBackup = File.Exists(indexPath) ? File.ReadAllBytes(indexPath) : null;

        var logPath = EditLogReader.NewLogPath("register-e2e");
        var session = "reg-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        var fixture = Path.Combine(TestPaths.ProjectRoot, "examples", "demos", "oven_control_statable.psbproj");
        Assert.True(File.Exists(fixture), "fixture missing: " + fixture);

        var args = "--test-project \"" + fixture + "\" --test-open-editor \"Controller Box\" --test-open-register-snippet " + funcName;
        using var app = LaunchWithLog(args, logPath, session);
        try
        {
            using var automation = new UIA3Automation();
            EditLogReader.WaitForEvent(logPath, "OpenEditor", TimeSpan.FromSeconds(25));
            Thread.Sleep(1500);

            var dlg = WaitForRegisterDialog(app, automation, app.ProcessId, 15);
            Assert.NotNull(dlg);

            var txtLayer = FindByAutomationId(dlg!, "TxtLayer");
            var txtFunc = FindByAutomationId(dlg!, "TxtFunc");
            Assert.NotNull(txtLayer);
            Assert.NotNull(txtFunc);
            // WinForms TextBox.Name in UIA returns the AutomationId ("TxtLayer"),
            // not the text content; use ValuePattern (AsTextBox().Text) instead.
            var layerText = txtLayer!.AsTextBox().Text ?? "";
            var funcText = txtFunc!.AsTextBox().Text ?? "";
            Assert.Contains(layerName, layerText);
            Assert.Contains(funcName, funcText);

            var txtDesc = FindByAutomationId(dlg!, "TxtDescription");
            Assert.NotNull(txtDesc);
            txtDesc!.AsTextBox().Text = "E2E automated description";
            Thread.Sleep(200);

            var btnOK = FindByAutomationId(dlg!, "BtnRegisterSnippetOK");
            Assert.NotNull(btnOK);
            btnOK!.Click();

            EditLogReader.WaitForEvent(logPath, "RegisterSnippetSuccess", TimeSpan.FromSeconds(15));
            var evt = EditLogReader.FirstEvent(logPath, "RegisterSnippetSuccess");
            Assert.NotNull(evt);
            Assert.Equal(funcName, EditLogReader.GetString(evt!.Value, "func"));
            Assert.Equal(layerName, EditLogReader.GetString(evt.Value, "layer"));

            Assert.True(File.Exists(snippetFile), "snippet file not created: " + snippetFile);
            var content = File.ReadAllText(snippetFile);
            Assert.DoesNotContain("STABLE_USER_CODE", content);
        }
        finally
        {
            try { if (indexBackup != null) File.WriteAllBytes(indexPath, indexBackup); } catch { }
            try { if (File.Exists(snippetFile)) File.Delete(snippetFile); } catch { }
            try { app.Kill(); } catch { }
            try { File.Delete(logPath); } catch { }
        }
    }
}
