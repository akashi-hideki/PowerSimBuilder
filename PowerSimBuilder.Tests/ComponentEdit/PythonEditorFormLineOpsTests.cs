using System;
using System.Threading;
using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit;

public class PythonEditorFormLineOpsTests
{
    private static void RunSta(Action action)
    {
        Exception ex = null;
        var t = new Thread(() => { try { action(); } catch (Exception e) { ex = e; } });
        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();
        if (ex != null) throw ex;
    }

    [Fact]
    public void DuplicateLine_InsertsCopyBelow()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\nline2\nline3");
            form.Test_SetCursor(2, 0);
            form.Test_DuplicateCurrentLine();
            Assert.Equal("line1\nline2\nline2\nline3", form.GetPythonCode());
        });
    }

    [Fact]
    public void DeleteLine_RemovesCurrentLine()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\nline2\nline3");
            form.Test_SetCursor(2, 0);
            form.Test_DeleteCurrentLine();
            Assert.Equal("line1\nline3", form.GetPythonCode());
        });
    }

    [Fact]
    public void MoveLineUp_SwapsWithPrevious()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\nline2\nline3");
            form.Test_SetCursor(3, 0);
            form.Test_MoveLineUp();
            Assert.Equal("line1\nline3\nline2", form.GetPythonCode());
        });
    }

    [Fact]
    public void MoveLineDown_SwapsWithNext()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\nline2\nline3");
            form.Test_SetCursor(1, 0);
            form.Test_MoveLineDown();
            Assert.Equal("line2\nline1\nline3", form.GetPythonCode());
        });
    }

    [Fact]
    public void ToggleComment_AddsHash()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\nline2");
            form.Test_SetCursor(2, 0);
            form.Test_ToggleComment();
            Assert.Equal("line1\n# line2", form.GetPythonCode());
        });
    }

    [Fact]
    public void ToggleComment_RemovesHash()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\n# line2");
            form.Test_SetCursor(2, 0);
            form.Test_ToggleComment();
            Assert.Equal("line1\nline2", form.GetPythonCode());
        });
    }

    [Fact]
    public void SelectLine_NoSelection_SelectsWholeLine()
    {
        RunSta(() =>
        {
            using var form = new PythonEditorForm(null, false, "line1\nline2\nline3");
            form.Test_SetCursor(2, 2);
            form.Test_SelectCurrentLine();
            Assert.Equal("line2", form.Test_GetSelection());
        });
    }
}