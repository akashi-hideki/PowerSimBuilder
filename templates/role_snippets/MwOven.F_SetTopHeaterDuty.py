target = self.globals.get("oven_target_temp", 200) or 200
if target <= 0:
    target = 200
actual = self.globals.get("thermistor_middle", 25) or 25
error = target - actual
kp = 0.02
duty = kp * error
if duty < 0.0:
    duty = 0.0
elif duty > 1.0:
    duty = 1.0
self.globals["heater_top_duty"] = duty
return 0
