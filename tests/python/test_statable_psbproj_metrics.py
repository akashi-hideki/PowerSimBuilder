"""Verify .psbproj simulation metrics match README (v1.8.0 quantitative)."""

from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
PROJ = ROOT / "examples" / "demos" / "oven_control_statable_v181.psbproj"
TOOL = ROOT / "tools" / "verify_statable_psbproj.py"


@pytest.mark.skipif(not PROJ.exists() or not TOOL.exists(), reason="demo or tool missing")
def test_oven_control_statable_metrics():
    """ss~=200.68, ovs~=1.45%, settle~=64.6s (matches README / no_aw tuning).

    Uses oven_control_statable_v181.psbproj (self-contained: bundles
    components/CookingHeaterController_powersim.py inside the ZIP).
    The non-v181 variant relies on generated/*_powersim.py which is
    gitignored, so this test would fail on a fresh CI checkout.
    """
    out = subprocess.run(
        [sys.executable, str(TOOL), str(PROJ)], capture_output=True, text=True, cwd=str(ROOT)
    )
    assert out.returncode == 0, out.stderr
    m = re.search(
        r"OvenChamber\.oven_current_temp\s+([\d.]+)\s+([-\d.]+)\s+([\d.]+)\s+([\d.]+)\s+([\d.]+)",
        out.stdout,
    )
    assert m, "could not parse metrics from:\n" + out.stdout
    ss, err, peak, ovs, settle = map(float, m.groups())
    assert abs(ss - 200.68) < 0.1, f"ss={ss}"
    assert abs(ovs - 1.45) < 0.1, f"ovs={ovs}"
    assert abs(settle - 64.6) < 0.5, f"settle={settle}"
