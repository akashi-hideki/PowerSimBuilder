"""Regression test: every component class must be exported from __init__.

Prevents the Phase 3 bug where recorded_signal.py was added but
RecordedSignal was not exported from components/__init__.py.

Uses AST to extract the actual class names from each file, because
the naming is not always pure snake_case -> PascalCase:
  pid_controller.py -> PIDController
  pwm_3phase.py     -> PWM3Phase
  sum_block.py      -> Sum
"""

from __future__ import annotations

import ast
import pathlib

import python_runtime.components as pkg


def _component_files() -> list[pathlib.Path]:
    components_dir = pathlib.Path(pkg.__file__).resolve().parent
    return sorted(f for f in components_dir.glob("*.py") if f.name != "__init__.py")


def _public_classes(path: pathlib.Path) -> list[str]:
    """Return top-level class names defined in the file (excluding _-prefixed)."""
    tree = ast.parse(path.read_text(encoding="utf-8"))
    classes: list[str] = []
    for node in tree.body:  # only top-level, skip nested
        if isinstance(node, ast.ClassDef) and not node.name.startswith("_"):
            classes.append(node.name)
    return classes


def test_every_module_exports_a_class() -> None:
    """Each component .py should export at least one class from the package."""
    missing: list[str] = []
    for f in _component_files():
        classes = _public_classes(f)
        if not classes:
            continue  # no classes (e.g. pure helper module) -- skip
        for class_name in classes:
            if not hasattr(pkg, class_name):
                missing.append(f"{f.stem}.py -> {class_name}")
    assert not missing, (
        "These classes are defined but not exported from components/__init__.py:\n"
        + "\n".join(f"  {m}" for m in missing)
    )


def test_every_export_is_in_dunder_all() -> None:
    """Every class exposed on the package must be listed in __all__."""
    missing: list[str] = []
    for f in _component_files():
        for class_name in _public_classes(f):
            if hasattr(pkg, class_name) and class_name not in pkg.__all__:
                missing.append(class_name)
    assert not missing, f"These classes are exported but missing from __all__: {missing}"


def test_recordedsignal_is_exported() -> None:
    """Explicit regression test for the Phase 3 bug (v1.10.0)."""
    from python_runtime.components import RecordedSignal

    assert RecordedSignal is not None
    assert "RecordedSignal" in pkg.__all__
