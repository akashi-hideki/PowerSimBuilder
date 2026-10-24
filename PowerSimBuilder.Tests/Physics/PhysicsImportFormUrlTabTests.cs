using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

/// <summary>
/// MVP-β Phase 3: verifies PhysicsImportForm wraps in TabControl (File + URL)
/// and that URL tab UI exists.
/// </summary>
public class PhysicsImportFormUrlTabTests
{
    private static void RunSta(Action action)
    {
        Exception ex = null;
        var t = new Thread(() => { try { action(); } catch (Exception e) { ex = e; } });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (ex != null) throw ex;
    }

    private static T? FindByType<T>(Control root) where T : Control
    {
        foreach (Control c in root.Controls)
        {
            if (c is T t) return t;
            var inner = FindByType<T>(c);
            if (inner != null) return inner;
        }
        return null;
    }

    [Fact]
    public void Form_HasTabControlWithFileAndUrlTabs()
    {
        RunSta(() =>
        {
            using var form = new PhysicsImportForm();
            var tabs = FindByType<TabControl>(form);
            Assert.NotNull(tabs);
            Assert.Equal(2, tabs!.TabPages.Count);
            Assert.Equal("TabFile", tabs.TabPages[0].Name);
            Assert.Equal("TabUrl", tabs.TabPages[1].Name);
        });
    }

    [Fact]
    public void UrlTab_HasUrlTextboxAndDownloadButton()
    {
        RunSta(() =>
        {
            using var form = new PhysicsImportForm();
            var urlTab = form.Test_TabUrl;
            Assert.NotNull(urlTab);

            var txt = urlTab!.Controls.Cast<Control>()
                .SelectMany(FindAllTextboxes)
                .FirstOrDefault(t => t.Name == "TxtUrl");
            Assert.NotNull(txt);

            var btn = urlTab.Controls.Cast<Control>()
                .SelectMany(FindAllButtons)
                .FirstOrDefault(b => b.Name == "BtnDownload");
            Assert.NotNull(btn);
        });
    }

    [Fact]
    public void FileTab_PreservesOriginalControls()
    {
        RunSta(() =>
        {
            using var form = new PhysicsImportForm();
            var fileTab = form.Test_TabFile;
            Assert.NotNull(fileTab);

            // Original controls (e.g., the FMU file textbox) should be under File tab
            var hasAnyControl = fileTab!.Controls.Count > 0;
            Assert.True(hasAnyControl, "File tab should contain the original import controls");
        });
    }

    [Fact]
    public void Test_TabControl_HasNameForAutomation()
    {
        RunSta(() =>
        {
            using var form = new PhysicsImportForm();
            var tabs = form.Test_TabControl;
            Assert.NotNull(tabs);
            Assert.Equal("TabControl", tabs!.Name);
        });
    }

    // Recursive helpers
    private static System.Collections.Generic.IEnumerable<TextBox> FindAllTextboxes(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is TextBox tb) yield return tb;
            foreach (var inner in FindAllTextboxes(c)) yield return inner;
        }
    }

    private static System.Collections.Generic.IEnumerable<Button> FindAllButtons(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is Button b) yield return b;
            foreach (var inner in FindAllButtons(c)) yield return inner;
        }
    }
}