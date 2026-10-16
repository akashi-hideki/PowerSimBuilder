using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Initialize the class to prevent multiple instances of the application
            CrashReportManager crashReportManager = CrashReportManager.Instance;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length > 0 && args[0] == "--statable-import")
            {
                Application.Run(new StaTableImportForm());
            }
            else
            {
                Application.Run(new MainForm());
            }
        }
    }
}
