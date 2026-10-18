using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    public class DesignCanvasConnectionTests
    {
        private static ComponentDescriptor PickDescriptor()
        {
            // Prefer a descriptor with distinct input/output port names at index 0,
            // so direction tests are unambiguous.
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

        private static void ClickPort(ComponentControl comp, Point pt, MouseButtons button = MouseButtons.Left)
        {
            GuiReflection.RaiseMouseDown(comp, button, pt.X, pt.Y);
        }

        // P3-01: 基本の Output -> Input 接続成立
        [Fact]
        public void P3_01_OutputToInput_CreatesConnection()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                Assert.NotNull(GuiReflection.GetField(canvas, "_currentConnection"));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Null(GuiReflection.GetField(canvas, "_currentConnection"));
                Assert.Single(canvas.Connections);
            });
        }

        // P3-04: 同じペアを 2 回接続 -> 重複拒否（ConnectionExists で弾かれる）
        [Fact]
        public void P3_04_DuplicateConnection_NotAddedTwice()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Single(canvas.Connections);
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Single(canvas.Connections);
            });
        }

        // P3-05: 自己接続（同一コンポーネント out -> in）は拒否されるべき
        // 現状のコードには自己接続チェックが見当たらないため、Bug が検出される可能性
        [Fact]
        public void P3_05_SelfConnection_NotAdded()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c1, InputPortCenter(c1, 0));
                Assert.Empty(canvas.Connections);
            });
        }

        // P3-06: Output -> Output は不正方向。拒否されるべき
        [Fact]
        public void P3_06_OutputToOutput_NotConnected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, OutputPortCenter(c1, 0));
                ClickPort(c2, OutputPortCenter(c2, 0));
                Assert.Empty(canvas.Connections);
            });
        }

        // P3-07: Input -> Input は不正方向。拒否されるべき
        [Fact]
        public void P3_07_InputToInput_NotConnected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, InputPortCenter(c1, 0));
                ClickPort(c2, InputPortCenter(c2, 0));
                Assert.Empty(canvas.Connections);
            });
        }

        // P3-10: 左クリック 1 回で _currentConnection が 1 回だけセットされる（二重発火の検出）
        [Fact]
        public void P3_10_LeftClickOnPort_SetsPendingConnection_NoDoubleFire()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 100, 100);
                ClickPort(c1, OutputPortCenter(c1, 0));
                // 接続はまだ追加されていない（自己接続で即完成していないか確認）
                Assert.Empty(canvas.Connections);
                var pending = GuiReflection.GetField(canvas, "_currentConnection") as Connection;
                Assert.NotNull(pending);
                Assert.Same(c1, pending.StartComponent);
                Assert.Equal(c1.Descriptor.OutputPorts[0], pending.StartPort);
            });
        }

        // P3-11: 逆向き（Input -> Output）は別接続として扱われるべき（現状挙動の確認）
        [Fact]
        public void P3_11_ReverseDirection_NotConnected()
        {
            GuiSta.Run(() =>
            {
                using var canvas = new DesignCanvas();
                var c1 = AddComp(canvas, 50, 50);
                var c2 = AddComp(canvas, 300, 50);
                ClickPort(c1, InputPortCenter(c1, 0));
                ClickPort(c2, OutputPortCenter(c2, 0));
                Assert.Empty(canvas.Connections);
            });
        }
    }
}
