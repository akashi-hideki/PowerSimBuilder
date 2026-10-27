using System;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    static class Program
    {

        // ============================================================
        // デバッグログ（ファイル出力）
        // ============================================================
        private static readonly object _dbgLock = new object();
        private static readonly string _dbgLogPath =
            System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "..", "..", "debug.log");

        public static void DBG(string message)
        {
            try
            {
                lock (_dbgLock)
                {
                    string path = System.IO.Path.GetFullPath(_dbgLogPath);
                    string line = string.Format(
                        "[{0:HH:mm:ss.fff}] {1}{2}",
                        DateTime.Now, message, Environment.NewLine);
                    System.IO.File.AppendAllText(path, line, System.Text.Encoding.UTF8);
                }
            }
            catch { /* ログ失敗は無視 */ }
        }
        [STAThread]
        static void Main(string[] args)
        {
            // Register global exception handler FIRST so that unexpected errors
            // in subsequent startup code (settings / i18n init / UI) are reported.
            CrashReportManager crashReportManager = CrashReportManager.Instance;

            // Load user settings and language (also covered by the handler above).
            AppSettings.Load();
            I18n.SetLanguage(AppSettings.ResolveStartupLanguage());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args.Length > 0 && args[0] == "--statable-import")
            {
                // 引数 2 つ目があればバッチモード（XML パス）
                if (args.Length >= 2 && !string.IsNullOrWhiteSpace(args[1]))
                {
                    string xmlPath = args[1];
                    string outPath = args.Length >= 3 ? args[2] : null;
                    Environment.ExitCode = RunStaTableBatch(xmlPath, outPath);
                    return;
                }
                // 引数なし → GUI 起動（従来動作）
                I18nDumpUi.InstallAutoDump(); Application.Run(new StaTableImportForm());
            }
            else if (args.Length >= 2 && args[0] == "--test-create-closed-loop-project")
            {
                RunHeadlessCreateClosedLoop(args[1]);
            }
            else if (args.Length >= 2 && args[0] == "--test-create-two-comp-project")
            {
                RunHeadlessCreateTwoComp(args[1]);
            }
            else if (args.Length >= 2 && args[0] == "--test-create-fmu-project")
            {
                RunHeadlessCreate(args[1]);
            }
            else if (args.Length >= 2 && args[0] == "--test-save-project")
            {
                RunHeadlessTest(args[1], null);
            }
            else if (args.Length >= 3 && args[0] == "--test-load-save-no-arrange")
            {
                RunHeadlessTest(args[2], args[1], disableArrange: true);
            }
            else if (args.Length >= 3 && args[0] == "--test-load-save")
            {
                RunHeadlessTest(args[2], args[1]);
            }
            else
            {
                Application.Run(new MainForm());
            }
        }

        /// <summary>
        /// StaTable XML を読み込み、controller_box.py を生成するバッチ処理。
        /// 戻り値: 0=成功, 1=パースエラー, 2=ファイルなし, 3=例外
        /// </summary>
        /// <summary>
        /// Headless test entry: load (optional) → save → exit. Writes log to %TEMP%.
        /// </summary>
        /// <summary>
        /// Headless: build a demo simulation (FirstOrderLag + Dahlquist FMU)
        /// programmatically, then save. Writes log to %TEMP%.
        /// </summary>
        private static void RunHeadlessCreateClosedLoop(string outputPath)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "PowerSimBuilder_TestSave.log");
            try { File.Delete(logPath); } catch { }
            void WriteLog(string msg) { try { File.AppendAllText(logPath, msg + "\n"); } catch { } }
            WriteLog("=== RunHeadlessCreateClosedLoop START ===");

            MainForm? form = null;
            var done = false;
            void Execute()
            {
                if (done) return;
                done = true;
                try
                {
                    WriteLog("Execute() entered");
                    form!.Test_CreateClosedLoopProject();
                    form.Test_SaveProjectToPath(outputPath);
                    WriteLog("CREATE_CLOSED_LOOP_OK: " + outputPath);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    WriteLog("FAIL: " + ex.Message);
                    WriteLog("STACK: " + ex.StackTrace);
                    Environment.ExitCode = 1;
                }
                finally { try { form!.Close(); } catch { } }
            }

            try
            {
                form = new MainForm();
                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (s, e) => { timer.Stop(); Execute(); };
                form.Shown += (s, e) => { WriteLog("Form.Shown fired"); timer.Start(); };
                Application.Run(form);
            }
            catch (Exception ex)
            {
                WriteLog("OUTER FAIL: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void RunHeadlessCreateTwoComp(string outputPath)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "PowerSimBuilder_TestSave.log");
            try { File.Delete(logPath); } catch { }
            void WriteLog(string msg) { try { File.AppendAllText(logPath, msg + "\n"); } catch { } }
            WriteLog("=== RunHeadlessCreateTwoComp START ===");

            MainForm? form = null;
            var done = false;
            void Execute()
            {
                if (done) return;
                done = true;
                try
                {
                    WriteLog("Execute() entered");
                    form!.Test_CreateTwoComponentProject();
                    form.Test_SaveProjectToPath(outputPath);
                    WriteLog("CREATE_TWO_COMP_OK: " + outputPath);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    WriteLog("FAIL: " + ex.Message);
                    WriteLog("STACK: " + ex.StackTrace);
                    Environment.ExitCode = 1;
                }
                finally { try { form!.Close(); } catch { } }
            }

            try
            {
                form = new MainForm();
                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (s, e) => { timer.Stop(); Execute(); };
                form.Shown += (s, e) => { WriteLog("Form.Shown fired"); timer.Start(); };
                Application.Run(form);
            }
            catch (Exception ex)
            {
                WriteLog("OUTER FAIL: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void RunHeadlessCreate(string outputPath)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "PowerSimBuilder_TestSave.log");
            try { File.Delete(logPath); } catch { }

            void WriteLog(string msg)
            {
                try { File.AppendAllText(logPath, msg + "\n"); } catch { }
            }

            WriteLog("=== RunHeadlessCreate START ===");
            WriteLog("outputPath = " + outputPath);

            MainForm? form = null;
            var done = false;

            void Execute()
            {
                if (done) return;
                done = true;
                try
                {
                    WriteLog("Execute() entered");
                    form!.Test_CreateDemoSimulation();
                    form.Test_SaveProjectToPath(outputPath);
                    WriteLog("CREATE_SAVE_OK: " + outputPath);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    WriteLog("FAIL: " + ex.Message);
                    WriteLog("STACK: " + ex.StackTrace);
                    Environment.ExitCode = 1;
                }
                finally
                {
                    try { form!.Close(); } catch { }
                }
            }

            try
            {
                form = new MainForm();
                WriteLog("MainForm constructed");
                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (s, e) => { timer.Stop(); Execute(); };
                form.Shown += (s, e) => { WriteLog("Form.Shown fired"); timer.Start(); };
                Application.Run(form);
                WriteLog("=== Application.Run returned ===");
            }
            catch (Exception ex)
            {
                WriteLog("OUTER FAIL: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static void RunHeadlessTest(string savePath, string? loadPath, bool disableArrange = false)
        {
            var logPath = Path.Combine(Path.GetTempPath(), "PowerSimBuilder_TestSave.log");
            try { File.Delete(logPath); } catch { }

            void WriteLog(string msg)
            {
                try { File.AppendAllText(logPath, msg + "\n"); } catch { }
            }

            WriteLog("=== RunHeadlessTest START ===");
            WriteLog("savePath = " + savePath);
            WriteLog("loadPath = " + (loadPath ?? "(null)"));

            MainForm? form = null;
            var done = false;

            void Execute()
            {
                if (done) return;
                done = true;
                try
                {
                    WriteLog("Execute() entered");
                    if (!string.IsNullOrEmpty(loadPath))
                    {
                        form!.Test_LoadProjectFromPath(loadPath!);
                        WriteLog("Load OK: " + loadPath);
                    }
                    form!.Test_SaveProjectToPath(savePath);
                    WriteLog(loadPath == null
                        ? "SAVE_OK: " + savePath
                        : "LOAD_SAVE_OK: " + loadPath + " -> " + savePath);
                    Environment.ExitCode = 0;
                }
                catch (Exception ex)
                {
                    WriteLog("FAIL: " + ex.Message);
                    WriteLog("STACK: " + ex.StackTrace);
                    Environment.ExitCode = 1;
                }
                finally
                {
                    try { form!.Close(); } catch { }
                }
            }

            try
            {
                form = new MainForm();
                form.Test_DisableArrangeOnLoad = disableArrange;
                WriteLog("MainForm constructed (disableArrange=" + disableArrange + ")");
                var timer = new System.Windows.Forms.Timer { Interval = 1500 };
                timer.Tick += (s, e) => { timer.Stop(); Execute(); };
                form.Shown += (s, e) =>
                {
                    WriteLog("Form.Shown fired");
                    timer.Start();
                };
                Application.Run(form);
                WriteLog("=== Application.Run returned ===");
            }
            catch (Exception ex)
            {
                WriteLog("OUTER FAIL: " + ex.Message);
                Environment.ExitCode = 1;
            }
        }

        private static int RunStaTableBatch(string xmlPath, string outPath)
        {
            try
            {
                if (!File.Exists(xmlPath))
                {
                    Console.Error.WriteLine("ERROR: XML ファイルが見つかりません: " + xmlPath);
                    return 2;
                }

                // 出力先決定: 引数優先、無ければ XML と同じディレクトリ
                if (string.IsNullOrEmpty(outPath))
                {
                    var dir = Path.GetDirectoryName(Path.GetFullPath(xmlPath));
                    outPath = Path.Combine(dir, "controller_box_powersim.py");
                }

                // パース
                var reader = new StaTableProjectReader();
                var result = reader.ParseFile(xmlPath);

                if (result.Errors.Count > 0)
                {
                    Console.Error.WriteLine("ERROR: XML パースに失敗:");
                    foreach (var err in result.Errors)
                        Console.Error.WriteLine("  - " + err);
                    return 1;
                }

                if (result.Warnings.Count > 0)
                {
                    Console.WriteLine("[WARNINGS]");
                    foreach (var w in result.Warnings)
                        Console.WriteLine("  - " + w);
                }

                // 生成
                var gen = new StaTableCodeGenerator();
                gen.GenerateToFile(result.Project, outPath);

                Console.WriteLine("OK: " + outPath);
                Console.WriteLine("  Project: " + result.Project.Name);
                Console.WriteLine("  Tabs: " + result.Project.Tabs.Count);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("ERROR: " + ex.Message);
                return 3;
            }
        }
    }
}