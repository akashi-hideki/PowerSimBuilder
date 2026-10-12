using System.Drawing;
using System.Text.Json;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Core
{
    public class PointJsonConverterTests
    {
        private readonly JsonSerializerOptions _options;

        public PointJsonConverterTests()
        {
            _options = new JsonSerializerOptions
            {
                Converters = { new PointJsonConverter() }
            };
        }

        [Fact]
        public void Serialize_Point_WritesXY()
        {
            var point = new Point(10, 20);
            string json = JsonSerializer.Serialize(point, _options);
            Assert.Contains("\"X\":10", json);
            Assert.Contains("\"Y\":20", json);
        }

        [Fact]
        public void Deserialize_ValidJson_ReturnsPoint()
        {
            string json = "{\"X\":30,\"Y\":40}";
            var point = JsonSerializer.Deserialize<Point>(json, _options);
            Assert.Equal(30, point.X);
            Assert.Equal(40, point.Y);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(-5, 10)]
        [InlineData(100, -100)]
        public void RoundTrip_PreservesValues(int x, int y)
        {
            var original = new Point(x, y);
            string json = JsonSerializer.Serialize(original, _options);
            var restored = JsonSerializer.Deserialize<Point>(json, _options);
            Assert.Equal(original.X, restored.X);
            Assert.Equal(original.Y, restored.Y);
        }

        [Fact]
        public void Deserialize_MissingProperties_DefaultsToZero()
        {
            string json = "{}";
            var point = JsonSerializer.Deserialize<Point>(json, _options);
            Assert.Equal(0, point.X);
            Assert.Equal(0, point.Y);
        }
    }
}
