using System;
using System.Diagnostics;
using System.IO;
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

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="executeMethod">Pythonスクリプトを実行するメソッド（nullの場合はデフォルト実装を使用）</param>
        public PythonExecutor(ExecutePythonScriptDelegate executeMethod = null)
        {
            if (executeMethod != null)
            {
                ExecuteMethod = executeMethod;
            }
            else
            {
                ExecuteMethod = DefaultExecutePythonScript;
            }
        }

        /// <summary>
        /// デフォルトのPython実行メソッド（Processを使用）
        /// </summary>
        /// <param name="scriptPath">実行するPythonスクリプトのパス</param>
        /// <returns>実行が成功した場合はtrue、失敗した場合はfalse</returns>
        private bool DefaultExecutePythonScript(string scriptPath)
        {
            try
            {
                if (!File.Exists(scriptPath))
                {
                    MessageBox.Show($"スクリプトファイルが見つかりません: {scriptPath}", "エラー",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                var process = new Process();
                process.StartInfo.FileName = "python";
                process.StartInfo.Arguments = $"\"{scriptPath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = false;
                
                process.OutputDataReceived += (s, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($"出力: {e.Data}");
                };
                
                process.ErrorDataReceived += (s, e) => 
                {
                    if (!string.IsNullOrEmpty(e.Data))
                        Console.WriteLine($"エラー: {e.Data}");
                };
                
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                
                MessageBox.Show("Pythonスクリプトを実行しました。コンソールウィンドウで出力を確認してください。", 
                    "情報", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Pythonスクリプトの実行に失敗しました: {ex.Message}\n\n" +
                               "Pythonがインストールされているか確認してください。", 
                    "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Pythonスクリプトを実行する
        /// </summary>
        /// <param name="scriptPath">実行するPythonスクリプトのパス</param>
        /// <returns>実行が成功した場合はtrue、失敗した場合はfalse</returns>
        public bool Run(string scriptPath)
        {
            return ExecuteMethod(scriptPath);
        }
    }
}