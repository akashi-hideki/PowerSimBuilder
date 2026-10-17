using System;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            CrashReportManager crashReportManager = CrashReportManager.Instance;
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
                Application.Run(new StaTableImportForm());
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