using System;
using System.IO;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Manege
{
    /// <summary>
    /// M2-err: GroupManager.Load must reject invalid .psbgroup files.
    /// Covers: missing file, empty file, malformed JSON, invalid schema.
    /// </summary>
    public class GroupManagerLoadErrorTests
    {
        private static string TempPath(string ext = ".psbgroup")
        {
            return Path.Combine(Path.GetTempPath(), "psb_err_" + Guid.NewGuid().ToString("N") + ext);
        }

        [Fact]
        public void Load_MissingFile_ThrowsFileNotFound()
        {
            var missing = Path.Combine(Path.GetTempPath(), "psb_missing_" + Guid.NewGuid().ToString("N") + ".psbgroup");
            Assert.Throws<FileNotFoundException>(() => GroupManager.Load(missing));
        }

        [Fact]
        public void Load_EmptyFile_Throws()
        {
            var p = TempPath();
            try
            {
                File.WriteAllText(p, "");
                Assert.ThrowsAny<Exception>(() => GroupManager.Load(p));
            }
            finally { try { File.Delete(p); } catch { } }
        }

        [Fact]
        public void Load_MalformedJson_Throws()
        {
            var p = TempPath();
            try
            {
                File.WriteAllText(p, "{ not valid json :::");
                Assert.ThrowsAny<Exception>(() => GroupManager.Load(p));
            }
            finally { try { File.Delete(p); } catch { } }
        }

        [Fact]
        public void Load_JsonNull_ThrowsInvalidData()
        {
            var p = TempPath();
            try
            {
                File.WriteAllText(p, "null");
                Assert.Throws<InvalidDataException>(() => GroupManager.Load(p));
            }
            finally { try { File.Delete(p); } catch { } }
        }

        [Fact]
        public void Load_EmptyObject_ReturnsEmptyGroup()
        {
            // An empty JSON object is valid ProjectFileV2 (all defaults).
            // Loading it should succeed but with 0 components.
            var p = TempPath();
            try
            {
                File.WriteAllText(p, "{}");
                var g = GroupManager.Load(p);
                Assert.NotNull(g);
                Assert.Empty(g.Components ?? new System.Collections.Generic.List<ComponentInstanceV2>());
            }
            finally { try { File.Delete(p); } catch { } }
        }

        [Fact]
        public void LoadAndAppend_MalformedJson_Throws()
        {
            var p = TempPath();
            try
            {
                File.WriteAllText(p, "not json at all");
                var mgr = new CanvasManager(new DesignCanvas(), new ComponentPalette());
                Assert.ThrowsAny<Exception>(() =>
                    GroupManager.LoadAndAppend(p, mgr, new System.Drawing.Point(0, 0)));
            }
            finally { try { File.Delete(p); } catch { } }
        }
    }
}
