using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PowerSimBuilder;
using PowerSimBuilder.ComponentEdit;
using PowerSimBuilder.Tests.Gui;
using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    [Collection("GlobalServices")]
    public class GuiDiagnosticsTests : IDisposable
    {
        private readonly MockMessageBoxService _box = new();
        private readonly MockFileDialogService _dlg = new();
        private readonly List<string> _tempFiles = new();
        private readonly List<string> _tempDirs = new();

        public GuiDiagnosticsTests()
        {
            I18n.SetLanguage("en");
            AppServices.MessageBox = _box;
            AppServices.FileDialog = _dlg;
        }

        public void Dispose()
        {
            AppServices.Reset();
            I18n.SetLanguage("en");
            foreach (var f in _tempFiles) { try { File.Delete(f); } catch { } }
            foreach (var d in _tempDirs) { try { Directory.Delete(d, true); } catch { } }
        }

        private string CreateTempPy(string content)
        {
            var path = Path.Combine(Path.GetTempPath(), "psb_diag_" + Guid.NewGuid().ToString("N") + ".py");
            File.WriteAllText(path, content);
            _tempFiles.Add(path);
            return path;
        }

        private string CreateTempDir()
        {
            var path = Path.Combine(Path.GetTempPath(), "psb_diag_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            _tempDirs.Add(path);
            return path;
        }

        [Fact]
        public void ComponentManager_NullDescriptor_ShowsError()
        {
            ComponentManager.GeneratePythonComponent(null);
            Assert.Single(_box.ErrorCalls);
            Assert.Equal(I18n.T("Manager.MsgNoSelection"), _box.ErrorCalls[0].Message);
            Assert.Equal(I18n.T("Common.MsgTitleError"), _box.ErrorCalls[0].Title);
            Assert.Empty(_box.InfoCalls);
        }

        [Fact]
        public void ComponentManager_SaveCanceled_DoesNothing()
        {
            _dlg.SaveResult = null;
            var descriptor = ComponentDescriptorDictionary.Descriptors.Values.First();
            ComponentManager.GeneratePythonComponent(descriptor);
            Assert.Single(_dlg.SaveCalls);
            Assert.Empty(_box.InfoCalls);
            Assert.Empty(_box.ErrorCalls);
        }

        [Fact]
        public void EditPanel_JumpToNextStub_NoFunctions_ShowsInfoMessage()
        {
            GuiSta.Run(() =>
            {
                using var panel = new EditPanel();
                panel.JumpToNextStub();
                Assert.Single(_box.InfoCalls);
                Assert.Equal(I18n.T("EditPanel.MsgJumpNoFunc"), _box.InfoCalls[0].Message);
                Assert.Equal(I18n.T("Common.MsgTitleInfo"), _box.InfoCalls[0].Title);
            });
        }

        [Fact]
        public void PythonEditorForm_CreateOverride_Bundled_WritesCommentedFile()
        {
            GuiSta.Run(() =>
            {
                var bundledDir = CreateTempDir();
                var bundledFile = Path.Combine(bundledDir, "Gain.py");
                File.WriteAllText(bundledFile, "class Gain:\n    pass\n");
                var projectDir = CreateTempDir();

                using var form = new PythonEditorForm(bundledFile, false);
                form.ProjectDir = projectDir;
                var info = new ComponentInfo
                {
                    Name = "Gain",
                    Source = ComponentSourceKind.Bundled,
                    FilePath = bundledFile,
                };
                var method = typeof(PythonEditorForm).GetMethod("CreateOverride",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(form, new object[] { info });

                var expectedPath = Path.Combine(projectDir, "user_components", "Gain.py");
                Assert.True(File.Exists(expectedPath), "override file should be created");
                var content = File.ReadAllText(expectedPath);
                Assert.Contains("# override:", content);
                Assert.Contains("# class Gain:", content);
                Assert.Single(_box.InfoCalls);
                Assert.StartsWith("Created override:", _box.InfoCalls[0].Message);
            });
        }

        [Fact]
        public void PythonEditorForm_CreateOverride_NonBundled_DoesNothing()
        {
            GuiSta.Run(() =>
            {
                var bundledFile = CreateTempPy("class Gain: pass\n");
                var projectDir = CreateTempDir();
                using var form = new PythonEditorForm(bundledFile, false);
                form.ProjectDir = projectDir;
                var info = new ComponentInfo
                {
                    Name = "Gain",
                    Source = ComponentSourceKind.Override,
                    FilePath = bundledFile,
                };
                var method = typeof(PythonEditorForm).GetMethod("CreateOverride",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(form, new object[] { info });

                Assert.False(Directory.Exists(Path.Combine(projectDir, "user_components")));
                Assert.Empty(_box.InfoCalls);
                Assert.Empty(_box.ErrorCalls);
            });
        }

        [Fact]
        public void PythonEditorForm_AdapterButton_InvokesNotImplementedMessage()
        {
            GuiSta.Run(() =>
            {
                var path = CreateTempPy("class Foo: pass\n");
                using var form = new PythonEditorForm(path, false);
                var editPanel = GetEditPanel(form);
                InvokeButtonClick(editPanel.Test_AdapterButton);
                Assert.Single(_box.InfoCalls);
                Assert.Equal(I18n.T("EditPanel.MsgAdapterNotReady"), _box.InfoCalls[0].Message);
                Assert.Equal(I18n.T("Common.MsgTitleInfo"), _box.InfoCalls[0].Title);
            });
        }

        [Fact]
        public void DesignCanvas_GeneratePythonItem_Click_InvokesSaveDialog()
        {
            GuiSta.Run(() =>
            {
                var canvas = new DesignCanvas();
                var descriptor = ComponentDescriptorDictionary.Descriptors.Values
                    .First(d => d.InputPorts != null && d.InputPorts.Length > 0
                             && d.OutputPorts != null && d.OutputPorts.Length > 0);
                canvas.AddComponent(descriptor, new Point(100, 100));
                var comp = canvas.Components[0];
                SetSelectedComponent(canvas, comp);
                _dlg.SaveResult = null;

                var method = typeof(DesignCanvas).GetMethod("GeneratePythonItem_Click",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                method.Invoke(canvas, new object[] { canvas, EventArgs.Empty });

                Assert.Single(_dlg.SaveCalls);
                Assert.Equal(I18n.T("Manager.SaveDialogTitle"), _dlg.SaveCalls[0].Title);
                Assert.EndsWith(".py", _dlg.SaveCalls[0].DefaultFileName);
            });
        }

        private static EditPanel GetEditPanel(PythonEditorForm form)
        {
            var field = typeof(PythonEditorForm).GetField("editPanel",
                BindingFlags.NonPublic | BindingFlags.Instance);
            return (EditPanel)field.GetValue(form);
        }

        private static void SetSelectedComponent(DesignCanvas canvas, ComponentControl comp)
        {
            var prop = typeof(DesignCanvas).GetProperty("SelectedComponent");
            var setter = prop.GetSetMethod(nonPublic: true);
            setter.Invoke(canvas, new object[] { comp });
        }

        private static void InvokeButtonClick(Button button)
        {
            var m = typeof(Button).GetMethod("OnClick",
                BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(button, new object[] { EventArgs.Empty });
        }
    }
}