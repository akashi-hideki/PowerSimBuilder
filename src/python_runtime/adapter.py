"""Adapter: wrap an existing Python class as a Component.

Design:
- The wrapped class (impl_class) is not modified.
- Shared library components/*.py is not edited.
- Project-specific differences belong in <project>/user_components/.
"""

from __future__ import annotations

from collections.abc import Callable, Mapping
from typing import Any

from .component_base import Component


def wrap_class(
    name: str,
    impl_class: type,
    *,
    parameters: Mapping[str, Any] | None = None,
    inputs: Mapping[str, Any] | None = None,
    outputs: Mapping[str, Any] | None = None,
    call: Callable[[Any, Mapping[str, Any], float], Mapping[str, Any]] | None = None,
) -> type[Component]:
    """Wrap an existing class as a Component subclass.

    Args:
        name: Component type name (e.g. "MyFilter").
        impl_class: Existing class to wrap. Not modified.
        parameters: kwargs passed to impl_class() at construction.
        inputs: Initial Component.inputs dict.
        outputs: Initial Component.outputs dict.
        call: Custom call (impl, inputs, dt) -> dict. None means
              impl.update(t, dt, **inputs).
    """

    class _Adapter(Component):
        def __init__(self, comp_id: str, comp_type: str | None = None) -> None:
            super().__init__(comp_id, comp_type or name)
            self.parameters = dict(parameters or {})
            self.inputs = dict(inputs or {})
            self.outputs = dict(outputs or {})
            self._impl: Any | None = None

        def reset(self) -> None:
            super().reset()
            self._impl = None

        def update(self, t: float, dt: float, sim_data: Any) -> None:
            if self._impl is None:
                self._impl = impl_class(**self.parameters)
            if call is not None:
                result = call(self._impl, self.inputs, dt)
            else:
                result = self._impl.update(t, dt, **self.inputs)
            if isinstance(result, dict):
                for k, v in result.items():
                    if k in self.outputs:
                        self.outputs[k] = v

    _Adapter.__name__ = name
    _Adapter.__qualname__ = name
    _Adapter.__doc__ = f"Adapter wrapping {impl_class.__module__}.{impl_class.__qualname__}"
    return _Adapter
