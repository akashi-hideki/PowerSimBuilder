using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Pythonスクリプト実行クラス
    /// </summary>
    public class PythonExecutor
    {
        /// <summary>
        /// Pythonスクリプトを実行するメソッドのデリゲート
        /// </summary>
        /// <param name="scriptPath">実行するPythonスクリプトのパス</param>
        /// <returns>実行が成功した場合はtrue、失敗した場合はfalse</returns>
        public delegate bool ExecutePythonScriptDelegate(string scriptPath);

        /// <summary>
        /// Python実行メソッド
        /// </summary>
        public ExecutePythonScriptDelegate ExecuteMethod { get; private set; }

        // Task 3-C-12: 実行中プロセスの参照（HasExited で生存判定）
        private static Process _lastProcess = null;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public PythonExecutor(ExecutePythonScriptDelegate executeMethod = null)
        {
            ExecuteMethod = executeMethod ?? DefaultExecutePythonScript;
        }

        /// <summary>
        /// デフォルトのPython実行メソッド（Processを使用）
        /// </summary>
        private bool DefaultExecutePythonScript(string scriptPath)
        {
            try
            {
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show(I18n.T("PythonExecutor.MsgScriptNotFound", scriptPath), I18n.T("Common.MsgTitleError"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var process = new Process();
                process.StartInfo.FileName = "python";
                process.StartInfo.Arguments = $"\"{scriptPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                // Task 3-C-11: GUI 起動時の作業ディレクトリ
                var scriptDir = Path.GetDirectoryName(scriptPath);
                if (!string.IsNullOrEmpty(scriptDir))
                    process.StartInfo.WorkingDirectory = scriptDir;

                // Task 3-C-11: PYTHONPATH に src/ を追加
                var srcDir = FindSrcDirectory();
                if (!string.IsNullOrEmpty(srcDir))
                {
                    var existing = Environment.GetEnvironmentVariable("PYTHONPATH") ?? "";
                    process.StartInfo.EnvironmentVariables["PYTHONPATH"] =
                        string.IsNullOrEmpty(existing) ? srcDir : srcDir + Path.PathSeparator + existing;
                }

                var stderrSb = new StringBuilder();

                process.OutputDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($"出力: {e.Data}");
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine($"エラー: {e.Data}");
                        lock (stderrSb) { stderrSb.AppendLine(e.Data); }
                    }
                };

                process.EnableRaisingEvents = true;
                process.Exited += (s, e) =>
                {
                    try
                    {
                        var exitCode = -1;
                        try { exitCode = process.ExitCode; } catch { }

                        string errTail;
                        lock (stderrSb) { errTail = stderrSb.ToString(); }

                        var msg = exitCode == 0
                            ? I18n.T("PythonExecutor.MsgCompleted") : I18n.T("PythonExecutor.MsgExitedWithCode", exitCode) + "" +
                              (string.IsNullOrEmpty(errTail)
                                  ? ""
                                  : errTail.Substring(0, Math.Min(500, errTail.Length)));

                        if (Application.OpenForms.Count > 0)
                        {
                            var form = Application.OpenForms[0];
                            form.BeginInvoke((Action)(() =>
                                MessageBox.Show(msg, I18n.T("Common.MsgTitleInfo"),
                                    MessageBoxButtons.OK,
                                    exitCode == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning)));
                        }
                    }
                    catch { /* best-effort */ }
                };

                _lastProcess = process;
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(I18n.T("PythonExecutor.MsgExecFail", ex.Message),
                    I18n.T("Common.MsgTitleError"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Pythonスクリプトを実行する
        /// Task 3-C-12: HasExited ベースの二重起動防止（フラグ固着なし）
        /// </summary>
        public bool Run(string scriptPath)
        {
            // 前回プロセスがまだ生きていれば拒否。終了していれば許可。
            var prev = _lastProcess;
            if (prev != null)
            {
                bool alive;
                try { alive = !prev.HasExited; }
                catch { alive = false; }  // 破棄済み等は終了扱い
                if (alive)
                {
                    MessageBox.Show(
                        I18n.T("PythonExecutor.MsgStillRunning"),
                        I18n.T("Common.MsgTitleInfo"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }
            }
            return ExecuteMethod(scriptPath);
        }

        /// <summary>
        /// exe から上方探索して "src/python_runtime" を含む "src" ディレクトリを返す。
        /// 見つからなければ null。
        /// </summary>
        private static string FindSrcDirectory()
        {
            try
            {
                var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
                while (dir != null)
                {
                    var candidate = Path.Combine(dir.FullName, "src");
                    if (Directory.Exists(candidate)
                        && Directory.Exists(Path.Combine(candidate, "python_runtime")))
                        return candidate;
                    dir = dir.Parent;
                }
            }
            catch { /* best-effort */ }
            return null;
        }
    }
}