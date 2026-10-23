#nullable enable
using System;
using PowerSimBuilder.ComponentEdit;
using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit;

public class EditPanelPhysicsTests
{
    [Fact]
    public void PhysicsButton_Exists()
    {
        using var panel = new EditPanel();
        Assert.NotNull(panel.Test_PhysicsButton);
    }

    [Fact]
    public void PhysicsButton_Click_RaisesPhysicsRequested()
    {
        using var panel = new EditPanel();
        int calls = 0;
        panel.PhysicsRequested += (s, e) => calls++;

        panel.Test_PhysicsButton.PerformClick();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void PhysicsButton_DoesNotRaiseOtherEvents()
    {
        using var panel = new EditPanel();
        int overrideCalls = 0, adapterCalls = 0, importCCalls = 0;
        panel.OverrideRequested += (s, e) => overrideCalls++;
        panel.AdapterRequested  += (s, e) => adapterCalls++;
        panel.ImportCRequested  += (s, e) => importCCalls++;

        panel.Test_PhysicsButton.PerformClick();

        Assert.Equal(0, overrideCalls);
        Assert.Equal(0, adapterCalls);
        Assert.Equal(0, importCCalls);
    }
}