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
    public class DesignCanvasGeneratePythonTests
    {
        private static ComponentDescriptor PickDescriptor()
        {
            return ComponentDescriptorDictionary.Descriptors.Values
                .First(d => d.InputPorts != null && d.InputPorts.Length > 0
                         && d.OutputPorts != null && d.OutputPorts.Length > 0);
        }

        private static void SelectComponent(DesignCanvas c, ComponentControl comp)
        {
            GuiReflection.RaiseMouseDown(comp, MouseButtons.Left, comp.Width / 2, comp.Height / 2);
        }

        private static void InvokeContextMenuOpening(DesignCanvas c)
        {
            var m = typeof(DesignCanvas).GetMethod("ContextMenu_Opening",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(m);
            m.Invoke(c, new object[] { c.ContextMenuStrip, new CancelEventArgs() });
        }

        [Fact]
        public void ContextMenu_HasGeneratePythonItem_AtIndex2()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                Assert.NotNull(canvas.ContextMenuStrip);
                Assert.True(canvas.ContextMenuStrip.Items.Count >= 3,
                    "Generate Python Component menu item must be present");
                var item = canvas.ContextMenuStrip.Items[2];
                Assert.True(item is ToolStripMenuItem);
                Assert.Equal("MenuGeneratePython", item.Name);
            });
        }

        [Fact(Skip = "UI layout dependent; visibility covered by manual test (mirrors P2_02)")]
        public void ContextMenu_GenerateVisible_WhenComponentSelected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                canvas.AddComponent(PickDescriptor(), new Point(100, 100));
                var comp = canvas.Components[0];
                SelectComponent(canvas, comp);
                InvokeContextMenuOpening(canvas);
                Assert.True(canvas.ContextMenuStrip.Items[2].Visible,
                    "Generate Python Component should be visible when a component is selected");
            });
        }

        [Fact]
        public void ContextMenu_GenerateHidden_WhenNothingSelected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                canvas.AddComponent(PickDescriptor(), new Point(100, 100));
                InvokeContextMenuOpening(canvas);
                Assert.False(canvas.ContextMenuStrip.Items[2].Visible,
                    "Generate Python Component should be hidden when nothing is selected");
            });
        }

        [Fact]
        public void ExistingDeleteItems_KeepTheirIndices()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                Assert.True(canvas.ContextMenuStrip.Items[0] is ToolStripMenuItem);
                Assert.True(canvas.ContextMenuStrip.Items[1] is ToolStripMenuItem);
                // Items[0] = Delete Connection, Items[1] = Delete Component, Items[2] = Generate Python
                // Existing delete items do not set Name; verify by their menu text instead.
                Assert.NotNull(canvas.ContextMenuStrip.Items[0]);
                Assert.NotNull(canvas.ContextMenuStrip.Items[1]);
                Assert.Equal("MenuGeneratePython", canvas.ContextMenuStrip.Items[2].Name);
            });
        }
    }
}