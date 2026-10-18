using System;
using System.Diagnostics;
using System.Threading;
using PowerSimBuilder;
using Xunit;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Tests for PythonExecutor.IsPythonAvailable() and the startup check.
    /// These tests must PASS regardless of whether Python is installed.
    /// </summary>
    public class PythonExecutorProbeTests
    {
        [Fact]
        public void IsPythonAvailable_DoesNotThrow()
        {
            // Must not throw even if python is missing, PATH is broken, etc.
            var result = PythonExecutor.IsPythonAvailable();
            Assert.True(result == true || result == false);
        }

        [Fact]
        public void IsPythonAvailable_PathWithoutPython_ReturnsFalse()
        {
            var originalPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            try
            {
                Environment.SetEnvironmentVariable("PATH", @"C:\__nonexistent_xyz__");
                var result = PythonExecutor.IsPythonAvailable();
                Assert.False(result, "PATH without python must return false");
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", originalPath);
            }
        }

        [Fact]
        public void IsPythonAvailable_IsFastEnough()
        {
            // Startup calls this; should not block more than ~3s (timeout)
            var sw = Stopwatch.StartNew();
            try
            {
                PythonExecutor.IsPythonAvailable();
            }
            finally
            {
                sw.Stop();
            }
            Assert.True(sw.ElapsedMilliseconds < 5000,
                $"IsPythonAvailable() took {sw.ElapsedMilliseconds} ms, expected < 5000");
        }
    }
}
