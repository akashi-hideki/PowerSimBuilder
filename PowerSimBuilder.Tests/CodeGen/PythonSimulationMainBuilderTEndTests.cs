using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using PowerSimBuilder;

namespace PowerSimBuilder.Tests.CodeGen
{
    /// <summary>
    /// v1.8.1: PythonSimulationMainBuilder.TEnd is written into demo_main.py.
    /// </summary>
    public class PythonSimulationMainBuilderTEndTests
    {
        [Theory]
        [InlineData(5.0)]
        [InlineData(300.0)]
        [InlineData(0.5)]
        public void TEnd_IsWrittenIntoGeneratedCode(double tEnd)
        {
            var tmp = Path.Combine(Path.GetTempPath(), "psb_tend_" + Guid.NewGuid().ToString("N") + ".py");
            try
            {
                var builder = new PythonSimulationMainBuilder
                {
                    FilePath = tmp,
                    Components = new List<ComponentDescriptor>(),
                    ComponentInstances = new List<ComponentControl>(),
                    Connections = new List<Connection>(),
                    TEnd = tEnd,
                };
                builder.BuildPythonSimulationFile();
                var code = File.ReadAllText(tmp);
                var invariant = tEnd.ToString(System.Globalization.CultureInfo.InvariantCulture);
                Assert.Contains("t_end = " + invariant, code);
            }
            finally { try { File.Delete(tmp); } catch { } }
        }
    }
}
