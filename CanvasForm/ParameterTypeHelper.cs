using System;
using System.Globalization;
using System.Text.Json;

namespace PowerSimBuilder
{
    /// <summary>
    /// ParameterDescriptor.Type から型カテゴリを判定し、
    /// テキストを型付きオブジェクトに変換するヘルパ。
    ///
    /// 背景 (Task 3-C-9-a):
    /// - Type が空/未知の場合、ParametersEditorForm で値が文字列化されていた
    /// - BuildEditor / ReadValue の型判定ロジック重複を解消
    /// </summary>
    public enum ParameterTypeCategory
    {
        Integer,
        Number,
        Boolean,
        String,
    }

    public static class ParameterTypeHelper
    {
        /// <summary>
        /// type 文字列（と必要なら現在値）から型カテゴリを決定する。
        /// 1. type 文字列が既知の型名を含む → そのカテゴリ
        /// 2. type が空 or 未知 → currentValue の .NET 型から推論
        /// 3. どちらも決まらない → String
        /// </summary>
        public static ParameterTypeCategory Classify(string type, object currentValue)
        {
            var t = (type ?? string.Empty).Trim().ToLowerInvariant();

            if (t.Length > 0)
            {
                if (t.Contains("bool") || t.Contains("flag") || t == "bit")
                    return ParameterTypeCategory.Boolean;

                if (t.Contains("int") || t.Contains("long") || t.Contains("short")
                    || t.Contains("byte") || t.Contains("uint"))
                    return ParameterTypeCategory.Integer;

                if (t.Contains("double") || t.Contains("float") || t.Contains("decimal")
                    || t == "number" || t == "single" || t == "real" || t == "numeric")
                    return ParameterTypeCategory.Number;

                if (t.Contains("string") || t.Contains("text") || t.Contains("char"))
                    return ParameterTypeCategory.String;
            }

            return InferFromValue(currentValue);
        }

        /// <summary>
        /// TextBox 入力をカテゴリに応じた .NET 型に変換する。
        /// Integer -> long, Number -> double, Boolean -> bool, String -> string
        /// </summary>
        public static object ParseText(string text, ParameterTypeCategory category)
        {
            text = text ?? string.Empty;
            switch (category)
            {
                case ParameterTypeCategory.Boolean:
                    return text == "1"
                        || string.Equals(text, "true", StringComparison.OrdinalIgnoreCase);

                case ParameterTypeCategory.Integer:
                    long lv;
                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out lv))
                        return lv;
                    throw new FormatException("整数として解釈できません: '" + text + "'");

                case ParameterTypeCategory.Number:
                    double dv;
                    if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out dv))
                        return dv;
                    if (double.TryParse(text, out dv))
                        return dv;
                    throw new FormatException("小数として解釈できません: '" + text + "'");

                default:
                    return text;
            }
        }

        private static ParameterTypeCategory InferFromValue(object value)
        {
            if (value == null) return ParameterTypeCategory.String;
            if (value is bool) return ParameterTypeCategory.Boolean;
            if (value is int || value is long || value is short || value is byte)
                return ParameterTypeCategory.Integer;
            if (value is float || value is double || value is decimal)
                return ParameterTypeCategory.Number;
            if (value is string) return ParameterTypeCategory.String;

            if (value is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return ParameterTypeCategory.Boolean;
                    case JsonValueKind.Number:
                        return ParameterTypeCategory.Number;
                    case JsonValueKind.String:
                        return ParameterTypeCategory.String;
                }
            }

            return ParameterTypeCategory.String;
        }
    }
}