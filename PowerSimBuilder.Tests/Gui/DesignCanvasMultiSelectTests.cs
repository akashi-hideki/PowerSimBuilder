using System;
using System.Collections.Generic;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Phase 1 multi-select core: SelectedComponents API tests.
    /// </summary>
    public class DesignCanvasMultiSelectTests
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
        public void SelectComponent_NonAdditive_ReplacesSelection()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            var b = MakeControl("b");
            canvas.SelectComponent(a);
            Assert.Single(canvas.SelectedComponents);
            Assert.Same(a, canvas.SelectedComponent);
            canvas.SelectComponent(b);
            Assert.Single(canvas.SelectedComponents);
            Assert.Same(b, canvas.SelectedComponent);
            Assert.False(a.IsSelected);
            Assert.True(b.IsSelected);
        }

        [Fact]
        public void SelectComponent_Additive_AppendsSelection()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            var b = MakeControl("b");
            canvas.SelectComponent(a);
            canvas.SelectComponent(b, additive: true);
            Assert.Equal(2, canvas.SelectedComponents.Count);
            Assert.True(a.IsSelected);
            Assert.True(b.IsSelected);
            Assert.Same(a, canvas.SelectedComponent);
        }

        [Fact]
        public void SelectComponents_ReplacesAll()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            var b = MakeControl("b");
            var c = MakeControl("c");
            canvas.SelectComponent(c);
            canvas.SelectComponents(new[] { a, b });
            Assert.Equal(2, canvas.SelectedComponents.Count);
            Assert.Contains(a, canvas.SelectedComponents);
            Assert.Contains(b, canvas.SelectedComponents);
            Assert.False(c.IsSelected);
        }

        [Fact]
        public void ClearSelection_EmptiesList()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            canvas.SelectComponent(a);
            canvas.ClearSelection();
            Assert.Empty(canvas.SelectedComponents);
            Assert.Null(canvas.SelectedComponent);
            Assert.False(a.IsSelected);
        }

        [Fact]
        public void IsComponentSelected_ReflectsMembership()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            var b = MakeControl("b");
            canvas.SelectComponent(a);
            Assert.True(canvas.IsComponentSelected(a));
            Assert.False(canvas.IsComponentSelected(b));
            canvas.SelectComponent(b, additive: true);
            Assert.True(canvas.IsComponentSelected(b));
        }

        [Fact]
        public void SelectedComponents_IsReadOnly()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a");
            canvas.SelectComponent(a);
            Assert.IsAssignableFrom<System.Collections.Generic.IReadOnlyList<ComponentControl>>
                (canvas.SelectedComponents);
        }
    }
}
