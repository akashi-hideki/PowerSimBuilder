"""Integration test: C#-generated Python code runs on the runtime.

`examples/auto_generated_poc.py` is a representation of the Python
code emitted by the C# side of PowerSimBuilder (PythonSimulationMainBuilder).
This test asserts that code executes correctly on the Python runtime.
"""
from __future__ import annotations

import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
POC_FILE = REPO_ROOT / "examples" / "auto_generated_poc.py"


def test_poc_file_exists():
    assert POC_FILE.exists(), f"{POC_FILE} not found"


def test_poc_runs_successfully():
    """Run the generated PoC as a subprocess and check exit code."""
    result = subprocess.run(
        [sys.executable, "-m", "examples.auto_generated_poc"],
        cwd=str(REPO_ROOT),
        capture_output=True,
        text=True,
        timeout=30,
    )
    assert result.returncode == 0, (
        f"auto_generated_poc failed:\n"
        f"STDOUT:\n{result.stdout}\n"
        f"STDERR:\n{result.stderr}"
    )
    # The PoC prints "LED ON" when the output activates
    assert "LED ON" in result.stdout
