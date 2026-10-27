using System;
using System.IO;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.CanvasForm
{
    public class FmuLicenseDetectorTests
    {
        [Fact]
        public void Dahlquist_DetectedAsBsd2Redistributable()
        {
            var root = Path.Combine(Path.GetTempPath(),
                "psb_lic_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Path.Combine(root, "Dahlquist", "source"));
            try
            {
                var info = FmuLicenseDetector.Detect(root, "Dahlquist/source/Dahlquist.fmu");
                Assert.Equal("BSD-2-Clause", info.LicenseId);
                Assert.Equal(FmuLicenseClassification.Redistributable, info.Classification);
                Assert.Contains("Modelica Association", info.CopyrightText);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void UnknownModel_ClassifiedAsUnknown()
        {
            var root = Path.Combine(Path.GetTempPath(),
                "psb_lic_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(Path.Combine(root, "MysteryModel", "source"));
            try
            {
                var info = FmuLicenseDetector.Detect(root, "MysteryModel/source/MysteryModel.fmu");
                Assert.Equal("Unknown", info.LicenseId);
                Assert.Equal(FmuLicenseClassification.Unknown, info.Classification);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void LicenseFile_MitRecognized()
        {
            var root = Path.Combine(Path.GetTempPath(),
                "psb_lic_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var modelDir = Path.Combine(root, "MyModel");
            Directory.CreateDirectory(Path.Combine(modelDir, "source"));
            try
            {
                File.WriteAllText(Path.Combine(modelDir, "LICENSE"),
                    "MIT License\n\nCopyright (c) 2024 Test Author\n\nPermission is hereby granted...");
                var info = FmuLicenseDetector.Detect(root, "MyModel/source/MyModel.fmu");
                Assert.Equal("MIT", info.LicenseId);
                Assert.Equal(FmuLicenseClassification.Redistributable, info.Classification);
                Assert.Contains("Copyright", info.CopyrightText);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }

        [Fact]
        public void LicenseFile_CommercialDetectedAsNonRedistributable()
        {
            var root = Path.Combine(Path.GetTempPath(),
                "psb_lic_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            var modelDir = Path.Combine(root, "CommercialModel");
            Directory.CreateDirectory(Path.Combine(modelDir, "source"));
            try
            {
                File.WriteAllText(Path.Combine(modelDir, "model.yaml"),
                    "name: CommercialModel\nlicense: Commercial\n");
                var info = FmuLicenseDetector.Detect(root, "CommercialModel/source/CommercialModel.fmu");
                Assert.Equal("Commercial", info.LicenseId);
                Assert.Equal(FmuLicenseClassification.NonRedistributable, info.Classification);
            }
            finally { try { Directory.Delete(root, true); } catch { } }
        }
    }
}
