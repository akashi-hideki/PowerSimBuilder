using System.Globalization;
using System.Text.RegularExpressions;

namespace PowerSimBuilder
{
    /// <summary>
    /// Overrides t_end = X in generated Python code. Testable without WinForms.
    /// </summary>
    public static class SimTimeOverride
    {
        public static string Apply(string code, double tEnd)
        {
            if (string.IsNullOrEmpty(code)) return code;
            string val = tEnd.ToString(CultureInfo.InvariantCulture);
            return Regex.Replace(
                code,
                @"^(\s*)t_end\s*=\s*[\d.]+",
                m => $"{m.Groups[1].Value}t_end = {val}",
                RegexOptions.Multiline);
        }
    }
}