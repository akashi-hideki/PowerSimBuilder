temp = self.globals.get("thermistor_top", 25) or 25
if temp > 250:
    self.fire_event("MwOven", "OV_OVERHEAT")
return 0
