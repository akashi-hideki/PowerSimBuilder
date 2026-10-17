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
    }
}