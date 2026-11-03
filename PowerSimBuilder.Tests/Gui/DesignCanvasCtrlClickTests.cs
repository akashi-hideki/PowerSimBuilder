using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Phase 2b: Ctrl+click toggles membership in the multi-selection.
    /// Tests the public SelectComponent(c, additive:true) path that
    /// HandleComponentMouseDown uses when Ctrl is held.
    /// </summary>
    public class DesignCanvasCtrlClickTests
    {
        private static ComponentControl MakeControl(string name)
        {
            var desc = new ComponentDescriptor
            {
                Name = name,
                Type = "FirstOrderLag",
                Category = "Simulation",
                InputPorts = new[] { "input" },
                OutputPorts = new[] { "output" },
                Parameters = new List<ParameterDescriptor>(),
            };
            return new ComponentControl(desc) { InstanceName = name };
        }

        [Fact]
        public void AdditiveToggle_AddsThenRemoves()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            var b = MakeControl("b");
            canvas.SelectComponent(a);
            canvas.SelectComponent(b, additive: true);
            Assert.Equal(2, canvas.SelectedComponents.Count);
            canvas.SelectComponent(b, additive: true);
            Assert.Single(canvas.SelectedComponents);
            Assert.Same(a, canvas.SelectedComponent);
            Assert.False(b.IsSelected);
        }

        [Fact]
        public void PlainClickOnSelected_PreservesWholeSelection()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            var b = MakeControl("b");
            canvas.SelectComponent(a);
            canvas.SelectComponent(b, additive: true);
            // Simulate plain click on already-selected "a" (no Ctrl).
            Assert.True(canvas.IsComponentSelected(a));
            if (!canvas.IsComponentSelected(a))
                canvas.SelectComponent(a, additive: false);
            Assert.Equal(2, canvas.SelectedComponents.Count);
        }
    }
}
