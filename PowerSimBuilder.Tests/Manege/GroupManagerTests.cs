using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Manege
{
    /// <summary>
    /// Phase 3: GroupManager.Extract / Save / Load round-trip.
    /// </summary>
    public class GroupManagerTests
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
        public void Extract_EmptySelection_Throws()
        {
            Assert.Throws<InvalidOperationException>(() =>
                GroupManager.Extract("g", new ComponentControl[0], new Connection[0]));
        }

        [Fact]
        public void Extract_RelativePositions()
        {
            var a = MakeControl("a", 100, 100);
            var b = MakeControl("b", 300, 200);
            var project = GroupManager.Extract("g", new[] { a, b }, new Connection[0]);
            Assert.Equal(2, project.Components.Count);
            var ca = project.Components.First(c => c.InstanceName == "a");
            var cb = project.Components.First(c => c.InstanceName == "b");
            Assert.Equal(0, ca.Location.X);
            Assert.Equal(0, ca.Location.Y);
            Assert.Equal(200, cb.Location.X);
            Assert.Equal(100, cb.Location.Y);
        }

        [Fact]
        public void Extract_OnlyInternalConnections()
        {
            var a = MakeControl("a", 0, 0);
            var b = MakeControl("b", 100, 0);
            var c = MakeControl("c", 200, 0);
            var conn1 = new Connection { StartComponent = a, StartPort = "output", EndComponent = b, EndPort = "input" };
            var conn2 = new Connection { StartComponent = b, StartPort = "output", EndComponent = c, EndPort = "input" };
            var project = GroupManager.Extract("g", new[] { a, b }, new[] { conn1, conn2 });
            Assert.Single(project.Connections);
            Assert.Equal("a.output", project.Connections[0].From);
            Assert.Equal("b.input", project.Connections[0].To);
        }

        [Fact]
        public void Save_Load_RoundTrip()
        {
            var a = MakeControl("a", 100, 100);
            var b = MakeControl("b", 300, 200);
            var project = GroupManager.Extract("g", new[] { a, b }, new Connection[0]);
            var tmp = Path.Combine(Path.GetTempPath(), "psb_group_" + Guid.NewGuid().ToString("N") + ".psbgroup");
            try
            {
                GroupManager.Save(tmp, project);
                var loaded = GroupManager.Load(tmp);
                Assert.Equal("g", loaded.Metadata.Name);
                Assert.Equal(2, loaded.Components.Count);
                Assert.Equal(0, loaded.Components.First(c => c.InstanceName == "a").Location.X);
                Assert.Equal(200, loaded.Components.First(c => c.InstanceName == "b").Location.X);
            }
            finally { try { File.Delete(tmp); } catch { } }
        }
    }
}
