// The MIT License (MIT)
// 
// Copyright (c) .NET Foundation and Contributors
// 
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// Crash Report Maker
    /// </summary>
    public sealed class CrashReportManager
    {
        // Delayed instantiation of the singleton instance Lazy
        private static readonly Lazy<CrashReportManager> instance = new Lazy<CrashReportManager>(() => new CrashReportManager());

        private readonly string logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash_report.txt");

        /// <summary>
        ///      Property Name - Instance.
        ///  </summary>
        public static CrashReportManager Instance
        {
            get { return instance.Value; }
        }

        /// <summary>
        ///  Function Name :  CrashReportManager.
        /// </summary>
        /// <returns>void.</returns>
        private CrashReportManager()
        {
            // Handle UI thread exceptions      
            Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
            // Handle unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            // Handle unobserved task exceptions
            TaskScheduler.UnobservedTaskException += Task_UnobservedTaskException;

        }

        /// <summary>
        ///  Function Name :  Application_ThreadException.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is :
        ///     System.Threading.ThreadExceptionEventArgs.</param>
        /// <returns>void.</returns>
        private void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show($"An {e.Exception.Message} occurred in the application. Please check the crash report for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        ///  Function Name :  CurrentDomain_UnhandledException.
        /// </summary>
        /// <param name="sender">This sender's Datatype is : object.</param>
        /// <param name="e">This e's Datatype is :
        ///     System.UnhandledExceptionEventArgs.</param>
        /// <returns>void.</returns>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogException(ex);
            }
            MessageBox.Show($"A critical {e.ExceptionObject.ToString()} occurred in the application. Please check the crash report for details.", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        /// <summary>
        /// this function is used to handle the unobserved task exception.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Task_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show($"An {e.Exception.Message} occurred in the application. Please check the crash report for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            e.SetObserved();
        }
        /// <summary>
        ///  Function Name :  LogException.
        /// </summary>
        /// <param name="ex">This ex's Datatype is : System.Exception.</param>
        /// <returns>void.</returns>
        
        public void LogException(Exception ex)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(logFilePath, true))
                {
                    writer.WriteLine("====================================");
                    writer.WriteLine("=== Crash Report ===");
                    writer.WriteLine("Date: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
                    writer.WriteLine();

                    // Exception.ToString() includes:
                    // - Exception type
                    // - Message
                    // - StackTrace
                    // - InnerException (recursive)
                    writer.WriteLine("Exception Details:");
                    writer.WriteLine(ex.ToString());

                    writer.WriteLine("====================================");
                    writer.WriteLine();
                }
            }
            catch (Exception logEx)
            {
                // Do not allow logging failure to hide the original exception.
                System.Diagnostics.Debug.WriteLine(
                    "Crash report logging failed: " + logEx);
            }
        }
    }
}