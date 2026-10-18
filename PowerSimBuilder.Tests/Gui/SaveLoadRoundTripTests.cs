using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.Json;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Save/Load round-trip tests for CanvasModel JSON serialization.
    /// These use the same JsonSerializerOptions as MainForm.saveButton_Click
    /// and MainForm.loadButton_Click, so they verify the actual on-disk format.
    /// </summary>
    public class SaveLoadRoundTripTests
    {
        // Same as MainForm.saveButton_Click (L331-336)
        private static readonly JsonSerializerOptions SaveOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new PointJsonConverter() }
        };

        // Same as MainForm.loadButton_Click (L368-372)
        private static readonly JsonSerializerOptions LoadOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new PointJsonConverter() }
        };

        private static ComponentDescriptor MakeDescriptor(string name = "TestComp", string type = "Gain")
        {
            return new ComponentDescriptor
            {
                Name = name,
                Type = type,
                Category = "Control",
                Parameters = new List<ParameterDescriptor>(),
                InputPorts = new[] { "in" },
                OutputPorts = new[] { "out" }
            };
        }

        private static CanvasModel RoundTrip(CanvasModel model)
        {
            var json = JsonSerializer.Serialize(model, SaveOptions);
            var restored = JsonSerializer.Deserialize<CanvasModel>(json, LoadOptions);
            Assert.NotNull(restored);
            return restored;
        }

        [Fact]
        public void RoundTrip_EmptyCanvas_IsEmpty()
        {
            var model = new CanvasModel();
            var restored = RoundTrip(model);
            Assert.Empty(restored.Components);
            Assert.Empty(restored.Connections);
            Assert.Equal(-1, restored.SelectedComponentIndex);
            Assert.Equal(-1, restored.SelectedConnectionIndex);
        }

        [Fact]
        public void RoundTrip_MultipleComponents_PreservesPositions()
        {
            var model = new CanvasModel();
            model.Components.Add(new ComponentModel
            {
                Descriptor = MakeDescriptor("A", "Gain"),
                InstanceName = "a1",
                Location = new Point(100, 100),
                Parameters = new Dictionary<string, object>()
            });
            model.Components.Add(new ComponentModel
            {
                Descriptor = MakeDescriptor("B", "Sum"),
                InstanceName = "b1",
                Location = new Point(300, 50),
                Parameters = new Dictionary<string, object>()
            });

            var restored = RoundTrip(model);
            Assert.Equal(2, restored.Components.Count);
            Assert.Equal("a1", restored.Components[0].InstanceName);
            Assert.Equal("b1", restored.Components[1].InstanceName);
            Assert.Equal(new Point(100, 100), restored.Components[0].Location);
            Assert.Equal(new Point(300, 50), restored.Components[1].Location);
            Assert.Equal("Gain", restored.Components[0].Descriptor.Type);
            Assert.Equal("Sum", restored.Components[1].Descriptor.Type);
        }

        [Fact]
        public void RoundTrip_WithConnection_PreservesEndpoints()
        {
            var model = new CanvasModel();
            model.Components.Add(new ComponentModel
            {
                Descriptor = MakeDescriptor("A", "Gain"),
                InstanceName = "a1",
                Location = new Point(0, 0),
                Parameters = new Dictionary<string, object>()
            });
            model.Components.Add(new ComponentModel
            {
                Descriptor = MakeDescriptor("B", "Sum"),
                InstanceName = "b1",
                Location = new Point(100, 0),
                Parameters = new Dictionary<string, object>()
            });
            model.Connections.Add(new ConnectionModel
            {
                StartComponentIndex = 0,
                StartPort = "out",
                EndComponentIndex = 1,
                EndPort = "in"
            });

            var restored = RoundTrip(model);
            Assert.Single(restored.Connections);
            Assert.Equal(0, restored.Connections[0].StartComponentIndex);
            Assert.Equal("out", restored.Connections[0].StartPort);
            Assert.Equal(1, restored.Connections[0].EndComponentIndex);
            Assert.Equal("in", restored.Connections[0].EndPort);
        }

        [Fact]
        public void RoundTrip_PreservesSelectionIndices()
        {
            var model = new CanvasModel
            {
                SelectedComponentIndex = 2,
                SelectedConnectionIndex = 0
            };
            var restored = RoundTrip(model);
            Assert.Equal(2, restored.SelectedComponentIndex);
            Assert.Equal(0, restored.SelectedConnectionIndex);
        }

        [Fact]
        public void RoundTrip_PreservesNumericParameters()
        {
            // Note: Dictionary<string, object> values become JsonElement after
            // JSON deserialization (System.Text.Json limitation).
            var model = new CanvasModel();
            model.Components.Add(new ComponentModel
            {
                Descriptor = MakeDescriptor(),
                InstanceName = "c1",
                Location = new Point(0, 0),
                Parameters = new Dictionary<string, object>
                {
                    { "gain", 10.5 },
                    { "count", 3 },
                    { "enabled", true }
                }
            });

            var restored = RoundTrip(model);
            var p = restored.Components[0].Parameters;
            Assert.NotNull(p);
            Assert.Equal(10.5, ((JsonElement)p["gain"]).GetDouble());
            Assert.Equal(3, ((JsonElement)p["count"]).GetInt32());
            Assert.True(((JsonElement)p["enabled"]).GetBoolean());
        }

        [Fact]
        public void RoundTrip_PreservesStringParameters()
        {
            var model = new CanvasModel();
            model.Components.Add(new ComponentModel
            {
                Descriptor = MakeDescriptor(),
                InstanceName = "c1",
                Location = new Point(0, 0),
                Parameters = new Dictionary<string, object>
                {
                    { "label", "hello" }
                }
            });

            var restored = RoundTrip(model);
            var p = restored.Components[0].Parameters;
            Assert.Equal("hello", ((JsonElement)p["label"]).GetString());
        }

        [Fact]
        public void Serialize_UsesCamelCasePropertyNames()
        {
            var model = new CanvasModel();
            var json = JsonSerializer.Serialize(model, SaveOptions);
            Assert.Contains("\"components\"", json);
            Assert.Contains("\"connections\"", json);
            Assert.Contains("\"selectedComponentIndex\"", json);
            Assert.Contains("\"selectedConnectionIndex\"", json);
        }

        [Fact]
        public void Deserialize_EmptyString_Throws()
        {
            Assert.ThrowsAny<JsonException>(() =>
                JsonSerializer.Deserialize<CanvasModel>("", LoadOptions));
        }

        [Fact]
        public void Deserialize_InvalidJson_Throws()
        {
            Assert.ThrowsAny<JsonException>(() =>
                JsonSerializer.Deserialize<CanvasModel>("{ not valid json", LoadOptions));
        }

        [Fact]
        public void Deserialize_MissingFields_UsesDefaults()
        {
            var restored = JsonSerializer.Deserialize<CanvasModel>("{}", LoadOptions);
            Assert.NotNull(restored);
            Assert.Equal(-1, restored.SelectedComponentIndex);
            Assert.Equal(-1, restored.SelectedConnectionIndex);
        }

        [Fact]
        public void RoundTrip_RealDescriptorFromDictionary_Succeeds()
        {
            // Use a real descriptor from the app (proves the production data serializes)
            var realDescriptor = ComponentDescriptorDictionary.Descriptors.Values
                .FirstOrDefault(d => d.InputPorts != null && d.InputPorts.Length > 0
                                  && d.OutputPorts != null && d.OutputPorts.Length > 0);
            Assert.NotNull(realDescriptor);

            var model = new CanvasModel();
            model.Components.Add(new ComponentModel
            {
                Descriptor = realDescriptor,
                InstanceName = "real1",
                Location = new Point(42, 42),
                Parameters = new Dictionary<string, object>()
            });

            var restored = RoundTrip(model);
            Assert.Single(restored.Components);
            Assert.Equal("real1", restored.Components[0].InstanceName);
            Assert.Equal(realDescriptor.Type, restored.Components[0].Descriptor.Type);
            Assert.Equal(new Point(42, 42), restored.Components[0].Location);
        }
    }
}
