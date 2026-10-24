using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.Physics;

public class PhysicsBlockPickerFormTests
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

    private static string MakeInitBlock(string name) =>
        $"# === physics: {name} (auto-inserted) ===\n" +
        $"self.{name.ToLower()} = load_fmu(...)\n" +
        $"# === /physics: {name} ===\n";

    [Fact]
    public void Form_HasNameAndTitle()
    {
        RunSta(() =>
        {
            using var form = new PhysicsBlockPickerForm("");
            Assert.Equal("PhysicsBlockPickerForm", form.Name);
            Assert.False(string.IsNullOrWhiteSpace(form.Text));
        });
    }

    [Fact]
    public void Populate_EmptyCode_ShowsZeroItems()
    {
        RunSta(() =>
        {
            using var form = new PhysicsBlockPickerForm("def foo():\n    pass\n");
            Assert.Empty(form.Test_ListView.Items.Cast<ListViewItem>());
            Assert.False(string.IsNullOrWhiteSpace(form.Test_StatusLabel.Text));
        });
    }

    [Fact]
    public void Populate_OneBlock_ListsIt()
    {
        RunSta(() =>
        {
            var code = MakeInitBlock("PMSM");
            using var form = new PhysicsBlockPickerForm(code);
            Assert.Single(form.Test_ListView.Items.Cast<ListViewItem>());
            Assert.Equal("PMSM", form.Test_ListView.Items[0].Text);
        });
    }

    [Fact]
    public void Populate_TwoBlocks_ListsBoth()
    {
        RunSta(() =>
        {
            var code = MakeInitBlock("PMSM") + "\n" + MakeInitBlock("Feedthrough");
            using var form = new PhysicsBlockPickerForm(code);
            Assert.Equal(2, form.Test_ListView.Items.Count);
        });
    }

    [Fact]
    public void SelectFirst_EnablesRemoveButton()
    {
        RunSta(() =>
        {
            var code = MakeInitBlock("PMSM");
            using var form = new PhysicsBlockPickerForm(code);
            form.Test_SelectFirst();
            Assert.True(form.Test_RemoveButton.Enabled);
        });
    }

    [Fact]
    public void InvokeRemove_SetsSelectedBlock()
    {
        RunSta(() =>
        {
            var code = MakeInitBlock("PMSM");
            using var form = new PhysicsBlockPickerForm(code);
            form.Test_SelectFirst();
            form.Test_InvokeRemove();
            Assert.NotNull(form.SelectedBlock);
            Assert.Equal("PMSM", form.SelectedBlock!.ModelName);
        });
    }
}