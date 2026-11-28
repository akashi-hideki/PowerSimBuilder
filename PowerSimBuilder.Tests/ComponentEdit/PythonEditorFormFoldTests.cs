using System;
using ScintillaNET;
using Xunit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    /// <summary>
    /// Regression tests for fold marker configuration.
    /// Trap #126: marker 25-31 mis-mapping broke click-to-fold.
    /// Uses Test_TextEditor (exposed via PythonEditorForm.Physics.cs).
    ///
    /// Note: ScintillaNET's Line class has no IsFoldPoint / IsExpanded.
    /// We use FoldLevelFlags.Header and SCI_GETFOLDEXPANDED instead.
    /// </summary>
    [Collection("WinForms")]
    public class PythonEditorFormFoldTests : IDisposable
    {
        private const int SCI_GETFOLDEXPANDED = 2230;

        private readonly PythonEditorForm _form;

        public PythonEditorFormFoldTests()
        {
            _form = new PythonEditorForm(
                filePath: null,
                enableRunButtons: false,
                initialCode: "def foo():\n    pass\n",
                initialTEnd: 5.0);
        }

        public void Dispose() => _form?.Dispose();

        private Scintilla Editor => _form.Test_TextEditor;

        // ScintillaNET computes FoldLevelFlags only when the lexer runs.
        // Headless tests never Show() the form, so we force it explicitly.
        private void SetCodeAndLex(string code)
        {
            if (!Editor.IsHandleCreated) Editor.CreateControl();
            _form.SetPythonCode(code);
            Editor.Colorize(0, -1);
            System.Windows.Forms.Application.DoEvents();
        }



        private static bool IsFoldPoint(Line line)
            => line.FoldLevelFlags.HasFlag(FoldLevelFlags.Header);

        private bool IsFoldExpanded(int lineIndex)
            => Editor.DirectMessage(SCI_GETFOLDEXPANDED,
                   new IntPtr(lineIndex), IntPtr.Zero) != IntPtr.Zero;

        // ============================================================
        // Trap #126: Marker 25-31 must match SC_MARKNUM_* constants.
        //   25 = FOLDEREND     -> BoxPlusConnected
        //   26 = FOLDEROPENMID -> BoxMinusConnected
        //   27 = FOLDERMIDTAIL -> TCorner
        //   28 = FOLDERTAIL    -> LCorner
        //   29 = FOLDERSUB     -> VLine
        //   30 = FOLDER (HEAD, collapsed) -> BoxPlus
        //   31 = FOLDEROPEN (HEAD, expanded) -> BoxMinus
        // ============================================================
        [Theory]
        [InlineData(25, MarkerSymbol.BoxPlusConnected)]
        [InlineData(26, MarkerSymbol.BoxMinusConnected)]
        [InlineData(27, MarkerSymbol.TCorner)]
        [InlineData(28, MarkerSymbol.LCorner)]
        [InlineData(29, MarkerSymbol.VLine)]
        [InlineData(30, MarkerSymbol.BoxPlus)]
        [InlineData(31, MarkerSymbol.BoxMinus)]
        public void MarkerSymbols_MatchScintillaConstant(int index, MarkerSymbol expected)
        {
            Assert.Equal(expected, Editor.Markers[index].Symbol);
        }

        // ============================================================
        // Fold margin configuration
        // ============================================================
        [Fact]
        public void FoldMargin_IsSymbolType_AndSensitive()
        {
            var m = Editor.Margins[1];
            Assert.Equal(MarginType.Symbol, m.Type);
            Assert.True(m.Sensitive,
                "Margin 1 must be Sensitive to accept clicks");
            Assert.True(m.Width > 0,
                "Margin 1 must have non-zero width");
        }

        // ============================================================
        // Fold point detection at def / class lines
        // ============================================================
        [Fact]
        public void SetPythonCode_DefLine_IsFoldPoint()
        {
            SetCodeAndLex("def foo():\n    x = 1\n    y = 2\n");
            Assert.True(IsFoldPoint(Editor.Lines[0]),
                "def line must be a fold point");
        }

        [Fact]
        public void SetPythonCode_ClassAndDef_BothFoldPoints()
        {
            SetCodeAndLex(
                "class A:\n" +
                "    def __init__(self):\n" +
                "        pass\n" +
                "    def run(self):\n" +
                "        pass\n");
            Assert.True(IsFoldPoint(Editor.Lines[0]), "class line");
            Assert.True(IsFoldPoint(Editor.Lines[1]), "__init__ line");
            Assert.True(IsFoldPoint(Editor.Lines[3]), "run line");
        }

        [Fact]
        public void SetPythonCode_NonBlockLine_NotFoldPoint()
        {
            SetCodeAndLex("x = 1\ny = 2\n");
            Assert.False(IsFoldPoint(Editor.Lines[0]));
            Assert.False(IsFoldPoint(Editor.Lines[1]));
        }

        // ============================================================
        // Fold toggle behavior
        // ============================================================
        [Fact]
        public void ToggleFold_ChangesExpandedState()
        {
            SetCodeAndLex("def foo():\n    a = 1\n    b = 2\n");
            var line = Editor.Lines[0];
            Assert.True(IsFoldPoint(line), "precondition: def must be fold point");

            bool before = IsFoldExpanded(0);
            line.ToggleFold();
            Assert.NotEqual(before, IsFoldExpanded(0));

            line.ToggleFold();
            Assert.Equal(before, IsFoldExpanded(0));
        }
    }
}
