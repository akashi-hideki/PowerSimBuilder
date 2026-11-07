using System;
using PowerSimBuilder.SimPartsManege;
using Xunit;

namespace PowerSimBuilder.Tests.RoleSnippets
{
    /// <summary>
    /// Tests for the "Register Snippet" visibility rule:
    /// the entry points must appear only when the file contains at least one
    /// STABLE_USER_CODE_START marker.
    ///
    /// Layer 1 (pure function) : RoleSnippetRegistrar.ContainsRoleMarkers
    /// Layer 2 (form behaviour): PythonEditorForm.UpdateRegisterSnippetAvailability
    /// </summary>
    public class RoleSnippetVisibilityTests
    {
        [Theory]
        [InlineData("class Controller:\n    def F_Test(self):\n        # STABLE_USER_CODE_START:MwOven.F_Test\n        return 0\n        # STABLE_USER_CODE_END:MwOven.F_Test\n", true)]
        [InlineData("# STABLE_USER_CODE_START:DriverInput.F_ReadSensorsMovingAvg\n", true)]
        [InlineData("class X:\n    #STABLE_USER_CODE_START:A.F_A\n", true)]
        [InlineData("class X:\n    def foo(self): pass\n", false)]
        [InlineData("class X:\n    # STABLE_USER_CODE_END:A.F_A\n", false)]
        [InlineData("print(\"# STABLE_USER_CODE_START:A.F_A\")\n", false)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   \n\t\n", false)]
        public void ContainsRoleMarkers_ClassifiesVariousInputs(string text, bool expected)
        {
            Assert.Equal(expected, RoleSnippetRegistrar.ContainsRoleMarkers(text));
        }

        [Fact]
        public void Form_UpdateAvailability_ShowsButtonAndMenuWhenMarkersPresent()
        {
            const string code =
                "class Controller:\n" +
                "    def F_Test(self):\n" +
                "        # STABLE_USER_CODE_START:MwOven.F_Test\n" +
                "        return 0\n" +
                "        # STABLE_USER_CODE_END:MwOven.F_Test\n";
            using var form = new PythonEditorForm(null, false, code);
            form.Test_UpdateRegisterSnippetAvailability();
            Assert.True(form.Test_BtnRegisterSnippetVisible);
            Assert.True(form.Test_MenuItemRegisterSnippetVisible);
        }

        [Fact]
        public void Form_UpdateAvailability_HidesButtonAndMenuWhenNoMarkers()
        {
            const string code = "class X:\n    def foo(self): pass\n";
            using var form = new PythonEditorForm(null, false, code);
            form.Test_UpdateRegisterSnippetAvailability();
            Assert.False(form.Test_BtnRegisterSnippetVisible);
            Assert.False(form.Test_MenuItemRegisterSnippetVisible);
        }

        [Fact]
        public void Form_UpdateAvailability_HidesButtonOnEmptyCode()
        {
            using var form = new PythonEditorForm(null, false, "");
            form.Test_UpdateRegisterSnippetAvailability();
            Assert.False(form.Test_BtnRegisterSnippetVisible);
            Assert.False(form.Test_MenuItemRegisterSnippetVisible);
        }

        [Fact]
        public void Form_UpdateAvailability_TogglesCorrectlyAcrossCodeChanges()
        {
            const string withMarkers =
                "class A:\n    # STABLE_USER_CODE_START:A.F_A\n    def F_A(self): pass\n";
            const string withoutMarkers =
                "class A:\n    def foo(self): pass\n";

            using var form = new PythonEditorForm(null, false, withMarkers);
            form.Test_UpdateRegisterSnippetAvailability();
            Assert.True(form.Test_BtnRegisterSnippetVisible);

            form.SetPythonCode(withoutMarkers);
            form.Test_UpdateRegisterSnippetAvailability();
            Assert.False(form.Test_BtnRegisterSnippetVisible);

            form.SetPythonCode(withMarkers);
            form.Test_UpdateRegisterSnippetAvailability();
            Assert.True(form.Test_BtnRegisterSnippetVisible);
        }
    }
}
