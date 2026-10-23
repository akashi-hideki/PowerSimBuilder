#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using PowerSimBuilder;
using PowerSimBuilder.Physics;
using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit;

public class PythonEditorFormPhysicsTests
{
    private const string Template =
        "class Foo:\n" +
        "    def __init__(self):\n" +
        "        self.x = 0\n" +
        "\n" +
        "    def update(self, t, dt):\n" +
        "        pass\n";

    private sealed class TempDir : IDisposable
    {
        public string Path { get; }
        public TempDir()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "psb_editor_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }
        public void Dispose()
        {
            try { Directory.Delete(Path, true); } catch { }
        }
    }

    private static string CreateMinimalFmu(string path, string modelName)
    {
        var dir = System.IO.Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(dir);
        var xml =
            "<fmiModelDescription fmiVersion=\"2.0\" fmiType=\"CoSimulation\" modelName=\"" + modelName + "\">\n" +
            "  <ModelVariables>\n" +
            "    <ScalarVariable name=\"u\" valueReference=\"1\" causality=\"input\"><Real start=\"0\"/></ScalarVariable>\n" +
            "    <ScalarVariable name=\"y\" valueReference=\"2\" causality=\"output\"><Real/></ScalarVariable>\n" +
            "  </ModelVariables>\n" +
            "</fmiModelDescription>\n";
        if (File.Exists(path)) File.Delete(path);
        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("modelDescription.xml");
            using var es = entry.Open();
            var bytes = Encoding.UTF8.GetBytes(xml);
            es.Write(bytes, 0, bytes.Length);
        }
        return path;
    }

    private static PhysicsModelEntry MakeEntry(string name, string tempDir)
    {
        var modelDir = System.IO.Path.Combine(tempDir, name);
        var fmuPath = System.IO.Path.Combine(modelDir, "source", "model.fmu");
        CreateMinimalFmu(fmuPath, name);
        var info = FmuInfoReader.Read(fmuPath);
        return new PhysicsModelEntry(
            name,
            modelDir,
            fmuPath,
            name + "/source/model.fmu",
            info);
    }

    private static void RunOnSta(Action action)
    {
        Exception? captured = null;
        var t = new Thread(() =>
        {
            try { action(); }
            catch (Exception ex) { captured = ex; }
        });
        t.SetApartmentState(ApartmentState.STA);
        t.IsBackground = true;
        t.Start();
        t.Join(TimeSpan.FromSeconds(30));
        if (captured != null) throw captured;
    }

    private sealed class MockPicker : IPhysicsPicker
    {
        private readonly PhysicsModelEntry? _toReturn;
        public int CallCount { get; private set; }
        public MockPicker(PhysicsModelEntry? e) { _toReturn = e; }
        public PhysicsModelEntry? Pick(IWin32Window? owner)
        {
            CallCount++;
            return _toReturn;
        }
    }

    [Fact]
    public void SetPythonCode_UpdatesEditor()
    {
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            form.SetPythonCode("print('hi')");
            Assert.Equal("print('hi')", form.Test_TextEditor.Text);
        });
    }

    [Fact]
    public void OnPhysicsRequested_WithMockPicker_InsertsCode()
    {
        using var tmp = new TempDir();
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            form.SetPythonCode(Template);
            var mock = new MockPicker(MakeEntry("PMSM", tmp.Path));
            form.PickerProvider = mock;

            form.Test_RaisePhysicsRequested();

            Assert.Equal(1, mock.CallCount);
            var text = form.Test_TextEditor.Text;
            Assert.Contains("# === physics: PMSM (auto-inserted) ===", text);
            Assert.Contains("self.pmsm = load_fmu(", text);
            Assert.Contains("# === physics: PMSM (auto-inserted in update) ===", text);
            Assert.Contains("self.pmsm.doStep(self._pmsm_time, dt)", text);
        });
    }

    [Fact]
    public void OnPhysicsRequested_NullPickerResult_NoChange()
    {
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            form.SetPythonCode(Template);
            var mock = new MockPicker(null);
            form.PickerProvider = mock;

            form.Test_RaisePhysicsRequested();

            Assert.Equal(1, mock.CallCount);
            Assert.Equal(Template, form.Test_TextEditor.Text);
            Assert.DoesNotContain("=== physics:", form.Test_TextEditor.Text);
        });
    }

    [Fact]
    public void OnPhysicsRequested_PopulatesCache()
    {
        using var tmp = new TempDir();
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            form.SetPythonCode(Template);
            form.PickerProvider = new MockPicker(MakeEntry("PMSM", tmp.Path));

            form.Test_RaisePhysicsRequested();

            Assert.True(form.Test_PhysicsCache.ContainsKey("PMSM"));
            Assert.Single(form.Test_PhysicsCache["PMSM"].Inputs);
        });
    }

    [Fact]
    public void OnPhysicsRequested_InsertsExactlyOnce()
    {
        using var tmp = new TempDir();
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            form.SetPythonCode(Template);
            form.PickerProvider = new MockPicker(MakeEntry("PMSM", tmp.Path));
            form.Test_RaisePhysicsRequested();

            var afterFirst = form.Test_TextEditor.Text;
            Assert.Equal(1, CountOccurrences(afterFirst, "# === physics: PMSM (auto-inserted) ==="));
        });
    }

    [Fact]
    public void SelectionChanged_InsideBlock_ShowsPhysicsInStatus()
    {
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            const string code =
                "class Foo:\n" +
                "    def __init__(self):\n" +
                "        # === physics: PMSM (auto-inserted) ===\n" +
                "        self.pmsm = load_fmu('x')\n" +
                "        # === /physics: PMSM ===\n";
            form.SetPythonCode(code);
            form.Test_PhysicsCache["PMSM"] = new FmuInfo(
                "PMSM", "2.0", "CoSimulation",
                new[] { "u_d", "u_q" }, new[] { "y" },
                new List<FmuParameter>(), null);

            var actual = form.Test_TextEditor.Text;
            var pos = actual.IndexOf("load_fmu", StringComparison.Ordinal);
            Assert.True(pos > 0);
            form.Test_TextEditor.SelectionStart = pos;
            form.Test_RaiseSelectionChanged();

            var status = form.Test_StatusLabel.Text;
            Assert.Contains("Physics: PMSM", status);
            Assert.Contains("init", status);
        });
    }

    [Fact]
    public void SelectionChanged_OutsideBlock_NoPhysicsInStatus()
    {
        RunOnSta(() =>
        {
            using var form = new PythonEditorForm(null, false);
            form.SetPythonCode(Template);
            form.Test_TextEditor.SelectionStart = 0;
            form.Test_RaiseSelectionChanged();
            Assert.DoesNotContain("Physics:", form.Test_StatusLabel.Text);
        });
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0, idx = 0;
        while ((idx = haystack.IndexOf(needle, idx, StringComparison.Ordinal)) >= 0)
        {
            count++; idx += needle.Length;
        }
        return count;
    }
}