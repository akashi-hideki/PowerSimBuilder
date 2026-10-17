using System.Collections.Generic;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// NCalcToPythonConverter の変換ロジック検証。
    /// </summary>
    public class NCalcToPythonConverterTests
    {
        // ==============================================================
        // 基本
        // ==============================================================

        [Fact]
        public void ConvertCondition_Null_ReturnsNull()
        {
            Assert.Null(NCalcToPythonConverter.ConvertCondition(null));
        }

        [Fact]
        public void ConvertCondition_Empty_ReturnsEmpty()
        {
            Assert.Equal("", NCalcToPythonConverter.ConvertCondition(""));
        }

        // ==============================================================
        // 論理演算子
        // ==============================================================

        [Fact]
        public void ConvertCondition_LogicalAnd()
        {
            Assert.Equal("a and b",
                NCalcToPythonConverter.ConvertCondition("a && b"));
        }

        [Fact]
        public void ConvertCondition_LogicalOr()
        {
            Assert.Equal("a or b",
                NCalcToPythonConverter.ConvertCondition("a || b"));
        }

        [Fact]
        public void ConvertCondition_UnaryNot()
        {
            Assert.Equal("not flag",
                NCalcToPythonConverter.ConvertCondition("!flag"));
        }

        [Fact]
        public void ConvertCondition_NotEqual_Unchanged()
        {
            Assert.Equal("a != b",
                NCalcToPythonConverter.ConvertCondition("a != b"));
        }

        [Fact]
        public void ConvertCondition_NotBeforeAnd()
        {
            Assert.Equal("not a and b",
                NCalcToPythonConverter.ConvertCondition("!a && b"));
        }

        [Fact]
        public void ConvertCondition_LessThanOrEqual_Unchanged()
        {
            Assert.Equal("a <= b",
                NCalcToPythonConverter.ConvertCondition("a <= b"));
        }

        // ==============================================================
        // 変数・関数
        // ==============================================================

        [Fact]
        public void ConvertCondition_GlobalVariable()
        {
            Assert.Equal("self.globals[\"g_counter\"] > 5",
                NCalcToPythonConverter.ConvertCondition("g_counter > 5"));
        }

        [Fact]
        public void ConvertCondition_RoleFunctionCall()
        {
            Assert.Equal("self.F_heater_on()",
                NCalcToPythonConverter.ConvertCondition("F_heater_on()"));
        }

        // ==============================================================
        // リテラル
        // ==============================================================

        [Fact]
        public void ConvertCondition_TrueLiteral()
        {
            Assert.Equal("True",
                NCalcToPythonConverter.ConvertCondition("true"));
        }

        [Fact]
        public void ConvertCondition_FalseLiteral()
        {
            Assert.Equal("False",
                NCalcToPythonConverter.ConvertCondition("false"));
        }

        [Fact]
        public void ConvertCondition_NullLiteral()
        {
            Assert.Equal("None",
                NCalcToPythonConverter.ConvertCondition("null"));
        }

        // ==============================================================
        // 文字列リテラル保護
        // ==============================================================

        [Fact]
        public void ConvertCondition_StringLiteralWithAnd_NotConverted()
        {
            Assert.Equal("\"a && b\"",
                NCalcToPythonConverter.ConvertCondition("\"a && b\""));
        }

        [Fact]
        public void ConvertCondition_StringLiteralWithBang_NotConverted()
        {
            Assert.Equal("\"!flag\"",
                NCalcToPythonConverter.ConvertCondition("\"!flag\""));
        }

        [Fact]
        public void ConvertCondition_StringLiteralWithTrue_NotConverted()
        {
            Assert.Equal("\"true\"",
                NCalcToPythonConverter.ConvertCondition("\"true\""));
        }

        // ==============================================================
        // 複合式
        // ==============================================================

        [Fact]
        public void ConvertCondition_ComplexExpression()
        {
            var result = NCalcToPythonConverter.ConvertCondition(
                "g_counter > 10 && F_heater_on() && !g_error");
            Assert.Equal(
                "self.globals[\"g_counter\"] > 10 and self.F_heater_on() and not self.globals[\"g_error\"]",
                result);
        }

        [Fact]
        public void ConvertCondition_WithStringCompare()
        {
            var result = NCalcToPythonConverter.ConvertCondition(
                "g_state == \"running\" && g_counter > 0");
            Assert.Equal(
                "self.globals[\"g_state\"] == \"running\" and self.globals[\"g_counter\"] > 0",
                result);
        }

        // ==============================================================
        // Action
        // ==============================================================

        [Fact]
        public void ConvertAction_Assignment()
        {
            Assert.Equal("self.globals[\"g_counter\"] = 5",
                NCalcToPythonConverter.ConvertAction("g_counter = 5"));
        }

        [Fact]
        public void ConvertAction_FunctionCall()
        {
            Assert.Equal("self.F_heater_on()",
                NCalcToPythonConverter.ConvertAction("F_heater_on()"));
        }

        [Fact]
        public void ConvertAction_Null()
        {
            Assert.Null(NCalcToPythonConverter.ConvertAction(null));
        }

        // ==============================================================
        // 三項演算子 (Phase 2-B'')
        // ==============================================================

        [Fact]
        public void ConvertCondition_SimpleTernary()
        {
            Assert.Equal("(1 if a > 0 else 0)",
                NCalcToPythonConverter.ConvertCondition("a > 0 ? 1 : 0"));
        }

        [Fact]
        public void ConvertCondition_NestedTernaryElse()
        {
            // a ? b : (c ? d : e) — 右結合
            Assert.Equal("(b if a else (d if c else e))",
                NCalcToPythonConverter.ConvertCondition("a ? b : c ? d : e"));
        }

        [Fact]
        public void ConvertCondition_NestedTernaryThen()
        {
            // (a ? (b ? c : d) : e) — 条件部に三項演算子
            Assert.Equal("((c if b else d) if a else e)",
                NCalcToPythonConverter.ConvertCondition("a ? b ? c : d : e"));
        }

        [Fact]
        public void ConvertCondition_TernaryWithLogical()
        {
            Assert.Equal("(self.F_a() if x and y else self.F_b())",
                NCalcToPythonConverter.ConvertCondition("x && y ? F_a() : F_b()"));
        }

        [Fact]
        public void ConvertCondition_TernaryWithArithmetic()
        {
            Assert.Equal("(b + 1 if a > 0 else c * 2)",
                NCalcToPythonConverter.ConvertCondition("a > 0 ? b + 1 : c * 2"));
        }

        [Fact]
        public void ConvertCondition_TernaryInParen()
        {
            Assert.Equal("(b if a else c) > 0",
                NCalcToPythonConverter.ConvertCondition("(a ? b : c) > 0"));
        }

        [Fact]
        public void ConvertCondition_TernaryInString_NotConverted()
        {
            Assert.Equal("\"a?b:c\"",
                NCalcToPythonConverter.ConvertCondition("\"a?b:c\""));
        }

        [Fact]
        public void ConvertCondition_NoTernary_Unchanged()
        {
            Assert.Equal("a > 0",
                NCalcToPythonConverter.ConvertCondition("a > 0"));
        }

        // ==============================================================
        // B'-bare: 裸関数呼び出しの自動 F_ 補完
        // ==============================================================

        [Fact]
        public void ConvertCondition_BareCallWithContext_PrefixedWithWarning()
        {
            var warnings = new List<string>();
            var known = new HashSet<string>(new[] { "F_HeaterOn" });

            var result = NCalcToPythonConverter.ConvertCondition(
                "HeaterOn() > 0", known, "DriverOutput", warnings);

            Assert.Contains("self.F_HeaterOn", result);
            Assert.DoesNotContain("self.HeaterOn", result);
            Assert.Single(warnings);
            Assert.Contains("B'-bare", warnings[0]);
            Assert.Contains("HeaterOn", warnings[0]);
            Assert.Contains("defined elsewhere", warnings[0]);
        }

        [Fact]
        public void ConvertCondition_BareCallUnknown_PrefixedWithWarning()
        {
            var warnings = new List<string>();
            var known = new HashSet<string>();

            var result = NCalcToPythonConverter.ConvertCondition(
                "UnknownFn() > 0", known, "Layer", warnings);

            Assert.Contains("self.F_UnknownFn", result);
            Assert.Single(warnings);
            Assert.Contains("B'-bare", warnings[0]);
            Assert.Contains("UnknownFn", warnings[0]);
            Assert.Contains("not defined", warnings[0]);
        }

        [Fact]
        public void ConvertCondition_KnownBuiltin_NotPrefixedNoWarning()
        {
            var warnings = new List<string>();
            var known = new HashSet<string>();

            var result = NCalcToPythonConverter.ConvertCondition(
                "Abs(0) > 0", known, "Layer", warnings);

            Assert.Contains("Abs(", result);
            Assert.DoesNotContain("F_Abs", result);
            Assert.Empty(warnings);
        }

        [Fact]
        public void ConvertCondition_AlreadyF_NotDoublePrefixed()
        {
            var warnings = new List<string>();
            var known = new HashSet<string>(new[] { "F_HeaterOn" });

            var result = NCalcToPythonConverter.ConvertCondition(
                "F_HeaterOn() > 0", known, "Layer", warnings);

            Assert.Contains("self.F_HeaterOn", result);
            Assert.DoesNotContain("F_F_HeaterOn", result);
        }

        [Fact]
        public void ConvertCondition_NoContext_BareCallUnchanged()
        {
            var result = NCalcToPythonConverter.ConvertCondition("HeaterOn() > 0");
            Assert.Contains("HeaterOn()", result);
            Assert.DoesNotContain("F_HeaterOn", result);
        }
    }
}