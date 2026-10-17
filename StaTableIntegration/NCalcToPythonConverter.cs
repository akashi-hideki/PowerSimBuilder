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
    /// Phase 2-B'': 三項演算子 (a ? b : c) に対応。
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

            // 7. 三項演算子 a ? b : c → (b if a else c)
            masked = ConvertTernary(masked);

            // 8. 連続空白の正規化（文字列リテラルはマスク済みなので安全）
            masked = WhitespaceRegex.Replace(masked, " ").Trim();

            // 9. 文字列リテラルを復元
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

        // ==============================================================
        // 三項演算子: a ? b : c → (b if a else c)
        // 右結合: a ? b : c ? d : e は a ? b : (c ? d : e) と解釈
        // ==============================================================

        private static string ConvertTernary(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return expr;
            expr = expr.Trim();
            if (expr.Length == 0) return expr;

            int q = FindTopLevelQuestionMark(expr);
            if (q < 0)
            {
                // トップレベルに三項演算子なし。括弧内を再帰処理
                return RecurseIntoParens(expr);
            }

            int c = FindMatchingColon(expr, q + 1);
            if (c < 0) return expr;  // 対応する : が無い → そのまま返す

            string condRaw = expr.Substring(0, q).Trim();
            string thenRaw = expr.Substring(q + 1, c - q - 1).Trim();
            string elseRaw = expr.Substring(c + 1).Trim();

            if (condRaw.Length == 0 || thenRaw.Length == 0 || elseRaw.Length == 0)
                return expr;  // 空オペランドは構文エラー扱い

            // 各オペランドを再帰変換（右結合）
            string cond  = ConvertTernary(condRaw);
            string thenB = ConvertTernary(thenRaw);
            string elseB = ConvertTernary(elseRaw);

            return "(" + thenB + " if " + cond + " else " + elseB + ")";
        }

        private static int FindTopLevelQuestionMark(string expr)
        {
            int depth = 0;
            for (int i = 0; i < expr.Length; i++)
            {
                char ch = expr[i];
                if (ch == '(') depth++;
                else if (ch == ')') depth--;
                else if (ch == '?' && depth == 0) return i;
            }
            return -1;
        }

        private static int FindMatchingColon(string expr, int start)
        {
            int depthParen = 0;
            int depthTernary = 0;
            for (int i = start; i < expr.Length; i++)
            {
                char ch = expr[i];
                if (ch == '(') depthParen++;
                else if (ch == ')') depthParen--;
                else if (depthParen == 0)
                {
                    if (ch == '?') depthTernary++;
                    else if (ch == ':')
                    {
                        if (depthTernary == 0) return i;
                        depthTernary--;
                    }
                }
            }
            return -1;
        }

        private static string RecurseIntoParens(string expr)
        {
            var sb = new StringBuilder(expr.Length);
            int i = 0;
            while (i < expr.Length)
            {
                if (expr[i] == '(')
                {
                    int depth = 1;
                    int j = i + 1;
                    while (j < expr.Length && depth > 0)
                    {
                        if (expr[j] == '(') depth++;
                        else if (expr[j] == ')') depth--;
                        j++;
                    }
                    if (depth == 0)
                    {
                        string inner = expr.Substring(i + 1, j - i - 2);
                        string converted = ConvertTernary(inner);
                        if (IsBalancedParens(converted))
                            sb.Append(converted);
                        else
                        {
                            sb.Append('(');
                            sb.Append(converted);
                            sb.Append(')');
                        }
                        i = j;
                        continue;
                    }
                }
                sb.Append(expr[i]);
                i++;
            }
            return sb.ToString();
        }

        private static bool IsBalancedParens(string s)
        {
            if (s.Length < 2 || s[0] != '(' || s[s.Length - 1] != ')') return false;
            int depth = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == '(') depth++;
                else if (s[i] == ')')
                {
                    depth--;
                    if (depth == 0 && i < s.Length - 1) return false;
                }
            }
            return depth == 0;
        }
    }
}