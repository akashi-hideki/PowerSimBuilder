using System;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.StaTable
{
    /// <summary>
    /// SPEC docs/SPEC_PORT_SELECTION.md §4 Auto-suggest ヒューリスティックの検証。
    /// SuggestAsInput/SuggestAsOutput/IsExcluded は internal static。
    /// </summary>
    public class PortSelectorControlTests
    {
        // ---- §4.1 Input 候補 ----
        [Theory]
        [InlineData("thermistor_top",     true)]
        [InlineData("thermistor_bottom",  true)]
        [InlineData("steam_sensor",       true)]
        [InlineData("oven_current_temp",  true)]
        [InlineData("mw_anode_current",   true)]
        [InlineData("door_open",          true)]
        [InlineData("heater_command",     true)]
        [InlineData("temp_setpoint",      true)]
        [InlineData("oven_target_temp",   true)]
        [InlineData("heater_top_duty",    false)]  // output パターン
        [InlineData("fan_duty",           false)]  // output パターン
        [InlineData("error_code",         false)]  // output パターン
        [InlineData("g_system_tick",      false)]  // 除外
        [InlineData("rx_buf",             false)]  // 除外
        [InlineData("seq_init",           false)]  // 除外
        [InlineData("step_count",         false)]  // 除外
        [InlineData("stage_heat",         false)]  // 除外
        public void SuggestAsInput_MatchesSpec(string name, bool expected)
            => Assert.Equal(expected, PortSelectorControl.SuggestAsInput(name));

        // ---- §4.2 Output 候補 ----
        [Theory]
        [InlineData("heater_top_duty", true)]
        [InlineData("heater_middle_duty", true)]
        [InlineData("fan_duty",        true)]
        [InlineData("heater_top_on",   true)]
        [InlineData("pump_off",        true)]
        [InlineData("mw_power",        true)]
        [InlineData("error_code",      true)]
        [InlineData("motor_fault",     true)]
        [InlineData("g_system_tick",   false)] // 除外
        [InlineData("rx_buf",          false)] // 除外
        [InlineData("thermistor_top",  false)] // input パターン
        [InlineData("door_open",       false)] // input パターン
        public void SuggestAsOutput_MatchesSpec(string name, bool expected)
            => Assert.Equal(expected, PortSelectorControl.SuggestAsOutput(name));

        // ---- §4.3 除外パターン ----
        [Theory]
        [InlineData("g_system_tick", true)]
        [InlineData("rx_buf",        true)]
        [InlineData("step_count",    true)]
        [InlineData("seq_init",      true)]
        [InlineData("seq_run",       true)]
        [InlineData("stage_heat",    true)]
        [InlineData("stage_cool",    true)]
        [InlineData("heater_top_duty", false)]
        [InlineData("error_code",    false)]
        [InlineData("thermistor_top", false)]
        [InlineData("",              true)]  // 空文字
        [InlineData(null,            true)]  // null
        public void IsExcluded_MatchesSpec(string name, bool expected)
            => Assert.Equal(expected, PortSelectorControl.IsExcluded(name));

        // ---- 相互排他性（Input/Output のいずれか一方のみ true） ----
        [Theory]
        [InlineData("thermistor_top")]
        [InlineData("heater_top_duty")]
        [InlineData("door_open")]
        [InlineData("fan_duty")]
        [InlineData("error_code")]
        [InlineData("heater_command")]
        public void SuggestAsInput_And_SuggestAsOutput_AreMutuallyExclusive(string name)
        {
            var asIn  = PortSelectorControl.SuggestAsInput(name);
            var asOut = PortSelectorControl.SuggestAsOutput(name);
            Assert.False(asIn && asOut, $"{name} cannot be both input and output");
        }

        // ---- 除外パターンは Input/Output 両方 false ----
        [Theory]
        [InlineData("g_system_tick")]
        [InlineData("rx_buf")]
        [InlineData("step_count")]
        [InlineData("seq_init")]
        [InlineData("stage_heat")]
        public void Excluded_IsNeitherInputNorOutput(string name)
        {
            Assert.False(PortSelectorControl.SuggestAsInput(name));
            Assert.False(PortSelectorControl.SuggestAsOutput(name));
        }
    }
}