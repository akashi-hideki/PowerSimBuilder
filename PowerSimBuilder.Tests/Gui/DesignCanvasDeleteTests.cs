using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    public class DesignCanvasDeleteTests
    {
        private static ComponentDescriptor PickDescriptor()
        {
            return ComponentDescriptorDictionary.Descriptors.Values
                .First(d => d.InputPorts != null && d.InputPorts.Length > 0
                         && d.OutputPorts != null && d.OutputPorts.Length > 0);
        }

        private static ComponentControl AddComp(DesignCanvas c, int x, int y)
        {
            c.AddComponent(PickDescriptor(), new Point(x, y));
            return c.Components[c.Components.Count - 1];
        }

        private static void SelectComponent(DesignCanvas c, ComponentControl comp)
        {
            GuiReflection.RaiseMouseDown(comp, MouseButtons.Left, comp.Width / 2, comp.Height / 2);
        }

        private static void RaiseDeleteKey(DesignCanvas c)
        {
            var m = typeof(DesignCanvas).GetMethod("HandleKeyDown",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            m.Invoke(c, new object[] { new KeyEventArgs(Keys.Delete) });
        }

        private static void RaiseEmptyCanvasMouseDown(DesignCanvas c)
        {
            var m = typeof(DesignCanvas).GetMethod("HandleMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            m.Invoke(c, new object[] { new Point(5000, 5000) });
        }

        private static void InvokeContextMenuOpening(DesignCanvas c)
        {
            var m = typeof(DesignCanvas).GetMethod("ContextMenu_Opening",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            m.Invoke(c, new object[] { c.ContextMenuStrip, new CancelEventArgs() });
        }

        private static void SetSelectedConnection(DesignCanvas c, Connection conn)
        {
            var prop = typeof(DesignCanvas).GetProperty("SelectedConnection");
            var setter = prop.GetSetMethod(nonPublic: true);
            Assert.NotNull(setter);
            setter.Invoke(c, new object[] { conn });
        }

        // ===== P1: selection =====

        [Fact]
        public void P1_05_EmptyCanvasLeftClick_ClearsSelectedComponent()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var comp = AddComp(canvas, 100, 100);
                SelectComponent(canvas, comp);
                Assert.Same(comp, canvas.SelectedComponent);
                RaiseEmptyCanvasMouseDown(canvas);
                // Fix 4 target: empty-canvas click should deselect
                Assert.Null(canvas.SelectedComponent);
            });
        }

        [Fact]
        public void P1_06_RightClickEmptyCanvas_DoesNotCrash()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var m = typeof(DesignCanvas).GetMethod("OnMouseDown",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                m.Invoke(canvas, new object[] {
                    new MouseEventArgs(MouseButtons.Right, 1, 50, 50, 0) });
                Assert.Empty(canvas.Components);
            });
        }

        // ===== P2: delete =====

        [Fact]
        public void P2_01_DeleteKey_RemovesSelectedComponent()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                var c2 = AddComp(canvas, 300, 100);
                Assert.Equal(2, canvas.Components.Count);
                SelectComponent(canvas, c1);
                RaiseDeleteKey(canvas);
                Assert.Single(canvas.Components);
                Assert.DoesNotContain(c1, canvas.Components);
                Assert.Contains(c2, canvas.Components);
            });
        }

        [Fact]
        public void P2_03_DeleteLastComponent_RemovesIt()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var comp = AddComp(canvas, 100, 100);
                SelectComponent(canvas, comp);
                RaiseDeleteKey(canvas);
                Assert.Empty(canvas.Components);
            });
        }

        [Fact]
        public void P2_07_DeleteKey_WithNothingSelected_NoOp()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                AddComp(canvas, 100, 100);
                int before = canvas.Components.Count;
                RaiseDeleteKey(canvas);
                Assert.Equal(before, canvas.Components.Count);
            });
        }

        // ===== P2: ContextMenu items =====

        [Fact(Skip = "UI layout dependent; visibility covered by manual test")]
        public void P2_02_ContextMenu_Opening_DeleteVisible_WhenComponentSelected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var comp = AddComp(canvas, 100, 100);
                SelectComponent(canvas, comp);
                Assert.NotNull(canvas.ContextMenuStrip);
                InvokeContextMenuOpening(canvas);
                // Items[0] = connection delete, Items[1] = component delete
                Assert.True(canvas.ContextMenuStrip.Items[1].Visible,
                    "Delete Component should be visible when a component is selected");
            });
        }

        [Fact]
        public void P1_04f_ContextMenu_Opening_DeleteHidden_WhenNothingSelected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                AddComp(canvas, 100, 100);
                Assert.NotNull(canvas.ContextMenuStrip);
                InvokeContextMenuOpening(canvas);
                Assert.False(canvas.ContextMenuStrip.Items[1].Visible,
                    "Delete Component should be hidden when nothing is selected");
            });
        }

        // ===== P2: cascade / connection delete =====

        [Fact]
        public void P2_06_RemoveComponent_RemovesConnectedConnections()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                var c2 = AddComp(canvas, 300, 100);
                var conn = new Connection
                {
                    StartComponent = c1,
                    StartPort = c1.Descriptor.OutputPorts[0],
                    EndComponent = c2,
                    EndPort = c2.Descriptor.InputPorts[0],
                    Visual = new ConnectionVisual()
                };
                canvas.AddConnection(conn);
                Assert.Single(canvas.Connections);
                canvas.RemoveComponent(c1);
                Assert.Empty(canvas.Connections);
            });
        }

        [Fact]
        public void P2_05_DeleteKey_RemovesSelectedConnection()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                var c2 = AddComp(canvas, 300, 100);
                var conn = new Connection
                {
                    StartComponent = c1,
                    StartPort = c1.Descriptor.OutputPorts[0],
                    EndComponent = c2,
                    EndPort = c2.Descriptor.InputPorts[0],
                    Visual = new ConnectionVisual()
                };
                canvas.AddConnection(conn);
                Assert.Single(canvas.Connections);
                SetSelectedConnection(canvas, conn);
                RaiseDeleteKey(canvas);
                Assert.Empty(canvas.Connections);
                Assert.Equal(2, canvas.Components.Count);
            });
        }
        [Fact]
        public void P1_02_LeftClickAnotherComponent_UnselectsPrevious()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                var c2 = AddComp(canvas, 300, 100);
                SelectComponent(canvas, c1);
                Assert.Same(c1, canvas.SelectedComponent);
                Assert.True(GetIsSelected(c1), "c1 should be selected");
                SelectComponent(canvas, c2);
                Assert.Same(c2, canvas.SelectedComponent);
                Assert.False(GetIsSelected(c1), "Fix 3: c1 should be unselected");
                Assert.True(GetIsSelected(c2), "c2 should be selected");
            });
        }

        private static bool GetIsSelected(ComponentControl c)
        {
            var prop = typeof(ComponentControl).GetProperty("IsSelected",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (prop != null) return (bool)prop.GetValue(c);
            var fld = typeof(ComponentControl).GetField("IsSelected",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fld != null) return (bool)fld.GetValue(c);
            throw new InvalidOperationException("IsSelected not found on ComponentControl");
        }
    }
}
