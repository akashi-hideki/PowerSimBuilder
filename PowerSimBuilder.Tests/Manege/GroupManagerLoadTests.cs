using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Manege
{
    /// <summary>
    /// Phase 4: GroupManager.LoadAndAppend round-trip.
    /// </summary>
    public class GroupManagerLoadTests
    {
        private static ComponentDescriptor MakeDescriptor(string name)
        {
            return new ComponentDescriptor
            {
                Name = name,
                Type = "FirstOrderLag",
                Category = "Simulation",
                InputPorts = new[] { "input" },
                OutputPorts = new[] { "output" },
                Parameters = new List<ParameterDescriptor>(),
            };
        }

        private static ComponentControl MakeControl(string name, int x, int y)
        {
            return new ComponentControl(MakeDescriptor(name))
            {
                InstanceName = name,
                Location = new System.Drawing.Point(x, y),
            };
        }

        [Fact]
        public void LoadAndAppend_AddsComponentsAndConnections()
        {
            var manager = new CanvasManager(new DesignCanvas(), new ComponentPalette());
            var a = MakeControl("a", 100, 100);
            var b = MakeControl("b", 300, 200);
            var conn = new Connection { StartComponent = a, StartPort = "output", EndComponent = b, EndPort = "input" };
            var project = GroupManager.Extract("g", new[] { a, b }, new[] { conn });
            var tmp = Path.Combine(Path.GetTempPath(), "psb_gla_" + Guid.NewGuid().ToString("N") + ".psbgroup");
            try
            {
                GroupManager.Save(tmp, project);
                var (comps, conns) = GroupManager.LoadAndAppend(tmp, manager, new System.Drawing.Point(10, 20));
                Assert.Equal(2, comps);
                Assert.Equal(1, conns);
                var all = manager.GetAllComponents();
                Assert.Equal(2, all.Count);
                // a(100,100) is normalized to (0,0) by Extract (minX=100, minY=100),
                // then offset (10,20) is applied → (10,20).
                var a2 = all.First(c => c.InstanceName == "a");
                Assert.Equal(10, a2.Location.X);
                Assert.Equal(20, a2.Location.Y);
                // b(300,200) → (200,100) relative → +offset = (210,120)
                var b2 = all.First(c => c.InstanceName == "b");
                Assert.Equal(210, b2.Location.X);
                Assert.Equal(120, b2.Location.Y);
                Assert.Single(manager.GetAllConnections());
            }
            finally { try { File.Delete(tmp); } catch { } }
        }

        [Fact]
        public void LoadAndAppend_DuplicateNames_GetSuffix()
        {
            var manager = new CanvasManager(new DesignCanvas(), new ComponentPalette());
            manager.AddComponent(MakeDescriptor("a"), new System.Drawing.Point(0, 0));
            manager.GetAllComponents()[0].InstanceName = "a";

            var src = MakeControl("a", 0, 0);
            var project = GroupManager.Extract("g", new[] { src }, new Connection[0]);
            var tmp = Path.Combine(Path.GetTempPath(), "psb_gla_" + Guid.NewGuid().ToString("N") + ".psbgroup");
            try
            {
                GroupManager.Save(tmp, project);
                GroupManager.LoadAndAppend(tmp, manager, new System.Drawing.Point(0, 0));
                var names = manager.GetAllComponents().Select(c => c.InstanceName).ToList();
                Assert.Contains("a", names);
                Assert.Contains("a_2", names);
            }
            finally { try { File.Delete(tmp); } catch { } }
        }
    }
}
