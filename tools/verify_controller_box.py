"""verify_controller_box.py - Actually drive ControllerBox.update() to verify gating."""
import zipfile, json, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(ROOT / "src"))
from python_runtime.components.controller_box import ControllerBox

p = Path(sys.argv[1])
d = json.loads(zipfile.ZipFile(p).read("project.json"))

cb_data = None
for c in d["components"]:
    if c.get("descriptor", {}).get("Type") == "ControllerBox":
        cb_data = c
        break
if not cb_data:
    print("No ControllerBox in project"); sys.exit(0)

desc = cb_data["descriptor"]
params = cb_data.get("parameters", {})
inst = cb_data["instanceName"]
ig = desc.get("InputPorts", [])
og = desc.get("OutputPorts", [])

print(f"=== {inst} ===")
cb = ControllerBox(inst, input_globals=ig, output_globals=og)
for k in ("auto_events", "gate_layer", "gate_states", "gate_input_port", "gate_output_port"):
    if k in params:
        cb.parameters[k] = params[k]

# ★ Lazy init BEFORE setting inputs (otherwise _ensure_runtime resets them)
cb._ensure_runtime()

gate_layer = params.get("gate_layer", "")
gate_states = [s.strip() for s in params.get("gate_states", "").split(",") if s.strip()]
gate_in  = params.get("gate_input_port", "")
gate_out = params.get("gate_output_port", "")

def run_tick(label, heater_cmd, oven_temp, t):
    if gate_in in cb.inputs:
        cb.inputs[gate_in] = heater_cmd
    if "oven_current_temp" in cb.inputs:
        cb.inputs["oven_current_temp"] = oven_temp
    cb.update(t, 0.05, None)
    st = {l.layer_name: l.state for l in cb._runtime.layers}
    ht = cb.outputs.get(gate_out, 0)
    print(f"  [{label}] heater_command={heater_cmd}  MwOven={st.get('MwOven')}  {gate_out}={ht}")
    return st.get(gate_layer, ""), ht

print("\n[tick sequence - gating test]")
st, ht = run_tick("start", 0.7, 25.0, 0.00)
st, ht = run_tick("on",    0.7, 25.0, 0.05)
assert st == "PidHeating", f"Expected PidHeating, got {st}"
assert abs(ht - 0.7) < 1e-6, f"Expected {gate_out}=0.7 (passed), got {ht}"

# Force-block test: set MwOven to OvIdle manually
cb._runtime.layers_by_name["MwOven"].state = "OvIdle"
st, ht = run_tick("blocked", 0.7, 25.0, 0.10)
assert st == "OvIdle", f"Expected OvIdle, got {st}"
assert abs(ht) < 1e-6, f"Expected {gate_out}=0 (blocked), got {ht}"

print("\n[PASS] gating verified both directions (pass + block)")