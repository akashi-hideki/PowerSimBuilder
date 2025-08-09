// The MIT License (MIT)
// 
// Copyright (c) .NET Foundation and Contributors
// 
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.Threading;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// single instance app
    /// </summary>
    public class SingleInstanceApp
    {
        private Mutex mutex;
        private string mutexName;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleInstanceApp"/>
        /// class.
        /// </summary>
        /// <param name="mutexName">Name of the mutex.</param>
        public SingleInstanceApp(string mutexName)
        {
            this.mutexName = mutexName;
            this.mutex = new Mutex(false, mutexName);
        }

        // Check if the application is already running
        /// <summary>
        /// Determines whether [is already running].
        /// </summary>
        /// <returns><c>true</c> if [is already running]; otherwise,
        ///     <c>false</c>.</returns>
        public bool IsAlreadyRunning()
        {
            return !mutex.WaitOne(TimeSpan.Zero, true);
        }

        /// <summary>
        /// Brings the existing window to front.
        /// </summary>
        // Bring the window of an already running application instance to the front
        public void BringExistingWindowToFront()
        {
            foreach (Form form in Application.OpenForms)
            {
                BringFormToFront(form);
            }
        }

        // Release the mutex
        /// <summary>
        /// Releases the mutex.
        /// </summary>
        public void ReleaseMutex()
        {
            if (mutex != null)
            {
                mutex.ReleaseMutex();
            }
        }

        // Bring a form to the front
        private void BringFormToFront(Form form)
        {
            if (form.InvokeRequired)
            {
                form.Invoke(new MethodInvoker(() => BringFormToFront(form)));
            }
            else
            {
                if (form.WindowState == FormWindowState.Minimized)
                {
                    form.WindowState = FormWindowState.Normal; // Restore if minimized
                }
                form.BringToFront(); // Bring to front
            }
        }
    }
}