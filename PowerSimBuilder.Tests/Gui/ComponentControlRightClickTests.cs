using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    internal static class GuiSta
    {
        public static void Run(Action action)
        {
            Exception ex = null;
            var t = new Thread(() =>
            {
                try { action(); }
                catch (Exception e) { ex = e; }
            });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            t.Join();
            if (ex != null) throw new AggregateException("STA thread failed", ex);
        }
    }

    internal static class GuiReflection
    {
        public static void RaiseMouseDown(Control c, MouseButtons b, int x, int y)
        {
            var m = typeof(Control).GetMethod("OnMouseDown",
                BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(c, new object[] { new MouseEventArgs(b, 1, x, y, 0) });
        }

        public static void RaiseMouseMove(Control c, MouseButtons b, int x, int y)
        {
            var m = typeof(Control).GetMethod("OnMouseMove",
                BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(c, new object[] { new MouseEventArgs(b, 0, x, y, 0) });
        }

        public static void RaiseMouseUp(Control c, MouseButtons b, int x, int y)
        {
            var m = typeof(Control).GetMethod("OnMouseUp",
                BindingFlags.Instance | BindingFlags.NonPublic);
            m.Invoke(c, new object[] { new MouseEventArgs(b, 1, x, y, 0) });
        }

        public static object GetField(object o, string name)
        {
            var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            return f == null ? null : f.GetValue(o);
        }

        public static void SetField(object o, string name, object v)
        {
            var f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f != null) f.SetValue(o, v);
        }
    }

    public class ComponentControlRightClickTests
    {
        private static ComponentDescriptor PickDescriptorWithPorts()
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
        public void P1_04_RightClickOnPort_DoesNotRaisePortClicked()
        {
            GuiSta.Run(() =>
            {
                var d = PickDescriptorWithPorts();
                using var c = new ComponentControl(d) { Location = new Point(50, 50) };
                int portFired = 0;
                c.PortClicked += (_, __) => portFired++;
                var pt = InputPortCenter(c, 0);
                GuiReflection.RaiseMouseDown(c, MouseButtons.Right, pt.X, pt.Y);
                Assert.Equal(0, portFired);
            });
        }

        [Fact]
        public void P1_03_RightClickOnBody_DoesNotRaisePortClicked()
        {
            GuiSta.Run(() =>
            {
                var d = PickDescriptorWithPorts();
                using var c = new ComponentControl(d) { Location = new Point(50, 50) };
                int portFired = 0;
                c.PortClicked += (_, __) => portFired++;
                GuiReflection.RaiseMouseDown(c, MouseButtons.Right, c.Width / 2, c.Height / 2);
                Assert.Equal(0, portFired);
            });
        }

        [Fact]
        public void P1_04b_RightClickOnPort_RaisesSelectedChanged()
        {
            GuiSta.Run(() =>
            {
                var d = PickDescriptorWithPorts();
                using var c = new ComponentControl(d) { Location = new Point(50, 50) };
                int selFired = 0;
                c.SelectedChanged += (_, __) => selFired++;
                var pt = InputPortCenter(c, 0);
                GuiReflection.RaiseMouseDown(c, MouseButtons.Right, pt.X, pt.Y);
                Assert.True(selFired >= 1, "v0.7.1 fix: right-click on port must fire SelectedChanged");
            });
        }

        [Fact]
        public void LeftClickOnPort_RaisesPortClicked()
        {
            GuiSta.Run(() =>
            {
                var d = PickDescriptorWithPorts();
                using var c = new ComponentControl(d) { Location = new Point(50, 50) };
                int portFired = 0;
                c.PortClicked += (_, __) => portFired++;
                var pt = InputPortCenter(c, 0);
                GuiReflection.RaiseMouseDown(c, MouseButtons.Left, pt.X, pt.Y);
                Assert.True(portFired >= 1, "Left-click on port must still work");
            });
        }

        [Fact]
        public void LeftClickOnBody_DoesNotRaisePortClicked()
        {
            GuiSta.Run(() =>
            {
                var d = PickDescriptorWithPorts();
                using var c = new ComponentControl(d) { Location = new Point(50, 50) };
                int portFired = 0;
                c.PortClicked += (_, __) => portFired++;
                GuiReflection.RaiseMouseDown(c, MouseButtons.Left, c.Width / 2, c.Height / 2);
                Assert.Equal(0, portFired);
            });
        }
    }
}
