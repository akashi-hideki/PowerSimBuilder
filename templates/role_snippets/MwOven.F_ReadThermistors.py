temp = self.inputs.get("oven_current_temp", 0) or 0
self.globals["thermistor_top"] = temp + 5
self.globals["thermistor_middle"] = temp
self.globals["thermistor_bottom"] = temp - 5
return 0
