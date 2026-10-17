"""Integration test: C#-generated Python code runs on the runtime.

`examples/auto_generated_poc.py` is a representation of the Python
code emitted by the C# side of PowerSimBuilder (PythonSimulationMainBuilder).
This test asserts that code executes correctly on the Python runtime.

Note: the subprocess must be able to import `python_runtime` even when
the parent pytest process has injected `pythonpath` via pyproject.toml.
We therefore propagate a `PYTHONPATH` pointing to `src/` explicitly,
which keeps this integration test independent of the editable install
state (which can vary between local dev and CI).
"""
from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[2]
POC_FILE = REPO_ROOT / "examples" / "auto_generated_poc.py"
SRC_DIR = REPO_ROOT / "src"


def _subprocess_env() -> dict[str, str]:
    """Return env with PYTHONPATH including the `src/` directory."""
    env = os.environ.copy()
    prev = env.get("PYTHONPATH", "")
    sep = os.pathsep
    env["PYTHONPATH"] = str(SRC_DIR) + (sep + prev if prev else "")
    return env


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
        env=_subprocess_env(),
    )
    assert result.returncode == 0, (
        f"auto_generated_poc failed:\n"
        f"STDOUT:\n{result.stdout}\n"
        f"STDERR:\n{result.stderr}"
    )
    assert "LED ON" in result.stdout
