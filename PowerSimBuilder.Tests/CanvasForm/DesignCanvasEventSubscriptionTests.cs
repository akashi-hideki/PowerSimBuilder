using System;
using System.Reflection;
using System.Threading;
using PowerSimBuilder.ComponentEdit;
using Xunit;

namespace PowerSimBuilder.Tests.CanvasForm;

/// <summary>
/// Guards against the historical bug where RegisterComponentEvents subscribed
/// the DoubleClicked handler twice, causing the editor to reopen after close/save.
/// </summary>
public class DesignCanvasEventSubscriptionTests
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

    private static int CountSubscribers(ComponentControl control, string eventName)
    {
        var field = typeof(ComponentControl).GetField(
            eventName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        var handler = field?.GetValue(control) as Delegate;
        return handler?.GetInvocationList().Length ?? 0;
    }

    private static ComponentDescriptor NewDescriptor()
    {
        return new ComponentDescriptor
        {
            Name = "TestComp",
            Type = "TestType",
            Category = "Test",
            InputPorts = new[] { "in" },
            OutputPorts = new[] { "out" }
        };
    }

    [Fact]
    public void AddComponent_SubscribesDoubleClickedExactlyOnce()
    {
        RunSta(() =>
        {
            var canvas = new DesignCanvas();
            var control = new ComponentControl(NewDescriptor());
            canvas.AddComponent(control);
            Assert.Equal(1, CountSubscribers(control, "DoubleClicked"));
        });
    }

    [Fact]
    public void AddSameComponentTwice_DoesNotDoubleSubscribe()
    {
        RunSta(() =>
        {
            var canvas = new DesignCanvas();
            var control = new ComponentControl(NewDescriptor());
            canvas.AddComponent(control);
            canvas.AddComponent(control);
            Assert.Equal(1, CountSubscribers(control, "DoubleClicked"));
        });
    }

    [Fact]
    public void RemoveComponent_UnsubscribesDoubleClicked()
    {
        RunSta(() =>
        {
            var canvas = new DesignCanvas();
            var control = new ComponentControl(NewDescriptor());
            canvas.AddComponent(control);
            Assert.Equal(1, CountSubscribers(control, "DoubleClicked"));

            canvas.RemoveComponent(control);
            Assert.Equal(0, CountSubscribers(control, "DoubleClicked"));
        });
    }
}
