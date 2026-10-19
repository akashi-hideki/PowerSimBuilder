using System;
using System.Collections.Generic;
using Xunit;
using PowerSimBuilder;
using PowerSimBuilder.ComponentEdit;

namespace PowerSimBuilder.Tests.ComponentEdit
{
    [Collection(I18nEnglishCollection.Name)]
    public class EditPanelTests
    {
        [Fact]
        public void LoadComponents_PopulatesTreeWithDisplayNames()
        {
            var panel = new EditPanel();
            try
            {
                var list = new List<ComponentInfo>
                {
                    new ComponentInfo { Name = "Gain", Source = ComponentSourceKind.Bundled },
                    new ComponentInfo { Name = "MyFilter", Source = ComponentSourceKind.Override },
                };
                panel.LoadComponents(list);

                Assert.Equal(2, panel.Test_ComponentsTree.Nodes.Count);
                Assert.Contains("\uD83D\uDD12", panel.Test_ComponentsTree.Nodes[0].Text);
                Assert.Contains("\uD83D\uDD04", panel.Test_ComponentsTree.Nodes[1].Text);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void LoadRoleFunctions_CountsImplementedAndStub()
        {
            var panel = new EditPanel();
            try
            {
                var list = new List<RoleFunctionInfo>
                {
                    new RoleFunctionInfo { Name = "F_A", StartLine = 10, EndLine = 12, Kind = RoleFunctionKind.Implemented },
                    new RoleFunctionInfo { Name = "F_B", StartLine = 15, EndLine = 17, Kind = RoleFunctionKind.Stub },
                    new RoleFunctionInfo { Name = "F_C", StartLine = 20, EndLine = 22, Kind = RoleFunctionKind.Stub },
                };
                panel.LoadRoleFunctions(list);

                Assert.Equal(3, panel.Test_RoleFunctionsTree.Nodes.Count);
                Assert.Equal("1/3", panel.Test_RoleCountLabel.Text);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void JumpToNextStub_FiresEventForFirstStub()
        {
            var panel = new EditPanel();
            try
            {
                RoleFunctionInfo captured = null;
                panel.JumpRequested += (s, e) => captured = e;
                panel.LoadRoleFunctions(new List<RoleFunctionInfo>
                {
                    new RoleFunctionInfo { Name = "F_A", Kind = RoleFunctionKind.Implemented },
                    new RoleFunctionInfo { Name = "F_B", Kind = RoleFunctionKind.Stub },
                    new RoleFunctionInfo { Name = "F_C", Kind = RoleFunctionKind.Stub },
                });
                panel.JumpToNextStub();
                Assert.NotNull(captured);
                Assert.Equal("F_B", captured.Name);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void JumpToNextStub_CyclesFromCurrent()
        {
            var panel = new EditPanel();
            try
            {
                var captured = new List<string>();
                panel.JumpRequested += (s, e) => captured.Add(e.Name);
                panel.LoadRoleFunctions(new List<RoleFunctionInfo>
                {
                    new RoleFunctionInfo { Name = "F_A", Kind = RoleFunctionKind.Stub },
                    new RoleFunctionInfo { Name = "F_B", Kind = RoleFunctionKind.Stub },
                    new RoleFunctionInfo { Name = "F_C", Kind = RoleFunctionKind.Stub },
                });
                panel.JumpToNextStub();
                panel.JumpToNextStub();
                Assert.Equal(new[] { "F_A", "F_B" }, captured.ToArray());
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void LoadRoleFunctions_NullDoesNotThrow()
        {
            var panel = new EditPanel();
            try
            {
                panel.LoadRoleFunctions(null);
                Assert.Equal(0, panel.Test_RoleFunctionsTree.Nodes.Count);
                Assert.Equal("0/0", panel.Test_RoleCountLabel.Text);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void OverrideButtonClick_WithoutSelection_DoesNotFire()
        {
            var panel = new EditPanel();
            try
            {
                bool fired = false;
                panel.OverrideRequested += (s, e) => fired = true;
                panel.Test_OverrideButton.PerformClick();
                Assert.False(fired);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void OverrideButtonClick_WithBundledSelected_Fires()
        {
            var panel = new EditPanel();
            try
            {
                ComponentInfo captured = null;
                panel.OverrideRequested += (s, e) => captured = e;
                panel.LoadComponents(new List<ComponentInfo>
                {
                    new ComponentInfo { Name = "Gain", Source = ComponentSourceKind.Bundled },
                });
                panel.Test_ComponentsTree.SelectedNode = panel.Test_ComponentsTree.Nodes[0];
                panel.Test_OverrideButton.PerformClick();
                Assert.NotNull(captured);
                Assert.Equal("Gain", captured.Name);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void OverrideButtonClick_WithOverrideSelected_DoesNotFire()
        {
            var panel = new EditPanel();
            try
            {
                bool fired = false;
                panel.OverrideRequested += (s, e) => fired = true;
                panel.LoadComponents(new List<ComponentInfo>
                {
                    new ComponentInfo { Name = "Mine", Source = ComponentSourceKind.Override },
                });
                panel.Test_ComponentsTree.SelectedNode = panel.Test_ComponentsTree.Nodes[0];
                panel.Test_OverrideButton.PerformClick();
                Assert.False(fired);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void ApplyI18n_SetsButtonTextFromResources()
        {
            var panel = new EditPanel();
            try
            {
                Assert.Equal("+Override", panel.Test_OverrideButton.Text);
                Assert.Equal("+Adapter",  panel.Test_AdapterButton.Text);
                Assert.Equal("+Import C", panel.Test_ImportCButton.Text);
            }
            finally { panel.Dispose(); }
        }

        [Fact]
        public void ApplyI18n_RoleCountFormatIsNeutral()
        {
            var panel = new EditPanel();
            try
            {
                panel.LoadRoleFunctions(new List<RoleFunctionInfo>
                {
                    new RoleFunctionInfo { Name = "F_A", Kind = RoleFunctionKind.Implemented },
                });
                Assert.Equal("1/1", panel.Test_RoleCountLabel.Text);
            }
            finally { panel.Dispose(); }
        }
    }
}