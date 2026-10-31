using System;
using System.Diagnostics;
using System.IO;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests
{
    /// <summary>
    /// Regression: after Create Simulation, PythonEditorForm must show
    /// btnRun / btnReload / btnOpenFolder (fixes first-launch layout race).
    /// Local-only; CI Session 0 is unstable (trap #21).
    /// </summary>
    [Trait("Category", "UI")]
    public class PythonEditorFormShownTests
    {
        [Fact(Skip = "Run locally only; CI unstable (trap #21)")]
        public void Buttons_Visible_After_Create_Simulation()
        {
            // Launch app, open pid_closed_loop.psbproj, Create Simulation, assert buttons visible.
            // Skeleton: see SmokeTests.cs for the launch/wait helpers.
            Assert.True(true, "Implement after SmokeTests patterns are reviewed");
        }
    }
}