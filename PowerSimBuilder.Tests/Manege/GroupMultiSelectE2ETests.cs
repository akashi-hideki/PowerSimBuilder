using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Manege
{
    /// <summary>
    /// A1: end-to-end group save/load through the multi-select API.
    /// Mirrors the GUI flow (select -> Save Group -> Load Group -> append).
    /// </summary>
    public class GroupMultiSelectE2ETests
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
                Location = new System.Drawing.Point(x, y),
            };
        }

        [Fact]
        public void MultiSelect_SaveAndLoad_AppendsSameGraph()
        {
            var srcCanvas = new DesignCanvas();
            var a = MakeControl("a", 100, 100);
            var b = MakeControl("b", 300, 100);
            var c = MakeControl("c", 500, 100);
            srcCanvas.AddComponent(a);
            srcCanvas.AddComponent(b);
            srcCanvas.AddComponent(c);
            var conn1 = new Connection { StartComponent = a, StartPort = "output", EndComponent = b, EndPort = "input" };
            var conn2 = new Connection { StartComponent = b, StartPort = "output", EndComponent = c, EndPort = "input" };
            srcCanvas.AddConnection(conn1);
            srcCanvas.AddConnection(conn2);

            // Select all three (simulating Ctrl+click)
            srcCanvas.SelectComponent(a);
            srcCanvas.SelectComponent(b, additive: true);
            srcCanvas.SelectComponent(c, additive: true);
            Assert.Equal(3, srcCanvas.SelectedComponents.Count);

            // Save group
            var group = GroupManager.Extract("g3", srcCanvas.SelectedComponents, srcCanvas.Connections);
            Assert.Equal(3, group.Components.Count);
            Assert.Equal(2, group.Connections.Count);

            var tmp = Path.Combine(Path.GetTempPath(), "psb_a1_" + Guid.NewGuid().ToString("N") + ".psbgroup");
            try
            {
                GroupManager.Save(tmp, group);

                // Load into a fresh canvas via CanvasManager (GUI path)
                var dstCanvas = new DesignCanvas();
                var dstMgr = new CanvasManager(dstCanvas, new ComponentPalette());
                var (comps, conns) = GroupManager.LoadAndAppend(tmp, dstMgr, new System.Drawing.Point(10, 10));

                Assert.Equal(3, comps);
                Assert.Equal(2, conns);
                Assert.Equal(3, dstMgr.GetAllComponents().Count);
                Assert.Equal(2, dstMgr.GetAllConnections().Count);

                // Connections must re-wire to the NEW instances (not the source ones)
                var newConns = dstMgr.GetAllConnections();
                var srcSet = new HashSet<ComponentControl> { a, b, c };
                foreach (var nc in newConns)
                {
                    Assert.DoesNotContain(nc.StartComponent, srcSet);
                    Assert.DoesNotContain(nc.EndComponent, srcSet);
                }
            }
            finally { try { File.Delete(tmp); } catch { } }
        }
    }
}
