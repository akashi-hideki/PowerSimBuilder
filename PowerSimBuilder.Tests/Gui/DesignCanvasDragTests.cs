using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// M3: drag moves ALL selected components by the same delta.
    /// Calls StartDragging/DragComponent via reflection (private internals).
    /// </summary>
    public class DesignCanvasDragTests
    {
        private static ComponentControl MakeControl(string name, int x, int y)
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
            return new ComponentControl(desc)
            {
                InstanceName = name,
                Location = new Point(x, y),
            };
        }

        private static void InvokePrivate(DesignCanvas canvas, string method, params object[] args)
        {
            var m = typeof(DesignCanvas).GetMethod(method,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            m!.Invoke(canvas, args);
        }

        [Fact]
        public void DragComponent_MovesAllSelectedBySameDelta()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a", 100, 100);
            var b = MakeControl("b", 200, 150);
            var c = MakeControl("c", 300, 200);
            canvas.AddComponent(a);
            canvas.AddComponent(b);
            canvas.AddComponent(c);
            canvas.SelectComponent(a);
            canvas.SelectComponent(b, additive: true);
            canvas.SelectComponent(c, additive: true);
            Assert.Equal(3, canvas.SelectedComponents.Count);

            InvokePrivate(canvas, "StartDragging", a, new Point(0, 0));
            InvokePrivate(canvas, "DragComponent", a, new Point(50, 30));

            Assert.Equal(new Point(150, 130), a.Location);
            Assert.Equal(new Point(250, 180), b.Location);
            Assert.Equal(new Point(350, 230), c.Location);

            InvokePrivate(canvas, "StopDragging");
        }

        [Fact]
        public void DragComponent_SingleSelection_MovesOnly()
        {
            var canvas = new DesignCanvas();
            var a = MakeControl("a", 100, 100);
            var b = MakeControl("b", 200, 150);
            canvas.AddComponent(a);
            canvas.AddComponent(b);
            canvas.SelectComponent(a);

            InvokePrivate(canvas, "StartDragging", a, new Point(0, 0));
            InvokePrivate(canvas, "DragComponent", a, new Point(25, 15));

            Assert.Equal(new Point(125, 115), a.Location);
            Assert.Equal(new Point(200, 150), b.Location);

            InvokePrivate(canvas, "StopDragging");
        }
    }
}
