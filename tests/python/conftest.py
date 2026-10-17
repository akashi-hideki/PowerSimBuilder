"""pytest 共通設定。"""

import sys
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "src"
if str(SRC) not in sys.path:
    sys.path.insert(0, str(SRC))


@pytest.fixture(scope="session")
def scenarios_dir() -> Path:
    """Path to tests/data/scenarios/ (external scenario data)."""
    return ROOT / "tests" / "data" / "scenarios"
