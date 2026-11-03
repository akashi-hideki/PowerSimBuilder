using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Gui
{
    /// <summary>
    /// Log-based test infrastructure: EditLogEvent writes JSON Lines to PSB_EDIT_LOG.
    /// Enables automated verification of UI edit paths without FlaUI.
    /// </summary>
    public class EditLogEventTests
    {
        private static MethodInfo GetMethod(string name)
        {
            var m = typeof(DesignCanvas).GetMethod(name,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(m);
            return m!;
        }

        [Fact]
        public void EditLogEvent_WritesJsonLineWithEventField()
        {
            var logPath = Path.Combine(Path.GetTempPath(),
                "psb_logtest_" + Guid.NewGuid().ToString("N") + ".log");
            var old = Environment.GetEnvironmentVariable("PSB_EDIT_LOG");
            Environment.SetEnvironmentVariable("PSB_EDIT_LOG", logPath);
            try
            {
                // Invoke EditLogEvent("OpenEditor", ("path","/x/y.py"))
                var m = GetMethod("EditLogEvent");
                var tupleType = typeof(ValueTuple<string, object>);
                var arr = Array.CreateInstance(tupleType, 1);
                arr.SetValue(Activator.CreateInstance(tupleType, "path", "/x/y.py"), 0);
                m.Invoke(null, new object[] { "OpenEditor", arr });

                Assert.True(File.Exists(logPath), "log file not created: " + logPath);
                var lines = File.ReadAllLines(logPath);
                Assert.NotEmpty(lines);
                var doc = JsonDocument.Parse(lines[lines.Length - 1]);
                Assert.Equal("OpenEditor", doc.RootElement.GetProperty("event").GetString());
                Assert.Equal("/x/y.py", doc.RootElement.GetProperty("path").GetString());
                Assert.True(doc.RootElement.TryGetProperty("ts", out _));
                Assert.True(doc.RootElement.TryGetProperty("session", out _));
            }
            finally
            {
                Environment.SetEnvironmentVariable("PSB_EDIT_LOG", old);
                try { File.Delete(logPath); } catch { }
            }
        }

        [Fact]
        public void EditDebugLog_BackwardCompat_WritesMessageEvent()
        {
            var logPath = Path.Combine(Path.GetTempPath(),
                "psb_logtest_" + Guid.NewGuid().ToString("N") + ".log");
            var old = Environment.GetEnvironmentVariable("PSB_EDIT_LOG");
            Environment.SetEnvironmentVariable("PSB_EDIT_LOG", logPath);
            try
            {
                var m = GetMethod("EditDebugLog");
                m.Invoke(null, new object[] { "hello world" });
                Assert.True(File.Exists(logPath));
                var lines = File.ReadAllLines(logPath);
                Assert.NotEmpty(lines);
                var doc = JsonDocument.Parse(lines[lines.Length - 1]);
                Assert.Equal("message", doc.RootElement.GetProperty("event").GetString());
                Assert.Equal("hello world", doc.RootElement.GetProperty("text").GetString());
            }
            finally
            {
                Environment.SetEnvironmentVariable("PSB_EDIT_LOG", old);
                try { File.Delete(logPath); } catch { }
            }
        }

        [Fact]
        public void GetEditLogPath_HonorsEnvironmentVariable()
        {
            var custom = Path.Combine(Path.GetTempPath(), "psb_custom_path.log");
            var old = Environment.GetEnvironmentVariable("PSB_EDIT_LOG");
            Environment.SetEnvironmentVariable("PSB_EDIT_LOG", custom);
            try
            {
                var m = GetMethod("GetEditLogPath");
                var result = (string)m.Invoke(null, null)!;
                Assert.Equal(custom, result);
            }
            finally { Environment.SetEnvironmentVariable("PSB_EDIT_LOG", old); }
        }
    }
}
