using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace PowerSimBuilder
{
    /// <summary>
    /// StaTable/ncalc 形式の条件式・アクション式を Python 式文字列に変換する。
    ///
    /// Phase 2-B: 論理演算子・単項否定・変数参照・RoleFunction 呼び出しに対応。
    /// 三項演算子 (a ? b : c) は未対応（Phase 2-B' で対応予定）。
    /// </summary>
    public static class NCalcToPythonConverter
    {
        private const string GlobalPrefix = "g_";
        private const string RoleFuncPrefix = "F_";

        // 単項否定 ! を検出（直前が = < > ! でなく、直後が = でない）
        private static readonly Regex UnaryNotRegex =
            new Regex(@"(?<![=<>!])!(?!=)", RegexOptions.Compiled);

        // g_xxx 単独参照（識別子境界付き）
        private static readonly Regex GlobalVarRegex =
            new Regex(@"\b(g_[A-Za-z0-9_]+)\b", RegexOptions.Compiled);

        // F_xxx 単独参照
        private static readonly Regex RoleFuncRegex =
            new Regex(@"\b(F_[A-Za-z0-9_]+)\b", RegexOptions.Compiled);

        // 真偽値・null リテラル
        private static readonly Regex BoolNullRegex =
            new Regex(@"\b(true|false|null)\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // 連続空白の正規化（マスク後の文字列に適用）
        private static readonly Regex WhitespaceRegex =
            new Regex(@"\s+", RegexOptions.Compiled);

        // ==============================================================
        // Public API
        // ==============================================================

        public static string ConvertCondition(string ncalcExpr)
        {
            return ConvertCore(ncalcExpr);
        }

        public static string ConvertAction(string ncalcExpr)
        {
            return ConvertCore(ncalcExpr);
        }

        // ==============================================================
        // Core
        // ==============================================================

        private static string ConvertCore(string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return expr;

            // 1. 文字列リテラルを退避
            var literals = new List<string>();
            var masked = MaskStringLiterals(expr, literals);

            // 2. 単項否定: !x -> not x
            masked = UnaryNotRegex.Replace(masked, " not ");

            // 3. 論理演算子
            masked = masked.Replace("&&", " and ");
            masked = masked.Replace("||", " or ");

            // 4. 真偽値・null リテラル
            masked = BoolNullRegex.Replace(masked, m =>
            {
                var token = m.Value.ToLowerInvariant();
                switch (token)
                {
                    case "true":  return "True";
                    case "false": return "False";
                    case "null":  return "None";
                    default:      return m.Value;
                }
            });

            // 5. 変数参照
            masked = GlobalVarRegex.Replace(masked,
                m => "self.globals[\"" + m.Groups[1].Value + "\"]");

            // 6. RoleFunction 呼び出し
            masked = RoleFuncRegex.Replace(masked,
                m => "self." + m.Groups[1].Value);

            // 7. 連続空白の正規化（文字列リテラルはマスク済みなので安全）
            masked = WhitespaceRegex.Replace(masked, " ").Trim();

            // 8. 文字列リテラルを復元
            masked = RestoreStringLiterals(masked, literals);

            return masked;
        }

        // ==============================================================
        // String literal masking
        // ==============================================================

        private const char MaskStart = '\uE000';
        private const char MaskEnd   = '\uE001';

        private static string MaskStringLiterals(string s, List<string> literals)
        {
            var sb = new StringBuilder(s.Length);
            int i = 0;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '"' || c == '\'')
                {
                    char quote = c;
                    int start = i;
                    i++;
                    while (i < s.Length)
                    {
                        if (s[i] == '\\' && i + 1 < s.Length)
                        {
                            i += 2;
                            continue;
                        }
                        if (s[i] == quote)
                        {
                            i++;
                            break;
                        }
                        i++;
                    }
                    var literal = s.Substring(start, i - start);
                    int idx = literals.Count;
                    literals.Add(literal);
                    sb.Append(MaskStart);
                    sb.Append(idx.ToString());
                    sb.Append(MaskEnd);
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }
            return sb.ToString();
        }

        private static string RestoreStringLiterals(string masked, List<string> literals)
        {
            var sb = new StringBuilder(masked.Length);
            int i = 0;
            while (i < masked.Length)
            {
                if (masked[i] == MaskStart)
                {
                    int j = i + 1;
                    while (j < masked.Length && masked[j] != MaskEnd)
                        j++;
                    var numStr = masked.Substring(i + 1, j - i - 1);
                    int idx;
                    if (int.TryParse(numStr, out idx) && idx >= 0 && idx < literals.Count)
                        sb.Append(literals[idx]);
                    i = (j < masked.Length) ? j + 1 : j;
                }
                else
                {
                    sb.Append(masked[i]);
                    i++;
                }
            }
            return sb.ToString();
        }
    }
}