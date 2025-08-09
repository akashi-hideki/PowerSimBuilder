using System;
using System.Windows.Forms;

namespace PowerSimBuilder
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			// Initialize the class to prevent multiple instances of the application
			CrashReportManager crashReportManager = CrashReportManager.Instance;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
	}
}
