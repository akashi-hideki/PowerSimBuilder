#!/usr/bin/env python3
"""
add_plot_layout_tests.py - add headless tests for the generated simulation graph.

  1. PowerSimBuilder.Tests/Plot/PlotLayoutTests.cs   (new file)
       Builds demo_main.py with PythonSimulationMainBuilder, runs the generated plot_signals() with fake
       data under the Agg backend (no window, no simulation) and checks the matplotlib figure:
       stacked layout = one axes per variable, each with its OWN y-range, width 18 in; overlay = 1 axes.
  2. tests/PowerSimBuilder.UITests/ProjectRoundTripTests.cs   (one test method inserted)
       Runs the app headless (--test-create-fmu-project) and checks that the PNG written into the
       .psbproj is about 1.5x wider than the previous 12 in figure.

    python tools\\add_plot_layout_tests.py --dry-run
    python tools\\add_plot_layout_tests.py

Run:
    dotnet test PowerSimBuilder.Tests\\PowerSimBuilder.Tests.csproj --filter "FullyQualifiedName~PlotLayout"
    dotnet test tests\\PowerSimBuilder.UITests\\PowerSimBuilder.UITests.csproj --filter "FullyQualifiedName~SimulationPng_IsWiderThanBefore"

Idempotent, atomic, backup in tmp\\patch_backup_plottests_<time>\\, line endings / BOM of existing files preserved.
"""
import argparse, datetime, shutil, sys
from pathlib import Path

NEW_REL = "PowerSimBuilder.Tests/Plot/PlotLayoutTests.cs"
UI_REL = "tests/PowerSimBuilder.UITests/ProjectRoundTripTests.cs"
UI_MARK = "CreateFmuProject_SimulationPng_IsWiderThanBefore"
UI_ANCHOR = "    private static string ExtractCoreJson(string archivePath)"

NEW_CS = r'''using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace PowerSimBuilder.Tests.Plot;

/// <summary>
/// Headless check of the generated simulation graph (no application window, no simulation run).
/// Generates demo_main.py with PythonSimulationMainBuilder, feeds fake simulation data to the generated
/// plot_signals() under the Agg backend and inspects the matplotlib figure itself.
/// Requires Python 3.10+ with numpy / matplotlib; src/ is added to PYTHONPATH for python_runtime.
/// </summary>
public class PlotLayoutTests
{
    [Fact]
    public void GeneratedPlot_Stacked_EachVariableHasOwnYScale_And_Is18InchesWide()
    {
        var repo = FindRepoRoot();
        var work = Path.Combine(Path.GetTempPath(), "psb_plot_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(work);
        try
        {
            var mainPy = Path.Combine(work, "demo_main.py");
            var builder = new PythonSimulationMainBuilder
            {
                FilePath = mainPy,
                Components = new List<ComponentDescriptor>(),
                Connections = new List<Connection>(),
            };
            builder.BuildPythonSimulationFile();

            var code = File.ReadAllText(mainPy);
            Assert.Contains("'layout': 'stacked'", code);
            Assert.Contains("'figure_size': (18, 8)", code);
            Assert.DoesNotContain("'figure_size': (12, 8)", code);

            var result = RunHarness(repo, work, mainPy);

            // stacked: one axes per variable, width 18 in (= 1.5 x 12), every axes has its own y-range
            var stacked = result.GetProperty("stacked");
            Assert.Equal(18.0, stacked.GetProperty("width_in").GetDouble(), 2);
            Assert.Equal(6, stacked.GetProperty("axes").GetInt32());
            var spans = stacked.GetProperty("ylims").EnumerateArray()
                .Select(a => Math.Round(a[1].GetDouble() - a[0].GetDouble(), 3))
                .ToList();
            Assert.True(spans.Distinct().Count() >= 5,
                "y-ranges should differ per variable, got spans: " + string.Join(", ", spans));

            // overlay (previous behaviour): a single shared axes, same width
            var overlay = result.GetProperty("overlay");
            Assert.Equal(1, overlay.GetProperty("axes").GetInt32());
            Assert.Equal(18.0, overlay.GetProperty("width_in").GetDouble(), 2);

            Assert.True(File.Exists(Path.Combine(work, "simulation_signals.png")),
                "simulation_signals.png was not written");
        }
        finally
        {
            try { Directory.Delete(work, recursive: true); } catch { }
        }
    }

    private static JsonElement RunHarness(string repo, string work, string mainPy)
    {
        var script = Path.Combine(work, "harness.py");
        File.WriteAllText(script, HarnessPy, new UTF8Encoding(false));

        var psi = new ProcessStartInfo
        {
            FileName = "python",
            WorkingDirectory = work,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        psi.ArgumentList.Add(script);
        psi.ArgumentList.Add(mainPy);
        psi.Environment["MPLBACKEND"] = "Agg";
        psi.Environment["PYTHONIOENCODING"] = "utf-8";
        var src = Path.Combine(repo, "src");
        var existing = Environment.GetEnvironmentVariable("PYTHONPATH");
        psi.Environment["PYTHONPATH"] = string.IsNullOrEmpty(existing) ? src : src + Path.PathSeparator + existing;

        using var proc = Process.Start(psi) ?? throw new InvalidOperationException("cannot start python");
        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();
        if (!proc.WaitForExit(120_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            Assert.Fail("python timed out");
        }
        var stdout = stdoutTask.GetAwaiter().GetResult();
        var stderr = stderrTask.GetAwaiter().GetResult();

        var line = stdout.Split('\n').FirstOrDefault(l => l.StartsWith("RESULT:", StringComparison.Ordinal));
        if (line == null)
        {
            Assert.Fail($"python harness failed (exit {proc.ExitCode}). Python 3.10+ with numpy/matplotlib is required.\n" +
                        $"--- stdout ---\n{stdout}\n--- stderr ---\n{stderr}");
        }
        using var doc = JsonDocument.Parse(line!.Substring("RESULT:".Length));
        return doc.RootElement.Clone();
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "src", "python_runtime")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("repository root (src/python_runtime) not found above " + AppContext.BaseDirectory);
    }

    private const string HarnessPy = """
        import importlib.util, json, math, sys
        import matplotlib
        matplotlib.use("Agg")
        import matplotlib.pyplot as plt

        spec = importlib.util.spec_from_file_location("gen_main", sys.argv[1])
        mod = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(mod)


        class FakeSim:
            pass


        sim = FakeSim()
        sim.logs = []
        for k in range(200):
            t = k * 0.5
            sim.logs.append({"time": t, "components": {
                "oven": {"outputs": {"temp": 25 + 175 * (1 - math.exp(-t / 30))}},
                "pid": {"outputs": {"duty": 0.5 + 0.4 * math.sin(t / 10)}},
                "ref": {"outputs": {"value": 200}},
                "door": {"outputs": {"open": 1 if 40 < t < 60 else 0}},
                "mw": {"outputs": {"current": 0.002 * t}},
                "pump": {"outputs": {"p": 101325 + 50 * math.sin(t)}},
            }})

        SIGNALS = [("temp", "oven", "temp"), ("duty", "pid", "duty"), ("ref", "ref", "value"),
                   ("door", "door", "open"), ("current", "mw", "current"), ("pressure", "pump", "p")]
        cfg = mod.plot_config
        cfg["signals"] = [
            {"name": n, "component": c, "port": p, "enabled": True, "color": None, "line_style": "-"}
            for n, c, p in SIGNALS
        ]
        cfg["global_settings"]["dpi"] = 50

        out = {}
        for layout in ("stacked", "overlay"):
            cfg["global_settings"]["layout"] = layout
            plt.close("all")
            mod.plot_signals(sim, cfg)
            fig = plt.gcf()
            w, h = fig.get_size_inches()
            out[layout] = {
                "width_in": float(w),
                "height_in": float(h),
                "axes": len(fig.axes),
                "ylims": [[float(a) for a in ax.get_ylim()] for ax in fig.axes],
            }
        print("RESULT:" + json.dumps(out))
        """;
}
'''

UI_METHOD = r"""    [Fact]
    public void CreateFmuProject_SimulationPng_IsWiderThanBefore()
    {
        var tmpDir = Path.Combine(Path.GetTempPath(),
            "psb_plotw_" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(tmpDir);
        try
        {
            var outputPath = Path.Combine(tmpDir, "plot.psbproj");
            var (exitCode, log) = RunHeadless("--test-create-fmu-project", outputPath);
            Assert.True(File.Exists(outputPath),
                $"Archive not created. exit={exitCode} log=\n{log}");

            using var zip = ZipFile.OpenRead(outputPath);

            using var pyStream = zip.GetEntry("generated/demo_main.py")!.Open();
            using var pyReader = new StreamReader(pyStream);
            var pyCode = pyReader.ReadToEnd();
            Assert.Contains("'layout': 'stacked'", pyCode);
            Assert.Contains("'figure_size': (18, 8)", pyCode);

            var dpiMatch = System.Text.RegularExpressions.Regex.Match(pyCode, @"'dpi':\s*(\d+)");
            Assert.True(dpiMatch.Success, "dpi not found in generated code");
            var dpi = int.Parse(dpiMatch.Groups[1].Value);

            var pngEntry = zip.GetEntry("generated/simulation_signals.png");
            Assert.NotNull(pngEntry);
            using var ms = new MemoryStream();
            using (var s = pngEntry!.Open()) s.CopyTo(ms);
            var png = ms.ToArray();
            Assert.True(png.Length > 24 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "not a PNG");
            int width = (png[16] << 24) | (png[17] << 16) | (png[18] << 8) | png[19];

            // before: 12 in wide; now 18 in (1.5x). bbox_inches='tight' may trim a little.
            Assert.True(width >= 12 * dpi * 1.3,
                $"PNG width {width}px is not wider than the old 12 in figure ({12 * dpi}px at {dpi} dpi)");
        }
        finally
        {
            try { Directory.Delete(tmpDir, recursive: true); } catch { }
        }
    }

"""


def read(p):
    raw = p.read_bytes()
    bom = raw.startswith(b"\xef\xbb\xbf")
    t = raw.decode("utf-8-sig")
    crlf = "\r\n" in t
    return t.replace("\r\n", "\n"), bom, crlf


def write(p, t, bom, crlf):
    if crlf:
        t = t.replace("\n", "\r\n")
    p.write_bytes((b"\xef\xbb\xbf" if bom else b"") + t.encode("utf-8"))


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--root", default=None)
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    root = Path(a.root).resolve() if a.root else Path(__file__).resolve().parent.parent
    new_p, ui_p = root / NEW_REL, root / UI_REL
    steps, ui_new = [], None

    if new_p.exists():
        steps.append(("SKIP", NEW_REL, "already exists"))
    else:
        steps.append(("CREATE", NEW_REL, "headless plot test"))

    if not ui_p.exists():
        print(f"FAIL: {UI_REL} not found"); return 1
    ui_text, ui_bom, ui_crlf = read(ui_p)
    if UI_MARK in ui_text:
        steps.append(("SKIP", UI_REL, "test already present"))
    else:
        if ui_text.count(UI_ANCHOR) != 1:
            print(f"FAIL: anchor '{UI_ANCHOR.strip()}' found {ui_text.count(UI_ANCHOR)} time(s); no changes")
            return 1
        ui_new = ui_text.replace(UI_ANCHOR, UI_METHOD + UI_ANCHOR)
        steps.append(("APPLY", UI_REL, "PNG width test"))

    print(f"Root: {root}\n")
    for st, rel, label in steps:
        print(f"  [{st:6}] {rel}  ({label})")
    if a.dry_run:
        print("\nDRY RUN: nothing written."); return 0

    bdir = root / "tmp" / f"patch_backup_plottests_{datetime.datetime.now():%Y%m%d_%H%M%S}"
    if ui_new is not None:
        (bdir / "tests/PowerSimBuilder.UITests").mkdir(parents=True, exist_ok=True)
        shutil.copy2(ui_p, bdir / UI_REL)
        write(ui_p, ui_new, ui_bom, ui_crlf)
        print(f"\nBackup: {bdir}")
    if not new_p.exists():
        new_p.parent.mkdir(parents=True, exist_ok=True)
        new_p.write_bytes(NEW_CS.encode("utf-8"))
    if UI_MARK not in read(ui_p)[0] or not new_p.exists():
        print("VERIFY FAILED"); return 2
    print("\nDone.")
    print('Next: dotnet test PowerSimBuilder.Tests\\PowerSimBuilder.Tests.csproj --filter "FullyQualifiedName~PlotLayout"')
    return 0


if __name__ == "__main__":
    sys.exit(main())
