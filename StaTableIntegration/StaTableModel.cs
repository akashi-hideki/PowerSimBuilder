using System;
using System.Collections.Generic;

namespace PowerSimBuilder
{
    // ==================================================================
    // StaTable XML モデル
    // 対応バージョン: v2.2 〜 v3.8 (canonical XML)
    // ==================================================================

    /// <summary>StaTable プロジェクト全体</summary>
    public class StaTableProject
    {
        public string Name { get; set; }
        public StaTableProjectSettings Settings { get; set; } = new StaTableProjectSettings();
        public StaTableGlobalDefinitions GlobalDefinitions { get; set; } = new StaTableGlobalDefinitions();
        public StaTableSharedLibraries SharedLibraries { get; set; } = new StaTableSharedLibraries();
        public List<StaTableStateMachine> Tabs { get; set; } = new List<StaTableStateMachine>();

        /// <summary>元 XML の SHA256 (生成コードに埋め込む)</summary>
        public string SourceXmlHash { get; set; }

        /// <summary>元 XML のファイルパス</summary>
        public string SourceFilePath { get; set; }
    }

    /// <summary>ProjectSettings / CodeGeneration 要素</summary>
    public class StaTableProjectSettings
    {
        public string ProjectName { get; set; }
        public string TableType { get; set; }             // "array"
        public string GenerationStyle { get; set; }       // "table_driven"
        public string OsType { get; set; }                // "non_rtos" / "freertos"
        public string FolderStructure { get; set; }       // "by_layer" / "by_type"
        public bool GenerateSuperInclude { get; set; }
        public string SuperIncludeFile { get; set; }
        public int MaxConsecutivePendingEvents { get; set; } = 16;
    }

    // ==================================================================
    // GlobalDefinitions
    // ==================================================================

    public class StaTableGlobalDefinitions
    {
        public List<StaTableVariable> SystemVariables { get; set; } = new List<StaTableVariable>();
        public List<StaTableEventFlag> EventFlags { get; set; } = new List<StaTableEventFlag>();
        public List<StaTableInterrupt> Interrupts { get; set; } = new List<StaTableInterrupt>();
        public List<StaTableTimer> TimerBase { get; set; } = new List<StaTableTimer>();
    }

    public class StaTableVariable
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Unit { get; set; }
        public string DefaultValue { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public int ArraySize { get; set; }
    }

    public class StaTableEventFlag
    {
        public string Name { get; set; }
        public int MinValue { get; set; }
        public int MaxValue { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
    }

    public class StaTableInterrupt
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string EventNames { get; set; }
        public bool IsTimer { get; set; }
        public string Title { get; set; }
        public List<StaTableInterruptAction> Actions { get; set; } = new List<StaTableInterruptAction>();
        public List<string> UsedVariables { get; set; } = new List<string>();
    }

    public class StaTableInterruptAction
    {
        public string Condition { get; set; }
        public string Action { get; set; }
    }

    public class StaTableTimer
    {
        public string VariableName { get; set; }
        public string Unit { get; set; }
        public string DataType { get; set; }
        public string Title { get; set; }
        public string InterruptName { get; set; }
    }

    // ==================================================================
    // SharedLibraries
    // ==================================================================

    public class StaTableSharedLibraries
    {
        public List<StaTableRoleFunction> RoleFunctionLibrary { get; set; } = new List<StaTableRoleFunction>();
        // ConditionLibrary / LiteralLibrary は将来対応
    }

    /// <summary>RoleFunction (共有ライブラリ側 / 層側 共通)</summary>
    public class StaTableRoleFunction
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ReturnType { get; set; } = "int";
        public string Arg1Type { get; set; }
        public string Arg1Name { get; set; }
        public string Arg2Type { get; set; }
        public string Arg2Name { get; set; }

        // v3.8+ 属性
        public List<string> UsedGlobalVars { get; set; } = new List<string>();
        public List<string> UsedEvents { get; set; } = new List<string>();
        public List<string> UsedLiterals { get; set; } = new List<string>();

        /// <summary>"Namespace.Name" 形式の完全修飾名</summary>
        public string FullyQualifiedName
        {
            get
            {
                if (string.IsNullOrEmpty(Namespace)) return Name;
                return Namespace + "." + Name;
            }
        }
    }

    // ==================================================================
    // StateMachine (Tab = 層)
    // ==================================================================

    public class StaTableStateMachine
    {
        public string LayerName { get; set; }
        public int LayerPriority { get; set; } = 5;
        public string LayerDescription { get; set; }
        public string InitialState { get; set; }

        public List<StaTableState> States { get; set; } = new List<StaTableState>();
        public List<StaTableEvent> Events { get; set; } = new List<StaTableEvent>();
        public List<StaTableTransition> Transitions { get; set; } = new List<StaTableTransition>();
        public List<StaTableRoleFunction> RoleFunctions { get; set; } = new List<StaTableRoleFunction>();

        // v2.2: セル単位メタデータ
        // キーは "source|event" 形式 (タプル回避のため文字列化)
        public Dictionary<string, List<StaTableCellAction>> CellActions { get; set; }
            = new Dictionary<string, List<StaTableCellAction>>();
        public Dictionary<string, List<StaTableCellRelation>> CellRelations { get; set; }
            = new Dictionary<string, List<StaTableCellRelation>>();
    }

    public class StaTableState
    {
        public string Name { get; set; }
        public string Type { get; set; }          // "initial" / "normal" / "final"
        public string Parent { get; set; }        // 階層状態の親 (空なら独立)
        public string Do { get; set; }            // 予約フィールド (v3.7+ 非表示)
        public string Description { get; set; }
    }

    public class StaTableEvent
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public string Kind { get; set; }              // "signal" 等
        public string Params { get; set; }
        public int Priority { get; set; }
        public string Description { get; set; }
        public string DeliveryType { get; set; }      // "direct" / "queue"
        public string SourceLayer { get; set; }       // "driver" / "middleware"
        public string DataType { get; set; }
        public string DataName { get; set; }
        public string Title { get; set; }
    }

    public class StaTableTransition
    {
        public string Source { get; set; }
        public string Event { get; set; }
        public string Condition { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string TransitionType { get; set; }    // "external" 等
        public string Title { get; set; }
        public bool HasElse { get; set; }
        public string ElseTarget { get; set; }
        public bool EarlyReturn { get; set; }
        public string Label { get; set; }             // "T1" 等
    }

    /// <summary>v2.2: セル単位のアクション</summary>
    public class StaTableCellAction
    {
        public string Kind { get; set; }              // "assign" / "call" 等
        public string Target { get; set; }
        public string Value { get; set; }
        public string RoleFunction { get; set; }      // FQName
    }

    /// <summary>v2.2: セル単位の関係</summary>
    public class StaTableCellRelation
    {
        public string Kind { get; set; }
        public string RoleFunction { get; set; }
        public string Description { get; set; }
    }

    // ==================================================================
    // パース結果 (警告・エラーの記録用)
    // ==================================================================

    public class StaTableParseResult
    {
        public StaTableProject Project { get; set; }
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();

        public bool IsUsable
        {
            get { return Errors.Count == 0 && Project != null; }
        }
    }
}