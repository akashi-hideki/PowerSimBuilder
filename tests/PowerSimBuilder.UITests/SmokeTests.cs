using System;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace PowerSimBuilder.UITests;

/// <summary>
/// L3 smoke tests via FlaUI UIA3. Launches the real PowerSimBuilder.exe.
/// Run locally: dotnet test tests\PowerSimBuilder.UITests --filter "Category=UI"
/// CI: main push only (see .github/workflows/ci.yml).
/// </summary>
[Trait("Category", "UI")]
public sealed class SmokeTests : IDisposable
{
    private readonly FlaUI.Core.Application _app;
    private readonly UIA3Automation _automation;

    public SmokeTests()
    {
        // Test 1 only: no arguments needed. args.Length == 0 -> MainForm opens.
        _app = FlaUI.Core.Application.Launch(TestPaths.AppExePath);
        _automation = new UIA3Automation();
    }

    [Fact]
    public void LaunchApp_ShowsMainWindow()
    {
        var window = _app.GetMainWindow(_automation, TimeSpan.FromSeconds(20));
        Assert.NotNull(window);

        var automationId = window.Properties.AutomationId.ValueOrDefault;
        Assert.Equal("MainForm", automationId);

        Assert.False(string.IsNullOrWhiteSpace(window.Title),
            "MainForm title should not be empty.");
    }

    public void Dispose()
    {
        try { _app.Close(); } catch { /* best effort */ }
        _automation.Dispose();
        _app.Dispose();
    }
}
