using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// StaTableCodeGenerator の生成コード検証。
    /// 決定論的出力を確認するため GeneratedAt を固定する。
    /// </summary>
    public class StaTableCodeGeneratorTests
    {
        private static readonly DateTime FixedTime =
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        private static StaTableCodeGenerator MakeGenerator()
        {
            return new StaTableCodeGenerator { GeneratedAt = FixedTime };
        }

        private static StaTableProject MakeMinimalProject()
        {
            var project = new StaTableProject
            {
                Name = "TestProject",
                SourceFilePath = "cooking_heater_controller.xml",
                SourceXmlHash = "abcdef0123456789",
                Settings = new StaTableProjectSettings
                {
                    GenerationStyle = "table_driven",
                    OsType = "non_rtos",
                },
            };

            // Layer 1
            var layer1 = new StaTableStateMachine
            {
                LayerName = "DriverInput",
                LayerPriority = 1,
                LayerDescription = "Input devices layer",
                InitialState = "InIdle",
            };
            layer1.States.Add(new StaTableState { Name = "InIdle", Type = "initial" });
            layer1.States.Add(new StaTableState { Name = "InSensing", Type = "normal" });
            layer1.Events.Add(new StaTableEvent
            {
                Name = "SENSOR_TICK", Id = 1, DeliveryType = "queue",
                Priority = 0, SourceLayer = "driver",
            });
            layer1.Events.Add(new StaTableEvent
            {
                Name = "ADC_DONE", Id = 2, DeliveryType = "direct",
                Priority = 0, SourceLayer = "driver",
            });
            layer1.Transitions.Add(new StaTableTransition
            {
                Source = "InIdle", Event = "SENSOR_TICK",
                Condition = "", Action = "", Target = "InSensing",
                Title = "Sensor tick",
            });
            layer1.Transitions.Add(new StaTableTransition
            {
                Source = "InSensing", Event = "ADC_DONE",
                Condition = "", Action = "", Target = "InIdle",
                Title = "Sensing done",
            });
            layer1.RoleFunctions.Add(new StaTableRoleFunction
            {
                Name = "ReadSensorsMovingAvg",
                Namespace = "DriverInput",
            });
            project.Tabs.Add(layer1);

            // Layer 2 (higher priority)
            var layer2 = new StaTableStateMachine
            {
                LayerName = "Application",
                LayerPriority = 5,
                LayerDescription = "Application layer",
                InitialState = "Idle",
            };
            layer2.States.Add(new StaTableState { Name = "Idle", Type = "initial" });
            layer2.States.Add(new StaTableState { Name = "Executing", Type = "normal" });
            layer2.Events.Add(new StaTableEvent
            {
                Name = "START", Id = 1, DeliveryType = "direct",
                Priority = 0, SourceLayer = "middleware",
            });
            layer2.Transitions.Add(new StaTableTransition
            {
                Source = "Idle", Event = "START",
                Condition = "", Action = "", Target = "Executing",
                Title = "Start",
            });
            project.Tabs.Add(layer2);

            // Global variable
            project.GlobalDefinitions.SystemVariables.Add(new StaTableVariable
            {
                Name = "g_system_tick", Type = "volatile uint32_t", DefaultValue = "0",
            });
            project.GlobalDefinitions.SystemVariables.Add(new StaTableVariable
            {
                Name = "error_code", Type = "uint8_t", DefaultValue = "0",
            });

            return project;
        }

        // ==============================================================
        // 基本
        // ==============================================================

        [Fact]
        public void Generate_NullProject_Throws()
        {
            var gen = MakeGenerator();
            Assert.Throws<ArgumentNullException>(() => gen.Generate(null));
        }

        [Fact]
        public void Generate_MinimalProject_ReturnsNonEmptyString()
        {
            var gen = MakeGenerator();
            var code = gen.Generate(MakeMinimalProject());

            Assert.False(string.IsNullOrEmpty(code));
        }

        // ==============================================================
        // Header
        // ==============================================================

        [Fact]
        public void Generate_Header_ContainsProjectName()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("StaTable project: TestProject", code);
        }

        [Fact]
        public void Generate_Header_ContainsSourceHash()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("Source SHA256: abcdef0123456789", code);
        }

        [Fact]
        public void Generate_Header_ContainsGenerationStyle()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("StaTable generation_style: table_driven", code);
        }

        [Fact]
        public void Generate_Header_ContainsTabList()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("Tabs: 2 (DriverInput, Application)", code);
        }

        [Fact]
        public void Generate_Header_ContainsFixedTimestamp()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("Generated: 2026-01-01T12:00:00", code);
        }

        // ==============================================================
        // Imports
        // ==============================================================

        [Fact]
        public void Generate_ImportsStaTableRuntime()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains(
                "from python_runtime.statable import StaTableControllerBase, StaTableRuntime",
                code);
        }

        // ==============================================================
        // Global variables
        // ==============================================================

        [Fact]
        public void Generate_GlobalVariables_ContainsAllVariables()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("\"g_system_tick\": 0,", code);
            Assert.Contains("\"error_code\": 0,", code);
        }

        // ==============================================================
        // Layer classes
        // ==============================================================

        [Fact]
        public void Generate_LayerClass_NamesAreCorrect()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("class DriverInputController(StaTableControllerBase):", code);
            Assert.Contains("class ApplicationController(StaTableControllerBase):", code);
        }

        [Fact]
        public void Generate_LayerClass_ContainsLayerAttributes()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("LAYER_NAME = \"DriverInput\"", code);
            Assert.Contains("LAYER_PRIORITY = 1", code);
            Assert.Contains("INITIAL_STATE = \"InIdle\"", code);
        }

        [Fact]
        public void Generate_LayerClass_ContainsDescription()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("\"\"\"Input devices layer\"\"\"", code);
        }

        [Fact]
        public void Generate_LayerClass_ContainsTransitions()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("(\"InIdle\", \"SENSOR_TICK\", None, None, \"InSensing\"),", code);
            Assert.Contains("(\"InSensing\", \"ADC_DONE\", None, None, \"InIdle\"),", code);
        }

        [Fact]
        public void Generate_LayerClass_ContainsEvents()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("\"SENSOR_TICK\": {\"id\": 1, \"delivery\": \"queue\"", code);
            Assert.Contains("\"ADC_DONE\": {\"id\": 2, \"delivery\": \"direct\"", code);
        }

        [Fact]
        public void Generate_LayerClass_ContainsInitMethod()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("def __init__(self, comp_id=\"driverinput\"):", code);
            Assert.Contains("comp_type=\"DriverInputController\"", code);
        }

        // ==============================================================
        // Role functions
        // ==============================================================

        [Fact]
        public void Generate_RoleFunction_HasStableUserCodeMarkers()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            Assert.Contains("# STABLE_USER_CODE_START:DriverInput.ReadSensorsMovingAvg", code);
            Assert.Contains("# STABLE_USER_CODE_END:DriverInput.ReadSensorsMovingAvg", code);
        }

        [Fact]
        public void Generate_RoleFunction_IsMethodOnLayerClass()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("def ReadSensorsMovingAvg(self):", code);
        }

        // ==============================================================
        // Runtime builder
        // ==============================================================

        [Fact]
        public void Generate_RuntimeBuilder_LayersArePrioritySorted()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());

            // DriverInput (priority 1) が Application (priority 5) より先に来る
            int idxDriver = code.IndexOf("DriverInputController(),");
            int idxApp = code.IndexOf("ApplicationController(),");

            Assert.True(idxDriver > 0, "DriverInputController が含まれていない");
            Assert.True(idxApp > 0, "ApplicationController が含まれていない");
            Assert.True(idxDriver < idxApp,
                "DriverInput (priority 1) は Application (priority 5) より先であるべき");
        }

        [Fact]
        public void Generate_RuntimeBuilder_PassesGlobalVariables()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("return StaTableRuntime(layers, GLOBAL_VARIABLES)", code);
        }

        // ==============================================================
        // Entry point
        // ==============================================================

        [Fact]
        public void Generate_EntryPoint_ContainsMainGuard()
        {
            var code = MakeGenerator().Generate(MakeMinimalProject());
            Assert.Contains("if __name__ == \"__main__\":", code);
            Assert.Contains("build_runtime()", code);
        }

        // ==============================================================
        // File output
        // ==============================================================

        [Fact]
        public void GenerateToFile_WritesUtf8NoBom()
        {
            var path = Path.Combine(Path.GetTempPath(),
                "psb_gen_" + Guid.NewGuid().ToString("N") + ".py");
            try
            {
                MakeGenerator().GenerateToFile(MakeMinimalProject(), path);

                Assert.True(File.Exists(path));
                var bytes = File.ReadAllBytes(path);
                // BOM なし確認 (先頭3バイトが EF BB BF でない)
                if (bytes.Length >= 3)
                {
                    bool hasBom = bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                    Assert.False(hasBom, "UTF-8 BOM が含まれている");
                }

                var text = File.ReadAllText(path);
                Assert.Contains("class DriverInputController", text);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // ==============================================================
        // 決定論性
        // ==============================================================

        [Fact]
        public void Generate_SameInput_ProducesSameOutput()
        {
            var gen = MakeGenerator();
            var project = MakeMinimalProject();

            var code1 = gen.Generate(project);
            var code2 = gen.Generate(project);

            Assert.Equal(code1, code2);
        }

        // ==============================================================
        // バックスラッシュ エスケープ問題の回帰テスト
        // C:\Users\... が Python docstring 内で \U として解釈される問題を防ぐ
        // ==============================================================

        [Fact]
        public void Generate_SourceFilePathWithBackslashes_NoRawBackslashInHeader()
        {
            var project = MakeMinimalProject();
            project.SourceFilePath = @"C:\Users\user\path\to\file.xml";

            var code = MakeGenerator().Generate(project);

            // ヘッダに生のバックスラッシュが残っていないこと
            Assert.DoesNotContain(@"C:\Users", code);
            // フォワードスラッシュに置換されていること
            Assert.Contains("C:/Users/user/path/to/file.xml", code);
        }

        [Fact]
        public void Generate_SourceFilePathNull_DoesNotThrow()
        {
            var project = MakeMinimalProject();
            project.SourceFilePath = null;

            var code = MakeGenerator().Generate(project);
            Assert.False(string.IsNullOrEmpty(code));
        }

        [Fact]
        public void Generate_SourceFilePathUnixStyle_Unchanged()
        {
            var project = MakeMinimalProject();
            project.SourceFilePath = "/home/user/path/file.xml";

            var code = MakeGenerator().Generate(project);
            Assert.Contains("/home/user/path/file.xml", code);
        }
    }
}