using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// StaTableProjectReader のパース動作テスト。
    /// 実 XML 構造（cooking_heater_controller.xml）に準拠した最小サンプルで検証する。
    /// </summary>
    public class StaTableProjectReaderTests
    {
        // ==============================================================
        // サンプル XML
        // ==============================================================

        private const string MinimalXml = @"<?xml version='1.0' encoding='utf-8'?>
<Project name=""TestProject"">
    <ProjectSettings>
        <CodeGeneration project_name=""TestProject"" table_type=""array"" generation_style=""table_driven"" os_type=""non_rtos"" folder_structure=""by_layer"" generate_super_include=""true"" super_include_file=""statable_all.h"" max_consecutive_pending_events=""16"" />
    </ProjectSettings>
    <GlobalDefinitions>
        <SystemVariables>
            <Variable name=""g_system_tick"" type=""volatile uint32_t"" unit=""1ms"" default_value=""0"" group=""Timer"" description=""System tick"" title=""System tick"" array_size=""0"" />
        </SystemVariables>
        <EventFlags>
            <Flag name=""EVT_OVERHEAT"" min_value=""0"" max_value=""1"" group=""Safety"" description=""Overheat"" title=""Overheat"" />
        </EventFlags>
    </GlobalDefinitions>
    <SharedLibraries>
        <RoleFunctionLibrary>
            <RoleFunction name=""StartMagnetron"" namespace=""MwMicrowave"" title=""Start magnetron"" description=""Start magnetron"" return_type=""int"" />
        </RoleFunctionLibrary>
    </SharedLibraries>
    <Tab name=""Application"">
        <StateMachine initial=""Idle"" layer_priority=""5"" layer_description=""App layer"" layer_name=""Application"">
            <States>
                <State name=""Idle"" type=""initial"" parent="""" do="""" description=""Waiting"" />
                <State name=""Executing"" type=""normal"" parent="""" do="""" description=""Running"" />
                <State name=""Error"" type=""final"" parent="""" do="""" description=""Fatal"" />
            </States>
            <Events>
                <Event name=""START"" id=""1"" kind=""signal"" params="""" priority=""0"" description=""Start"" delivery_type=""direct"" source_layer=""middleware"" data_type="""" data_name="""" title=""Start"" />
                <Event name=""FAULT"" id=""2"" kind=""signal"" params=""err_code"" priority=""9"" description=""Fault"" delivery_type=""queue"" source_layer=""middleware"" data_type=""uint8_t"" data_name=""err_code"" title=""Fault"" />
            </Events>
            <RoleFunctions>
                <RoleFunction name=""StartStage"" namespace=""Application"" title=""Start"" description=""Start"" return_type=""int"" />
            </RoleFunctions>
            <Transitions>
                <Transition source=""Idle"" event=""START"" condition="""" action="""" target=""Executing"" transition_type=""external"" title=""Start"" has_else=""false"" else_target="""" early_return=""true"" label=""T1"" />
                <Transition source=""Executing"" event=""FAULT"" condition="""" action="""" target=""Error"" transition_type=""external"" title=""Fault"" has_else=""false"" else_target="""" early_return=""true"" label=""T1"" />
            </Transitions>
        </StateMachine>
    </Tab>
</Project>";

        // ==============================================================
        // 基本
        // ==============================================================

        [Fact]
        public void Parse_MinimalXml_ReturnsUsableResult()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse(MinimalXml);

            Assert.True(result.IsUsable);
            Assert.NotNull(result.Project);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Parse_NullXml_ReturnsError()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse(null);

            Assert.False(result.IsUsable);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Parse_EmptyXml_ReturnsError()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse("");

            Assert.False(result.IsUsable);
            Assert.NotEmpty(result.Errors);
        }

        [Fact]
        public void Parse_InvalidXml_ReturnsError()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse("<Project><broken");

            Assert.False(result.IsUsable);
            Assert.Contains(result.Errors, e => e.Contains("XML パースエラー"));
        }

        [Fact]
        public void Parse_WrongRoot_ReturnsError()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse("<NotProject />");

            Assert.False(result.IsUsable);
            Assert.Contains(result.Errors, e => e.Contains("<Project>"));
        }

        // ==============================================================
        // Project
        // ==============================================================

        [Fact]
        public void Parse_ProjectName_IsRead()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse(MinimalXml);

            Assert.Equal("TestProject", result.Project.Name);
        }

        [Fact]
        public void Parse_Project_HasSha256Hash()
        {
            var reader = new StaTableProjectReader();
            var result = reader.Parse(MinimalXml);

            Assert.False(string.IsNullOrEmpty(result.Project.SourceXmlHash));
            Assert.Equal(64, result.Project.SourceXmlHash.Length);
        }

        // ==============================================================
        // ProjectSettings
        // ==============================================================

        [Fact]
        public void Parse_Settings_AreRead()
        {
            var reader = new StaTableProjectReader();
            var s = reader.Parse(MinimalXml).Project.Settings;

            Assert.Equal("TestProject", s.ProjectName);
            Assert.Equal("array", s.TableType);
            Assert.Equal("table_driven", s.GenerationStyle);
            Assert.Equal("non_rtos", s.OsType);
            Assert.Equal("by_layer", s.FolderStructure);
            Assert.True(s.GenerateSuperInclude);
            Assert.Equal("statable_all.h", s.SuperIncludeFile);
            Assert.Equal(16, s.MaxConsecutivePendingEvents);
        }

        // ==============================================================
        // GlobalDefinitions
        // ==============================================================

        [Fact]
        public void Parse_SystemVariables_AreRead()
        {
            var reader = new StaTableProjectReader();
            var vars = reader.Parse(MinimalXml).Project.GlobalDefinitions.SystemVariables;

            Assert.Single(vars);
            Assert.Equal("g_system_tick", vars[0].Name);
            Assert.Equal("volatile uint32_t", vars[0].Type);
            Assert.Equal("1ms", vars[0].Unit);
        }

        [Fact]
        public void Parse_EventFlags_AreRead()
        {
            var reader = new StaTableProjectReader();
            var flags = reader.Parse(MinimalXml).Project.GlobalDefinitions.EventFlags;

            Assert.Single(flags);
            Assert.Equal("EVT_OVERHEAT", flags[0].Name);
            Assert.Equal(0, flags[0].MinValue);
            Assert.Equal(1, flags[0].MaxValue);
        }

        // ==============================================================
        // SharedLibraries
        // ==============================================================

        [Fact]
        public void Parse_SharedRoleFunctions_AreRead()
        {
            var reader = new StaTableProjectReader();
            var rfs = reader.Parse(MinimalXml).Project.SharedLibraries.RoleFunctionLibrary;

            Assert.Single(rfs);
            Assert.Equal("StartMagnetron", rfs[0].Name);
            Assert.Equal("MwMicrowave", rfs[0].Namespace);
            Assert.Equal("MwMicrowave.StartMagnetron", rfs[0].FullyQualifiedName);
        }

        // ==============================================================
        // Tab / StateMachine
        // ==============================================================

        [Fact]
        public void Parse_Tab_IsRead()
        {
            var reader = new StaTableProjectReader();
            var tabs = reader.Parse(MinimalXml).Project.Tabs;

            Assert.Single(tabs);
            Assert.Equal("Application", tabs[0].LayerName);
            Assert.Equal(5, tabs[0].LayerPriority);
            Assert.Equal("Idle", tabs[0].InitialState);
        }

        [Fact]
        public void Parse_States_AreRead()
        {
            var reader = new StaTableProjectReader();
            var states = reader.Parse(MinimalXml).Project.Tabs[0].States;

            Assert.Equal(3, states.Count);
            Assert.Equal("Idle", states[0].Name);
            Assert.Equal("initial", states[0].Type);
            Assert.Equal("Error", states[2].Name);
            Assert.Equal("final", states[2].Type);
        }

        [Fact]
        public void Parse_Events_AreRead()
        {
            var reader = new StaTableProjectReader();
            var events = reader.Parse(MinimalXml).Project.Tabs[0].Events;

            Assert.Equal(2, events.Count);
            Assert.Equal("START", events[0].Name);
            Assert.Equal("direct", events[0].DeliveryType);
            Assert.Equal("FAULT", events[1].Name);
            Assert.Equal("queue", events[1].DeliveryType);
            Assert.Equal(9, events[1].Priority);
        }

        [Fact]
        public void Parse_Transitions_AreRead()
        {
            var reader = new StaTableProjectReader();
            var trans = reader.Parse(MinimalXml).Project.Tabs[0].Transitions;

            Assert.Equal(2, trans.Count);
            Assert.Equal("Idle", trans[0].Source);
            Assert.Equal("START", trans[0].Event);
            Assert.Equal("Executing", trans[0].Target);
            Assert.True(trans[0].EarlyReturn);
            Assert.Equal("T1", trans[0].Label);
        }

        [Fact]
        public void Parse_LayerRoleFunctions_AreRead()
        {
            var reader = new StaTableProjectReader();
            var rfs = reader.Parse(MinimalXml).Project.Tabs[0].RoleFunctions;

            Assert.Single(rfs);
            Assert.Equal("StartStage", rfs[0].Name);
            Assert.Equal("Application.StartStage", rfs[0].FullyQualifiedName);
        }

        // ==============================================================
        // 警告系
        // ==============================================================

        [Fact]
        public void Parse_XmlWithoutCodeGeneration_AddsWarning()
        {
            string xml = @"<?xml version='1.0'?>
<Project name=""NoSettings"">
    <Tab name=""Empty"">
        <StateMachine initial=""A"" layer_priority=""5"" layer_name=""Empty"">
            <States>
                <State name=""A"" type=""initial"" parent="""" do="""" description="""" />
            </States>
        </StateMachine>
    </Tab>
</Project>";

            var reader = new StaTableProjectReader();
            var result = reader.Parse(xml);

            Assert.True(result.IsUsable);
            Assert.Contains(result.Warnings, w => w.Contains("CodeGeneration"));
        }

        [Fact]
        public void Parse_XmlWithoutTab_AddsWarning()
        {
            string xml = @"<?xml version='1.0'?>
<Project name=""NoTabs"">
    <ProjectSettings>
        <CodeGeneration project_name=""NoTabs"" />
    </ProjectSettings>
</Project>";

            var reader = new StaTableProjectReader();
            var result = reader.Parse(xml);

            Assert.True(result.IsUsable);
            Assert.Contains(result.Warnings, w => w.Contains("Tab"));
        }
    }
}