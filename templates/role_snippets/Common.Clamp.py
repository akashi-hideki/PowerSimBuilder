    # Clamp a value into [lo, hi] (edit key / lo / hi)
    from python_runtime.lib.math_utils import clamp
    _raw = self.globals.get("duty_raw", 0.0)
    self.globals["duty_out"] = clamp(_raw, 0.0, 1.0)
