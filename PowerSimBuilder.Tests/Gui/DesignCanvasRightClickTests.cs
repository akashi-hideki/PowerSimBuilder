using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    public class DesignCanvasRightClickTests
    {
        private static ComponentDescriptor PickWithInputs()
        {
            return ComponentDescriptorDictionary.Descriptors.Values
                .First(d => d.InputPorts != null && d.InputPorts.Length > 0
                         && d.OutputPorts != null && d.OutputPorts.Length > 0);
        }

        private static Point InputPortCenter(ComponentControl c, int i)
        {
            int n = c.Descriptor.InputPorts.Length;
            int y = (int)(c.Height * (i + 1f) / (n + 1));
            return new Point(ComponentControl.PortRadius, y);
        }

        [Fact]
        public void P1_04c_RightClickOnPort_DoesNotSetCurrentConnection()
        {
            GuiSta.Run(() =>
            {
                var d = PickWithInputs();
                using var canvas = new DesignCanvas();
                canvas.AddComponent(d, new Point(100, 100));
                var comp = canvas.Components[0];
                var pt = InputPortCenter(comp, 0);
                GuiReflection.RaiseMouseDown(comp, MouseButtons.Right, pt.X, pt.Y);
                Assert.Null(GuiReflection.GetField(canvas, "_currentConnection"));
            });
        }

        [Fact]
        public void P3_03_RightClick_ClearsPendingConnection()
        {
            GuiSta.Run(() =>
            {
                var d = PickWithInputs();
                using var canvas = new DesignCanvas();
                canvas.AddComponent(d, new Point(100, 100));
                var comp = canvas.Components[0];
                var pending = new Connection
                {
                    StartComponent = comp,
                    StartPort = comp.Descriptor.OutputPorts[0],
                    Visual = new ConnectionVisual()
                };
                GuiReflection.SetField(canvas, "_currentConnection", pending);
                GuiReflection.RaiseMouseDown(comp, MouseButtons.Right, comp.Width / 2, comp.Height / 2);
                Assert.Null(GuiReflection.GetField(canvas, "_currentConnection"));
            });
        }

        [Fact]
        public void P1_04d_RightClick_SelectsClickedComponent_NotPrevious()
        {
            GuiSta.Run(() =>
            {
                var ds = ComponentDescriptorDictionary.Descriptors.Values
                    .Where(d => d.InputPorts != null && d.InputPorts.Length > 0
                             && d.OutputPorts != null && d.OutputPorts.Length > 0)
                    .Take(2).ToList();
                Assert.True(ds.Count >= 2, "Need 2 distinct descriptors");
                using var canvas = new DesignCanvas();
                canvas.AddComponent(ds[0], new Point(50, 50));
                canvas.AddComponent(ds[1], new Point(300, 50));
                var comp1 = canvas.Components[0];
                var comp2 = canvas.Components[1];
                GuiReflection.RaiseMouseDown(comp1, MouseButtons.Left, comp1.Width / 2, comp1.Height / 2);
                Assert.Same(comp1, canvas.SelectedComponent);
                var pt = InputPortCenter(comp2, 0);
                GuiReflection.RaiseMouseDown(comp2, MouseButtons.Right, pt.X, pt.Y);
                Assert.Same(comp2, canvas.SelectedComponent);
            });
        }

        [Fact]
        public void P3_02_EmptyCanvasLeftClick_ClearsPendingConnection()
        {
            GuiSta.Run(() =>
            {
                var d = PickWithInputs();
                using var canvas = new DesignCanvas();
                canvas.AddComponent(d, new Point(100, 100));
                var comp = canvas.Components[0];
                var pending = new Connection
                {
                    StartComponent = comp,
                    StartPort = comp.Descriptor.OutputPorts[0],
                    Visual = new ConnectionVisual()
                };
                GuiReflection.SetField(canvas, "_currentConnection", pending);
                var handleMouseDown = typeof(DesignCanvas).GetMethod("HandleMouseDown",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(handleMouseDown);
                handleMouseDown.Invoke(canvas, new object[] { new Point(500, 500) });
                Assert.Null(GuiReflection.GetField(canvas, "_currentConnection"));
            });
        }

        [Fact]
        public void P1_04e_RightClick_DoesNotCreateConnection()
        {
            GuiSta.Run(() =>
            {
                var d = PickWithInputs();
                using var canvas = new DesignCanvas();
                canvas.AddComponent(d, new Point(100, 100));
                var comp = canvas.Components[0];
                var pt = InputPortCenter(comp, 0);
                int before = canvas.Connections.Count;
                GuiReflection.RaiseMouseDown(comp, MouseButtons.Right, pt.X, pt.Y);
                Assert.Equal(before, canvas.Connections.Count);
            });
        }
    }
}
