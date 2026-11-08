    # Exponential moving average (alpha=0.1; lower = smoother)
    from python_runtime.lib.filters import ExponentialMovingAverage
    if not hasattr(self, "_ema"):
        self._ema = ExponentialMovingAverage(alpha=0.1)
    self.globals["ema_out"] = self._ema.update(self.globals.get("ema_in", 0.0))
