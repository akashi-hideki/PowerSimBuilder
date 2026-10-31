using System.Text.Json;
using Xunit;

namespace PowerSimBuilder.Tests.CanvasForm
{
    public class ProjectFileV2MetadataTests
    {
        [Fact]
        public void Metadata_Defaults_Are_5s_And_1ms()
        {
            var v2 = new ProjectFileV2();
            Assert.Equal(5.0, v2.Metadata.TEnd);
            Assert.Equal(0.001, v2.Metadata.Dt);
        }

        [Fact]
        public void Metadata_TEnd_Serializes_As_CamelCase()
        {
            var v2 = new ProjectFileV2 { Metadata = new ProjectMetadata { TEnd = 300.0 } };
            var json = JsonSerializer.Serialize(v2);
            Assert.Contains("\"tEnd\":300", json);
            Assert.Contains("\"dt\":0.001", json);
        }

        [Theory]
        [InlineData(300.0)]
        [InlineData(5.0)]
        [InlineData(0.5)]
        [InlineData(3600.0)]
        public void Metadata_TEnd_RoundTrips(double tEnd)
        {
            var v2 = new ProjectFileV2 { Metadata = new ProjectMetadata { TEnd = tEnd } };
            var json = JsonSerializer.Serialize(v2);
            var back = JsonSerializer.Deserialize<ProjectFileV2>(json);
            Assert.NotNull(back);
            Assert.Equal(tEnd, back!.Metadata.TEnd);
        }

        [Fact]
        public void Metadata_Missing_TEnd_Uses_Default_5()
        {
            const string legacy = "{\"schemaVersion\":\"2.0\",\"metadata\":{\"name\":\"x\"},\"components\":[],\"connections\":[]}";
            var v2 = JsonSerializer.Deserialize<ProjectFileV2>(legacy);
            Assert.NotNull(v2);
            Assert.Equal(5.0, v2!.Metadata.TEnd);
        }
    }
}