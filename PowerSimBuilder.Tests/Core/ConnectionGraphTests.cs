using System.Collections.Generic;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Core
{
    public class ConnectionGraphTests
    {
        private ComponentControl MakeComponent(string name)
        {
            return new ComponentControl(new ComponentDescriptor
            {
                Name = name,
                Type = "Test",
                Parameters = new List<ParameterDescriptor>(),
                InputPorts = new[] { "in1" },
                OutputPorts = new[] { "out1" }
            })
            { InstanceName = name };
        }

        [Fact]
        public void AddConnection_IncreasesCount()
        {
            var graph = new ConnectionGraph();
            var a = MakeComponent("A");
            var b = MakeComponent("B");

            var conn = new Connection
            {
                StartComponent = a,
                StartPort = "out1",
                EndComponent = b,
                EndPort = "in1"
            };

            graph.AddConnection(conn);

            Assert.Single(graph.GetConnectionsFrom(a));
            Assert.Single(graph.GetConnectionsTo(b));
        }

        [Fact]
        public void RemoveConnection_ClearsFromBothDirections()
        {
            var graph = new ConnectionGraph();
            var a = MakeComponent("A");
            var b = MakeComponent("B");

            var conn = new Connection
            {
                StartComponent = a,
                StartPort = "out1",
                EndComponent = b,
                EndPort = "in1"
            };

            graph.AddConnection(conn);
            graph.RemoveConnection(conn);

            Assert.Empty(graph.GetConnectionsFrom(a));
            Assert.Empty(graph.GetConnectionsTo(b));
        }

        [Fact]
        public void ConnectionExists_ReturnsTrueForDuplicate()
        {
            var graph = new ConnectionGraph();
            var a = MakeComponent("A");
            var b = MakeComponent("B");

            var conn = new Connection
            {
                StartComponent = a,
                StartPort = "out1",
                EndComponent = b,
                EndPort = "in1"
            };

            graph.AddConnection(conn);

            Assert.True(graph.ConnectionExists(a, "out1", b, "in1"));
            Assert.False(graph.ConnectionExists(a, "out1", b, "in999"));
        }
    }
}
