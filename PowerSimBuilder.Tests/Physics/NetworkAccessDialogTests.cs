using System;
using System.Threading;
using System.Windows.Forms;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class NetworkAccessDialogTests
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

    private static TextBox? FindUrlBox(Control root)
    {
        foreach (Control c in root.Controls)
        {
            if (c is TextBox tb && tb.Name == "TxtUrl") return tb;
            var inner = FindUrlBox(c);
            if (inner != null) return inner;
        }
        return null;
    }

    [Fact]
    public void Form_HasNameAndTitle()
    {
        RunSta(() =>
        {
            using var dlg = new NetworkAccessDialog("https://example.com/model.fmu", "model.fmu");
            Assert.Equal("NetworkAccessDialog", dlg.Name);
            Assert.False(string.IsNullOrWhiteSpace(dlg.Text));
        });
    }

    [Fact]
    public void Form_ShowsUrlInTextBox()
    {
        RunSta(() =>
        {
            var url = "https://example.com/model.fmu";
            using var dlg = new NetworkAccessDialog(url, "model.fmu", "180 KB", "BSD-2-Clause");
            var txt = FindUrlBox(dlg);
            Assert.NotNull(txt);
            Assert.Equal(url, txt!.Text);
        });
    }

    [Fact]
    public void Form_AlwaysAllowDefaultIsFalse()
    {
        RunSta(() =>
        {
            using var dlg = new NetworkAccessDialog("https://example.com/model.fmu", "model.fmu");
            Assert.False(dlg.Test_AlwaysAllowCheckbox.Checked);
        });
    }

    [Fact]
    public void Form_UrlPreservedInProperty()
    {
        RunSta(() =>
        {
            var url = "https://example.com/test.fmu";
            using var dlg = new NetworkAccessDialog(url, "test.fmu");
            Assert.Equal(url, dlg.Url);
        });
    }
}