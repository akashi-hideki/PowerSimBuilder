using System;
using System.IO;
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
    private static void DumpTree(FlaUI.Core.AutomationElements.AutomationElement root, string tag)
    {
        try
        {
            using (var w = new StreamWriter(TempPath("psb_uitree_" + tag + ".txt"), false,
                new System.Text.UTF8Encoding(false)))
            {
                w.WriteLine("=== UI TREE: " + tag + " ===");
                int count = 0;
                foreach (var el in root.FindAllDescendants())
                {
                    count++;
                    try
                    {
                        w.WriteLine($"  [{count}] Type={el.Properties.ControlType.ValueOrDefault} AutomationId='{el.Properties.AutomationId.ValueOrDefault}' Name='{el.Properties.Name.ValueOrDefault}' Class='{el.Properties.ClassName.ValueOrDefault}'");
                    } catch (Exception ex) { w.WriteLine($"  [{count}] err: {ex.Message}"); }
                }
                w.WriteLine("=== total " + count + " elements ===");
            }
        } catch (Exception ex) { Log("DumpTree err: " + ex.Message); }
    }

    [Fact]
    public void LaunchApp_ShowsMainWindow()
    {
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath);
        using var automation = new UIA3Automation();
        var window = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
        Assert.NotNull(window);
        Assert.Equal("MainForm", window.Properties.AutomationId.ValueOrDefault);
    }

    [Fact(Skip = "UIA3 Mouse.DoubleClick does not trigger WM_LBUTTONDBLCLK on WinForms UserControl (drag handler interferes). Covered by L2 tests PythonEditorFormPhysicsTests.")]
    public void DoubleClick_OpensEditor()
    {
        Log("DoubleClick START");
        var fixture = TestPaths.TestFixturePath("with-component.json");
        using var app = FlaUI.Core.Application.Launch(TestPaths.AppExePath,
            "--test-project \"" + fixture + "\"");
        try
        {
            using var automation = new UIA3Automation();
            var mainWindow = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
            Assert.NotNull(mainWindow);
            System.Threading.Thread.Sleep(3000);
            DumpTree(mainWindow, "main");
            Log("main dump written");

            // Multi-strategy search
            AutomationElement component = null;
            var cf1 = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("firstOrderLag1"));
            Log("ByAutomationId 'firstOrderLag1': " + (cf1 != null));
            if (cf1 != null) component = cf1;
            if (component == null) {
                var cf2 = mainWindow.FindFirstDescendant(cf => cf.ByName("firstOrderLag1"));
                Log("ByName 'firstOrderLag1': " + (cf2 != null));
                if (cf2 != null) component = cf2;
            }
            if (component == null) {
                var cf3 = mainWindow.FindFirstDescendant(cf => cf.ByName("First Order Lag"));
                Log("ByName 'First Order Lag': " + (cf3 != null));
                if (cf3 != null) component = cf3;
            }
            if (component == null) {
                var canvas = mainWindow.FindFirstDescendant(cf => cf.ByAutomationId("DesignCanvas"));
                Log("DesignCanvas found: " + (canvas != null));
                if (canvas != null) {
                    DumpTree(canvas, "canvas");
                    var cf4 = canvas.FindFirstChild();
                    Log("canvas first child: " + (cf4 != null));
                }
            }
            Log("component: " + (component != null));
            Assert.NotNull(component);

            // Enumerate BEFORE double-click
            Log("=== BEFORE double-click: desktop windows ===");
            try {
                foreach (var c in automation.GetDesktop().FindAllChildren())
                {
                    try { Log("  BEFORE: Id='" + c.Properties.AutomationId.ValueOrDefault + "' Name='" + c.Properties.Name.ValueOrDefault + "' Type=" + c.Properties.ControlType.ValueOrDefault); } catch { }
                }
            } catch (Exception ex) { Log("BEFORE enum err: " + ex.Message); }

            // Bring window to foreground (critical: app is behind other windows)
            try { mainWindow.SetForeground(); Log("SetForeground OK"); } catch (Exception ex) { Log("SetForeground err: " + ex.Message); }
            System.Threading.Thread.Sleep(500);

            // Focus component and click via coordinates
            try { component.Focus(); } catch { }
            System.Threading.Thread.Sleep(200);

            var rect = component.BoundingRectangle;
            int cx = (int)(rect.Left + rect.Width / 2);
            int cy = (int)(rect.Top + rect.Height / 2);
            Log("click coords: " + cx + "," + cy + " size=" + rect.Width + "x" + rect.Height);

            FlaUI.Core.Input.Mouse.MoveTo(cx, cy);
            System.Threading.Thread.Sleep(150);
            FlaUI.Core.Input.Mouse.DoubleClick(FlaUI.Core.Input.MouseButton.Left);
            Log("mouse double-click sent");
            Log("double-clicked");
            System.Threading.Thread.Sleep(3000);

            // Enumerate AFTER double-click
            Log("=== AFTER double-click: desktop windows ===");
            try {
                foreach (var c in automation.GetDesktop().FindAllChildren())
                {
                    try { Log("  AFTER: Id='" + c.Properties.AutomationId.ValueOrDefault + "' Name='" + c.Properties.Name.ValueOrDefault + "' Type=" + c.Properties.ControlType.ValueOrDefault); } catch { }
                }
            } catch (Exception ex) { Log("AFTER enum err: " + ex.Message); }

            // Multi-strategy search (ByAutomationId first!)
            Window editor = null;
            var deadline = DateTime.UtcNow.AddSeconds(10);
            while (DateTime.UtcNow < deadline && editor == null)
            {
                editor = automation.GetDesktop().FindFirstChild(cf => cf.ByAutomationId("PythonEditorForm"))?.AsWindow();
                if (editor != null) { Log("editor: found ByAutomationId"); break; }

                editor = automation.GetDesktop().FindFirstChild(cf => cf.ByName("Python Code Editor"))?.AsWindow();
                if (editor != null) { Log("editor: found ByName('Python Code Editor')"); break; }

                editor = automation.GetDesktop().FindFirstChild(cf => cf.ByName("Python 代码编辑器"))?.AsWindow();
                if (editor != null) { Log("editor: found ByName('Python 代码编辑器')"); break; }

                System.Threading.Thread.Sleep(500);
            }

            Log("editor final: " + (editor != null));
            Assert.NotNull(editor);
        }
        finally { try { app.Kill(); } catch { } }
    }

    [Fact(Skip = "Depends on DoubleClick_OpensEditor (skipped). Covered by L2 tests PhysicsModelPickerFormTests.")]
    public void AddPhysics_InsertsCode()
    {
        Assert.True(false, "TODO: implement after DoubleClick works");
    }
}
