using System.Collections.Generic;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Psblog
{
    public class PsblogImportFormTests
    {
        [Fact]
        public void PsblogImportResult_DefaultsAreEmpty()
        {
            var r = new PsblogImportResult();
            Assert.Equal("", r.PsblogPath);
            Assert.Empty(r.SelectedVariables);
            Assert.False(r.PlaceInferredComponents);
            Assert.Empty(r.ProposedComponents);
        }

        [Fact]
        public void PsblogImportResult_WithSelectedVariables()
        {
            var r = new PsblogImportResult
            {
                PsblogPath = "/x.psblog",
                SelectedVariables = new[] { "oven_current_temp", "door_open" },
                PlaceInferredComponents = true,
                ProposedComponents = new Dictionary<string, string?>
                {
                    ["oven_current_temp"] = "ThermalMass",
                    ["door_open"] = "DigitalInput",
                },
            };
            Assert.Equal("/x.psblog", r.PsblogPath);
            Assert.Equal(2, r.SelectedVariables.Count);
            Assert.True(r.PlaceInferredComponents);
            Assert.Equal("ThermalMass", r.ProposedComponents["oven_current_temp"]);
        }
    }
}
