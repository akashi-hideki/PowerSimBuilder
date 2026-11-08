    # Simple moving average (window=10 samples)
    from python_runtime.lib.filters import MovingAverage
    if not hasattr(self, "_ma"):
        self._ma = MovingAverage(window=10)
    self.globals["ma_out"] = self._ma.update(self.globals.get("ma_in", 0.0))
