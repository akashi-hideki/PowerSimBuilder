using System;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Manege
{
    public class RoleSnippetStorageTests
    {
        private static string TempDir()
        {
            var dir = Path.Combine(Path.GetTempPath(), "psb_rolesnippet_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        [Fact]
        public void LoadAll_EmptyFolder_ReturnsEmptyList()
        {
            var dir = TempDir();
            try
            {
                var list = RoleSnippetStorage.LoadAll(dir);
                Assert.NotNull(list);
                Assert.Empty(list);
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public void LoadAll_RepoDefault_ReturnsSampleSnippets()
        {
            var list = RoleSnippetStorage.LoadAll();
            Assert.NotEmpty(list);
            Assert.Contains(list, s => s.Layer == "MwOven" && s.Func == "F_ReadThermistors");
            Assert.Contains(list, s => s.Layer == "MwOven" && s.Func == "F_SetTopHeaterDuty");
            Assert.Contains(list, s => s.Layer == "MwOven" && s.Func == "F_CheckOvenOverheat");
        }

        [Fact]
        public void LoadCode_DefaultSnippet_ContainsExpectedPython()
        {
            var list = RoleSnippetStorage.LoadAll();
            var snip = list.First(s => s.Layer == "MwOven" && s.Func == "F_ReadThermistors");
            var code = RoleSnippetStorage.LoadCode(snip);
            Assert.Contains("thermistor_top", code);
            Assert.Contains("return 0", code);
        }

        [Fact]
        public void Save_Then_Load_RoundTrip()
        {
            var dir = TempDir();
            try
            {
                var snippet = new RoleSnippet
                {
                    Layer = "MwOven",
                    Func = "F_Test",
                    DisplayName = "Test",
                    Description = "Test snippet",
                };
                RoleSnippetStorage.Save(snippet, "x = 1\nreturn 0\n", dir);
                var list = RoleSnippetStorage.LoadAll(dir);
                Assert.Single(list);
                Assert.Equal("MwOven", list[0].Layer);
                Assert.Equal("MwOven.F_Test.py", list[0].File);
                Assert.Contains("x = 1", RoleSnippetStorage.LoadCode(list[0], dir));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public void Save_OverwritesExistingEntry()
        {
            var dir = TempDir();
            try
            {
                RoleSnippetStorage.Save(new RoleSnippet { Layer = "L", Func = "F", DisplayName = "v1" }, "return 0\n", dir);
                RoleSnippetStorage.Save(new RoleSnippet { Layer = "L", Func = "F", DisplayName = "v2" }, "return 1\n", dir);
                var list = RoleSnippetStorage.LoadAll(dir);
                Assert.Single(list);
                Assert.Equal("v2", list[0].DisplayName);
                Assert.Contains("return 1", RoleSnippetStorage.LoadCode(list[0], dir));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }

        [Fact]
        public void Delete_RemovesFromIndexAndDisk()
        {
            var dir = TempDir();
            try
            {
                var s = new RoleSnippet { Layer = "L", Func = "F", DisplayName = "x" };
                RoleSnippetStorage.Save(s, "return 0\n", dir);
                Assert.Single(RoleSnippetStorage.LoadAll(dir));
                RoleSnippetStorage.Delete(s, dir);
                Assert.Empty(RoleSnippetStorage.LoadAll(dir));
                Assert.False(File.Exists(Path.Combine(dir, "L.F.py")));
            }
            finally { try { Directory.Delete(dir, true); } catch { } }
        }
    }
}
