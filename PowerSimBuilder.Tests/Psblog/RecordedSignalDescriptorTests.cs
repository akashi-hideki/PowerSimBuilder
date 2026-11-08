using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Psblog
{
    public class RecordedSignalDescriptorTests
    {
        [Fact]
        public void Descriptor_ExistsInDictionary()
        {
            Assert.True(ComponentDescriptorDictionary.Descriptors.ContainsKey("Recorded Signal"));
        }

        [Fact]
        public void Descriptor_HasExpectedFields()
        {
            var d = ComponentDescriptorDictionary.Descriptors["Recorded Signal"];
            Assert.Equal("RecordedSignal", d.Type);
            Assert.Equal("Source", d.Category);
            Assert.NotNull(d.Parameters);
            Assert.Contains(d.Parameters, p => p.Name == "psblog_path");
            Assert.Contains(d.Parameters, p => p.Name == "var_name");
            Assert.Contains(d.Parameters, p => p.Name == "loop");
            Assert.Contains(d.Parameters, p => p.Name == "hold_last");
            Assert.Contains("value", d.OutputPorts);
        }
    }
}
