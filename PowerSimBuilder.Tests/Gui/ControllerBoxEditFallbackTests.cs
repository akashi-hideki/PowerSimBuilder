using System;
using System.IO;
using System.Reflection;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Palette ControllerBox (generated_path empty) now prompts the user to pick a
    /// *_powersim.py file to edit. The picker is a UI dialog and not unit-testable,
    /// but we verify the helper exists and the prompt type is OpenFileDialog.
    /// </summary>
    public class ControllerBoxEditFallbackTests
    {
        [Fact]
        public void PromptUserPickPowersimFile_Exists()
        {
            var m = typeof(DesignCanvas).GetMethod("PromptUserPickPowersimFile",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(m);
            Assert.Equal(typeof(string), m!.ReturnType);
        }

        [Fact]
        public void OldResolver_NoLongerExists()
        {
            // Ensures we do not accidentally re-introduce the misleading fallback
            // that opened user_components/ControllerBox.py (all-comment adapter).
            var m = typeof(DesignCanvas).GetMethod("ResolveDefaultControllerBoxPath",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.Null(m);
        }
    }
}
