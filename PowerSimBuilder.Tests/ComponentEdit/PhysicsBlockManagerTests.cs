using System;
using System.Collections.Generic;
using Xunit;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    public class PhysicsBlockManagerTests
    {
        [Fact]
        public void GetInsertedModels_ShouldDetectSingleModel()
        {
            string code = @"
# === physics: PMSM (auto-inserted) ===
self.pmsm = load_fmu('model.fmu')
# === /physics: PMSM ===
";
            var models = PhysicsBlockManager.GetInsertedModels(code);
            Assert.Single(models);
            Assert.Contains("PMSM", models);
        }

        [Fact]
        public void GetInsertedModels_ShouldDetectMultipleModels()
        {
            string code = @"
# === physics: PMSM (auto-inserted) ===
# ...
# === /physics: PMSM ===

# === physics: Dahlquist (auto-inserted in update) ===
# ...
# === /physics: Dahlquist ===
";
            var models = PhysicsBlockManager.GetInsertedModels(code);
            Assert.Equal(2, models.Count);
            Assert.Contains("PMSM", models);
            Assert.Contains("Dahlquist", models);
        }

        [Fact]
        public void GetInsertedModels_ShouldReturnEmpty_WhenNoMarkers()
        {
            string code = "def update(self):\n    pass\n";
            var models = PhysicsBlockManager.GetInsertedModels(code);
            Assert.Empty(models);
        }

        [Fact]
        public void RemovePhysicsBlock_ShouldRemoveTargetBlockOnly()
        {
            string code = @"
# === physics: PMSM (auto-inserted) ===
self.pmsm = 1
# === /physics: PMSM ===

def update(self):
    pass

# === physics: Dahlquist (auto-inserted in update) ===
self.d = 2
# === /physics: Dahlquist ===
";
            string result = PhysicsBlockManager.RemovePhysicsBlock(code, "PMSM");
            Assert.DoesNotContain("physics: PMSM", result);
            Assert.Contains("physics: Dahlquist", result);
            Assert.Contains("def update(self):", result);
        }
    }
}
