using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests;

public class PsblogImportE2ETests
{
    private static FlaUI.Core.Application LaunchWithPsblogImport(string psblogPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestPaths.AppExePath,
            Arguments = "",
            UseShellExecute = false,
            WorkingDirectory = TestPaths.ProjectRoot,
        };
        psi.EnvironmentVariables["PSB_PSBLOG_IMPORT"] = psblogPath;
        psi.EnvironmentVariables["PSB_E2E_TEST"] = "1";
        return FlaUI.Core.Application.Launch(psi);
    }

    // Trap #15 / #86 fallback: ShowInTaskbar=false modals are not in UIA3 desktop tree.
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    private static Window? WaitForImportDialog(FlaUI.Core.Application app, UIA3Automation automation, int processId, int seconds = 20)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                foreach (var w in app.GetAllTopLevelWindows(automation))
                {
                    try
                    {
                        var name = w.Name ?? "";
                        if (name.Contains("Import Signal Log")) return w;
                    }
                    catch { }
                }
            }
            catch { }

            IntPtr found = IntPtr.Zero;
            EnumWindows((hwnd, _) =>
            {
                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid != (uint)processId) return true;
                if (!IsWindowVisible(hwnd)) return true;
                var sb = new StringBuilder(512);
                GetWindowText(hwnd, sb, 512);
                var title = sb.ToString();
                if (!string.IsNullOrEmpty(title) && title.Contains("Import Signal Log"))
                {
                    found = hwnd;
                    return false;
                }
                return true;
            }, IntPtr.Zero);
            if (found != IntPtr.Zero)
            {
                var el = automation.FromHandle(found);
                if (el != null) return el.AsWindow();
            }
            Thread.Sleep(300);
        }
        return null;
    }

    private static string SamplePath(string name)
        => Path.Combine(TestPaths.ProjectRoot, "examples", "logs", name);

    [Fact]
    public void PsblogImport_OpensWithPreloadedFile()
    {
        using var app = LaunchWithPsblogImport(SamplePath("sample_pid_tuning.psblog"));
        using var automation = new UIA3Automation();
        var dialog = WaitForImportDialog(app, automation, app.ProcessId);
        Assert.NotNull(dialog);
        try { dialog!.Close(); } catch { }
    }

    [Fact]
    public void PsblogImport_ShowsLoadedVariables()
    {
        using var app = LaunchWithPsblogImport(SamplePath("sample_pid_tuning.psblog"));
        using var automation = new UIA3Automation();
        var dialog = WaitForImportDialog(app, automation, app.ProcessId);
        Assert.NotNull(dialog);

        // Find "Add to Canvas" button (Name -> AutomationId in UIA for WinForms)
        AutomationElement? addBtn = null;
        for (int i = 0; i < 20 && addBtn == null; i++)
        {
            try { addBtn = dialog!.FindFirstDescendant(cf => cf.ByAutomationId("BtnAddToCanvas")); } catch { }
            if (addBtn == null)
            {
                try { addBtn = dialog!.FindFirstDescendant(cf => cf.ByName("Add to Canvas")); } catch { }
            }
            if (addBtn == null) Thread.Sleep(200);
        }
        Assert.NotNull(addBtn);
        Assert.True(addBtn!.IsEnabled, "Add to Canvas button should be enabled for pre-loaded file");

        try { dialog!.Close(); } catch { }
    }
}
