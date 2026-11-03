using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests;

/// <summary>
/// Phase C: FlaUI + structured log for ControllerBox edit paths.
/// C1: StaTable-bound ControllerBox (generated_path set) opens editor directly.
/// C2: Palette ControllerBox (generated_path empty) shows *_powersim.py prompt.
/// </summary>
public class ControllerBoxEditE2ETests
{
    private static FlaUI.Core.Application LaunchWithLog(string args, string logPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = TestPaths.AppExePath,
            Arguments = args,
            UseShellExecute = false,
            // Resolve relative paths (generated/CookingHeaterController_powersim.py)
            // against the repo root, matching how --test-project is loaded.
            WorkingDirectory = TestPaths.ProjectRoot,
        };
        psi.EnvironmentVariables["PSB_EDIT_LOG"] = logPath;
        psi.EnvironmentVariables["PSB_EDIT_SESSION"] = "e2e-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        return FlaUI.Core.Application.Launch(psi);
    }

    [Fact]
    public void ExistingControllerBox_OpensEditor_WithoutPrompt()
    {
        var logPath = EditLogReader.NewLogPath("c1");
        var fixture = Path.Combine(TestPaths.ProjectRoot, "examples", "demos", "oven_control_statable.psbproj");
        Assert.True(File.Exists(fixture), "fixture missing: " + fixture);

        var args = $"--test-project \"{fixture}\" --test-open-editor \"Controller Box\"";
        using var app = LaunchWithLog(args, logPath);
        try
        {
            using var automation = new UIA3Automation();

            EditLogReader.WaitForEvent(logPath, "OpenEditor", TimeSpan.FromSeconds(20));

            var events = EditLogReader.EventNames(logPath);
            Assert.Contains("EditStart", events);
            Assert.Contains("GeneratedPathRead", events);
            Assert.Contains("OpenEditor", events);
            Assert.DoesNotContain("EmptyGeneratedPath", events);

            var read = EditLogReader.FirstEvent(logPath, "GeneratedPathRead");
            Assert.NotNull(read);
            var value = EditLogReader.GetString(read!.Value, "value") ?? "";
            Assert.Contains("CookingHeaterController_powersim.py", value);

            var open = EditLogReader.FirstEvent(logPath, "OpenEditor");
            Assert.NotNull(open);
            var path = EditLogReader.GetString(open!.Value, "path") ?? "";
            Assert.Contains("CookingHeaterController_powersim.py", path);
        }
        finally
        {
            try { app.Kill(); } catch { }
            try { File.Delete(logPath); } catch { }
        }
    }

    [Fact]
    public void PaletteControllerBox_PromptsForPowersimFile()
    {
        var logPath = EditLogReader.NewLogPath("c2");
        var fixture = TestPaths.TestFixturePath("with-controllerbox.json");
        Assert.True(File.Exists(fixture), "fixture missing: " + fixture);

        var args = $"--test-project \"{fixture}\" --test-open-editor \"controllerBox1\"";
        using var app = LaunchWithLog(args, logPath);
        try
        {
            using var automation = new UIA3Automation();

            // Wait until the prompt is shown (the dialog blocks the app thread).
            EditLogReader.WaitForEvent(logPath, "PromptUserPickStart", TimeSpan.FromSeconds(20));

            var events = EditLogReader.EventNames(logPath);
            Assert.Contains("EditStart", events);
            Assert.Contains("GeneratedPathRead", events);
            Assert.Contains("EmptyGeneratedPath", events);
            Assert.Contains("PromptUserPickStart", events);
            Assert.DoesNotContain("OpenEditor", events);

            var empty = EditLogReader.FirstEvent(logPath, "EmptyGeneratedPath");
            Assert.NotNull(empty);
            Assert.Equal("prompt", EditLogReader.GetString(empty!.Value, "action"));

            // Dismiss the OpenFileDialog with ESC, then verify cancellation is logged.
            Thread.Sleep(500);
            Keyboard.Press(VirtualKeyShort.ESCAPE);
            EditLogReader.WaitForEvent(logPath, "PromptUserPickCancelled", TimeSpan.FromSeconds(10));
            Assert.True(EditLogReader.HasEvent(logPath, "PromptUserPickCancelled"),
                "cancellation not logged after ESC");
        }
        finally
        {
            try { app.Kill(); } catch { }
            try { File.Delete(logPath); } catch { }
        }
    }
}
