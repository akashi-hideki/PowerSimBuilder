#nullable enable
using System;
using System.Collections.Generic;
using PowerSimBuilder;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests;

public class PhysicsModelPickerFormTests
{
    private static PhysicsModelEntry MakeEntry(
        string name,
        string[]? inputs = null,
        string[]? outputs = null)
    {
        inputs ??= new[] { "u" };
        outputs ??= new[] { "y" };
        var info = new FmuInfo(
            ModelName: name,
            FmiVersion: "2.0",
            FmiType: "CoSimulation",
            Inputs: inputs,
            Outputs: outputs,
            Parameters: new List<FmuParameter>(),
            Timing: null);
        return new PhysicsModelEntry(
            name,
            "/tmp/" + name,
            "/tmp/" + name + "/model.fmu",
            name + "/source/model.fmu",
            info);
    }

    [Fact]
    public void Ctor_WithEntries_PopulatesList()
    {
        var entries = new[] { MakeEntry("PMSM"), MakeEntry("Feedthrough") };
        using var form = new PhysicsModelPickerForm(entries);
        Assert.Equal(2, form.Test_ListView.Items.Count);
        Assert.Equal("PMSM", form.Test_ListView.Items[0].Text);
    }

    [Fact]
    public void Ctor_Empty_ShowsNoModelMessage()
    {
        using var form = new PhysicsModelPickerForm(Array.Empty<PhysicsModelEntry>());
        Assert.Empty(form.Test_ListView.Items);
        Assert.Contains("No physics models", form.Test_StatusLabel.Text);
    }

    [Fact]
    public void SelectFirst_EnablesInsertAndOpenFolder()
    {
        using var form = new PhysicsModelPickerForm(new[] { MakeEntry("PMSM") });
        form.Test_SelectFirst();
        Assert.True(form.Test_InsertButton.Enabled);
        Assert.True(form.Test_OpenFolderButton.Enabled);
    }

    [Fact]
    public void RefreshSelection_NoSelection_DisablesButtons()
    {
        using var form = new PhysicsModelPickerForm(new[] { MakeEntry("PMSM") });
        form.Test_ListView.SelectedItems.Clear();
        form.Test_RefreshSelection();
        Assert.False(form.Test_InsertButton.Enabled);
        Assert.False(form.Test_OpenFolderButton.Enabled);
    }

    [Fact]
    public void Detail_ShowsTypedPortsAndModelName()
    {
        using var form = new PhysicsModelPickerForm(new[]
        {
            MakeEntry("PMSM", new[] { "u_d", "u_q" }, new[] { "i_d" })
        });
        form.Test_SelectFirst();
        var detail = form.Test_DetailTextBox.Text;
        Assert.Contains("Model     : PMSM", detail);
        Assert.Contains("u_d : Real", detail);
        Assert.Contains("u_q : Real", detail);
        Assert.Contains("i_d : Real", detail);
        Assert.Contains("FMI       : 2.0 (CoSimulation)", detail);
    }

    [Fact]
    public void InsertClick_SetsSelectedModel()
    {
        var e = MakeEntry("PMSM");
        using var form = new PhysicsModelPickerForm(new[] { e });
        form.Test_SelectFirst();
        form.Test_InvokeInsert();
        Assert.Same(e, form.SelectedModel);
    }

    [Fact]
    public void InsertClick_NoSelection_DoesNotSetModel()
    {
        using var form = new PhysicsModelPickerForm(Array.Empty<PhysicsModelEntry>());
        form.Test_InsertButton.Enabled = true;
        form.Test_InvokeInsert();
        Assert.Null(form.SelectedModel);
    }

    [Fact]
    public void ListView_HasFourColumns()
    {
        using var form = new PhysicsModelPickerForm(Array.Empty<PhysicsModelEntry>());
        Assert.Equal(4, form.Test_ListView.Columns.Count);
    }
}