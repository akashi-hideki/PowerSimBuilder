using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Regression tests for v0.7.1-v0.7.3 fixes.
    /// Verifies that the fixes did not break existing behaviors.
    /// </summary>
    public class DesignCanvasRegressionTests
    {
        private static ComponentDescriptor PickDescriptor()
        {
            foreach (var d in ComponentDescriptorDictionary.Descriptors.Values)
            {
                if (d.InputPorts == null || d.InputPorts.Length == 0) continue;
                if (d.OutputPorts == null || d.OutputPorts.Length == 0) continue;
                if (d.InputPorts[0] != d.OutputPorts[0]) return d;
            }
            return ComponentDescriptorDictionary.Descriptors.Values
                .First(x => x.InputPorts != null && x.InputPorts.Length > 0
                         && x.OutputPorts != null && x.OutputPorts.Length > 0);
        }

        private static ComponentControl AddComp(DesignCanvas c, int x, int y)
        {
            c.AddComponent(PickDescriptor(), new Point(x, y));
            return c.Components[c.Components.Count - 1];
        }

        private static Point InputPortCenter(ComponentControl c, int i)
        {
            int n = c.Descriptor.InputPorts.Length;
            int y = (int)(c.Height * (i + 1f) / (n + 1));
            return new Point(ComponentControl.PortRadius, y);
        }

        private static Point OutputPortCenter(ComponentControl c, int i)
        {
            int n = c.Descriptor.OutputPorts.Length;
            int y = (int)(c.Height * (i + 1f) / (n + 1));
            return new Point(c.Width - ComponentControl.PortRadius, y);
        }

        private static void ClickPort(ComponentControl comp, Point pt)
        {
            GuiReflection.RaiseMouseDown(comp, MouseButtons.Left, pt.X, pt.Y);
        }

        private static void ClickBody(ComponentControl comp)
        {
            GuiReflection.RaiseMouseDown(comp, MouseButtons.Left, comp.Width / 2, comp.Height / 2);
        }

        private static void RaiseDelete(DesignCanvas c)
        {
            var m = typeof(DesignCanvas).GetMethod("HandleKeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(c, new object[] { new KeyEventArgs(Keys.Delete) });
        }

        private static bool GetIsSelected(ComponentControl c)
        {
            var prop = typeof(ComponentControl).GetProperty("IsSelected");
            return (bool)prop.GetValue(c);
        }

        [Fact]
        public void R01_DeleteSelectedComponent_ClearsSelectedComponent()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                ClickBody(c1);
                Assert.Same(c1, canvas.SelectedComponent);
                RaiseDelete(canvas);
                Assert.Empty(canvas.Components);
                Assert.Null(canvas.SelectedComponent);
            });
        }

        [Fact]
        public void R02_LeftClickBody_StartsDrag()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                ClickBody(c1);
                var isDragging = (bool)GuiReflection.GetField(canvas, "_isDragging");
                var draggedComp = GuiReflection.GetField(canvas, "_draggedComponent");
                Assert.True(isDragging, "Left-click on body should start drag");
                Assert.Same(c1, draggedComp);
            });
        }

        [Fact]
        public void R03_DragBody_MovesComponent()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                var startLoc = c1.Location;
                ClickBody(c1);
                GuiReflection.RaiseMouseMove(c1, MouseButtons.Left, 150, 150);
                Assert.NotEqual(startLoc, c1.Location);
            });
        }

        [Fact]
        public void R04_LeftClickPort_StartsConnection()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                ClickPort(c1, OutputPortCenter(c1, 0));
                var pending = GuiReflection.GetField(canvas, "_currentConnection");
                Assert.NotNull(pending);
                Assert.Empty(canvas.Connections);
            });
        }

        [Fact]
        public void R05_PendingConnection_RightClickOther_Cancels()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                Assert.NotNull(GuiReflection.GetField(canvas, "_currentConnection"));
                GuiReflection.RaiseMouseDown(c2, MouseButtons.Right, c2.Width / 2, c2.Height / 2);
                Assert.Null(GuiReflection.GetField(canvas, "_currentConnection"));
                Assert.Empty(canvas.Connections);
            });
        }

        [Fact]
        public void R06_CancelThenNewConnection_Works()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                GuiReflection.RaiseMouseDown(c2, MouseButtons.Right, c2.Width / 2, c2.Height / 2);
                Assert.Null(GuiReflection.GetField(canvas, "_currentConnection"));
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Single(canvas.Connections);
            });
        }

        [Fact]
        public void R07_SelectDeleteSelect_Works()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickBody(c1);
                RaiseDelete(canvas);
                ClickBody(c2);
                Assert.Same(c2, canvas.SelectedComponent);
                Assert.Single(canvas.Components);
            });
        }

        [Fact]
        public void R08_RightClickSwitchSelection_UnselectsPrevious()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickBody(c1);
                Assert.True(GetIsSelected(c1));
                GuiReflection.RaiseMouseDown(c2, MouseButtons.Right, c2.Width / 2, c2.Height / 2);
                Assert.False(GetIsSelected(c1), "Fix 3: c1 should be unselected on switch");
                Assert.True(GetIsSelected(c2));
                Assert.Same(c2, canvas.SelectedComponent);
            });
        }

        [Fact]
        public void R09_EmptyClick_UnselectsComponent()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                ClickBody(c1);
                Assert.True(GetIsSelected(c1));
                var m = typeof(DesignCanvas).GetMethod("HandleMouseDown",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                m.Invoke(canvas, new object[] { new Point(5000, 5000) });
                Assert.Null(canvas.SelectedComponent);
                Assert.False(GetIsSelected(c1), "Fix 4: c1 should be unselected on empty click");
            });
        }

        [Fact]
        public void R10_NormalConnection_StillWorks_AfterFix5()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Single(canvas.Connections);
                var conn = canvas.Connections[0];
                Assert.Same(c1, conn.StartComponent);
                Assert.Same(c2, conn.EndComponent);
                Assert.Equal(c1.Descriptor.OutputPorts[0], conn.StartPort);
                Assert.Equal(c2.Descriptor.InputPorts[0], conn.EndPort);
            });
        }

        [Fact]
        public void R11_DragEnd_ResetsDraggingState()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                ClickBody(c1);
                Assert.True((bool)GuiReflection.GetField(canvas, "_isDragging"));
                GuiReflection.RaiseMouseUp(c1, MouseButtons.Left, 150, 150);
                Assert.False((bool)GuiReflection.GetField(canvas, "_isDragging"));
                Assert.Null(GuiReflection.GetField(canvas, "_draggedComponent"));
            });
        }

        [Fact]
        public void R12_DeleteComponentWithConnection_CascadeStillWorks()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Single(canvas.Connections);
                canvas.RemoveComponent(c1);
                Assert.Empty(canvas.Connections);
                Assert.Single(canvas.Components);
            });
        }
    }
}
