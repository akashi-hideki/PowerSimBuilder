using System.Collections.Generic;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// StaTableModel の基本動作テスト。
    /// モデルは POCO のため、デフォルト値と算出プロパティのみ検証する。
    /// </summary>
    public class StaTableModelTests
    {
        // ==============================================================
        // StaTableProject
        // ==============================================================

        [Fact]
        public void StaTableProject_DefaultsAreInitialized()
        {
            var project = new StaTableProject();

            Assert.NotNull(project.Settings);
            Assert.NotNull(project.GlobalDefinitions);
            Assert.NotNull(project.SharedLibraries);
            Assert.NotNull(project.Tabs);
            Assert.Empty(project.Tabs);
        }

        // ==============================================================
        // StaTableProjectSettings
        // ==============================================================

        [Fact]
        public void StaTableProjectSettings_MaxConsecutivePendingEvents_DefaultIs16()
        {
            var settings = new StaTableProjectSettings();
            Assert.Equal(16, settings.MaxConsecutivePendingEvents);
        }

        // ==============================================================
        // StaTableGlobalDefinitions
        // ==============================================================

        [Fact]
        public void StaTableGlobalDefinitions_DefaultsAreEmptyLists()
        {
            var defs = new StaTableGlobalDefinitions();

            Assert.NotNull(defs.SystemVariables);
            Assert.NotNull(defs.EventFlags);
            Assert.NotNull(defs.Interrupts);
            Assert.NotNull(defs.TimerBase);
            Assert.Empty(defs.SystemVariables);
        }

        // ==============================================================
        // StaTableRoleFunction
        // ==============================================================

        [Fact]
        public void StaTableRoleFunction_FullyQualifiedName_WithNamespace()
        {
            var rf = new StaTableRoleFunction
            {
                Name = "StartMagnetron",
                Namespace = "MwMicrowave"
            };

            Assert.Equal("MwMicrowave.StartMagnetron", rf.FullyQualifiedName);
        }

        [Fact]
        public void StaTableRoleFunction_FullyQualifiedName_WithoutNamespace()
        {
            var rf = new StaTableRoleFunction
            {
                Name = "StandaloneFunc",
                Namespace = null
            };

            Assert.Equal("StandaloneFunc", rf.FullyQualifiedName);
        }

        [Fact]
        public void StaTableRoleFunction_FullyQualifiedName_EmptyNamespace()
        {
            var rf = new StaTableRoleFunction
            {
                Name = "StandaloneFunc",
                Namespace = ""
            };

            Assert.Equal("StandaloneFunc", rf.FullyQualifiedName);
        }

        [Fact]
        public void StaTableRoleFunction_DefaultsAreInitialized()
        {
            var rf = new StaTableRoleFunction();

            Assert.Equal("int", rf.ReturnType);
            Assert.NotNull(rf.UsedGlobalVars);
            Assert.NotNull(rf.UsedEvents);
            Assert.NotNull(rf.UsedLiterals);
        }

        // ==============================================================
        // StaTableStateMachine
        // ==============================================================

        [Fact]
        public void StaTableStateMachine_LayerPriority_DefaultIs5()
        {
            var sm = new StaTableStateMachine();
            Assert.Equal(5, sm.LayerPriority);
        }

        [Fact]
        public void StaTableStateMachine_DefaultsAreInitialized()
        {
            var sm = new StaTableStateMachine();

            Assert.NotNull(sm.States);
            Assert.NotNull(sm.Events);
            Assert.NotNull(sm.Transitions);
            Assert.NotNull(sm.RoleFunctions);
            Assert.NotNull(sm.CellActions);
            Assert.NotNull(sm.CellRelations);
            Assert.Empty(sm.States);
        }

        [Fact]
        public void StaTableStateMachine_CellActions_CanRegisterByCompositeKey()
        {
            var sm = new StaTableStateMachine();
            var key = "Idle|START";

            sm.CellActions[key] = new List<StaTableCellAction>
            {
                new StaTableCellAction { Kind = "call", RoleFunction = "App.Start" }
            };

            Assert.True(sm.CellActions.ContainsKey(key));
            Assert.Single(sm.CellActions[key]);
            Assert.Equal("App.Start", sm.CellActions[key][0].RoleFunction);
        }

        // ==============================================================
        // StaTableTransition
        // ==============================================================

        [Fact]
        public void StaTableTransition_DefaultsAreFalseForBooleans()
        {
            var tr = new StaTableTransition();

            Assert.False(tr.HasElse);
            Assert.False(tr.EarlyReturn);
        }

        // ==============================================================
        // StaTableParseResult
        // ==============================================================

        [Fact]
        public void StaTableParseResult_IsUsable_TrueWhenNoErrors()
        {
            var result = new StaTableParseResult
            {
                Project = new StaTableProject()
            };

            Assert.True(result.IsUsable);
        }

        [Fact]
        public void StaTableParseResult_IsUsable_FalseWhenErrorsExist()
        {
            var result = new StaTableParseResult
            {
                Project = new StaTableProject()
            };
            result.Errors.Add("parse failed");

            Assert.False(result.IsUsable);
        }

        [Fact]
        public void StaTableParseResult_IsUsable_FalseWhenProjectIsNull()
        {
            var result = new StaTableParseResult();
            Assert.False(result.IsUsable);
        }

        [Fact]
        public void StaTableParseResult_DefaultsAreEmptyLists()
        {
            var result = new StaTableParseResult();

            Assert.NotNull(result.Warnings);
            Assert.NotNull(result.Errors);
            Assert.Empty(result.Warnings);
            Assert.Empty(result.Errors);
        }
    }
}