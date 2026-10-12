using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.Core
{
    public class LogcatTraceHelperSmokeTests
    {
        [Fact]
        public void Log_AllLevels_DoNotThrow()
        {
            var ex = Record.Exception(() =>
            {
                LogcatTraceHelper.Instance.Log("TEST", "info message");
                LogcatTraceHelper.Instance.LogError("TEST", "error message");
                LogcatTraceHelper.Instance.LogWarning("TEST", "warning message");
                LogcatTraceHelper.Instance.SimpleMessage("TEST", "simple");
            });

            Assert.Null(ex);
        }
    }
}
