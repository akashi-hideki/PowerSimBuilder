# Tutorial: Building a Demo from StaTable XML

> **Audience**: First-time PowerSimBuilder (PSB) users
> **Duration**: ~20 minutes
> **Requirements**: PSB v1.10.1+, a StaTable XML (`cooking_heater_controller.xml` etc.)
> **Reference demo**: `examples/demos/oven_control_statable.psbproj`

---

## 1. Overview

> **First time?** Start by opening `examples/demos/oven_control_statable.psbproj`
> and clicking **Create Simulation → Run**. You can verify the working gating-based demo first.
> Then follow this tutorial to implement the **role-function approach**, which mirrors real firmware structure.



PowerSimBuilder reads a 7-layer state machine designed in StaTable (XML) and combines it with **physical models** to run a Python simulation.

```
StaTable (design)
   → cooking_heater_controller.xml
   → PSB (ControllerBox placement + physical model wiring + role functions)
   → demo_main.py generation
   → simulation execution
   → simulation_signals.png (temperature control waveform)
```

This tutorial builds a minimal demo using the **MwOven layer** (oven PID heating), with the **heater control implemented in ControllerBox role functions**.

### Control architecture (important)

```
[PID Controller]                    [Controller Box]              [OvenChamber]
  control (0-1) ──────────────────> heater_command
                                      │
                                      ▼ F_SetTopHeaterDuty
                                    heater_top_duty (0-100) ────> heater_top_duty
                                                                       │
                                      oven_current_temp <───────────── oven_current_temp
                                      measured <────────────────────── oven_current_temp
```

- **PID output (0-1)** goes to ControllerBox `heater_command` input
- **Role function `F_SetTopHeaterDuty`** scales 0-1 → 0-100
- **Role function `F_CheckOvenOverheat`** triggers emergency stop
- **Role function `F_ReadThermistors`** computes sensor values
- State-machine gating parameters are **not used** (control lives in role functions)

### Expected result

- `oven_current_temp` rises 25°C → 200°C
- PID settles near 200°C (ss ≈ 200.68°C, ovs ≈ 1.45%, settle ≈ 64.6s)
- Quantitative verification available via CLI

---

## 2. Prerequisites

### 2.1 Required files

| File | Location | Purpose |
|---|---|---|
| `cooking_heater_controller.xml` | `tests\\data\\scenarios\\cooking_heater\\` | StaTable XML |
| `PowerSimBuilder.exe` | `bin\\Debug\\net10.0-windows\\` | PSB binary |

### 2.2 Launch PSB

```powershell
cd C:\\dev\\PowerSimBuilder
.\\bin\\Debug\\net10.0-windows\\PowerSimBuilder.exe
```

---

## 3. Step by step

### Step 1: New project

Press **Ctrl+N** or choose **File → New Project**.

**Expected**: Canvas is completely empty.

### Step 2: Import StaTable XML

1. Click the **"StaTable Import"** toolbar button
2. Click **[Browse...]** next to **"XML File"** and select `cooking_heater_controller.xml`
3. Review the preview (7-layer state machine)
4. Click **[OK]**

**Expected**: A **`Controller Box`** component appears on the Canvas.

> **Note**: `Controller Box` ports are derived from the StaTable XML. They include `heater_command` (input), `heater_top_duty` (output), `oven_current_temp` (input), `error_code` (output), etc.

### Step 3: Place physical models

From the palette (left panel), drag & drop these two components:

| Component | Category | Purpose |
|---|---|---|
| **PID Controller** | Control | Temperature PID control |
| **OvenChamber** | Thermal | Oven thermal model |

**Expected**: 3 components on the Canvas.

### Step 4: Wire 4 connections

Click a port to start a wire. **Watch the direction** (output → input):

| # | From (output) | To (input) | Meaning |
|:---:|---|---|---|
| 1 | `PID Controller.control` | `Controller Box.heater_command` | PID output (0-1) → ControllerBox |
| 2 | `Controller Box.heater_top_duty` | `OvenChamber.heater_top_duty` | Heater command (0-100) → oven |
| 3 | `OvenChamber.oven_current_temp` | `Controller Box.oven_current_temp` | Current temp → ControllerBox |
| 4 | `OvenChamber.oven_current_temp` | `PID Controller.measured` | Current temp → PID feedback |

**Expected**: 4 wires drawn on the Canvas.

### Step 5: Configure Controller Box parameters

Double-click `Controller Box` → parameter editor.

| Parameter | Value | Meaning |
|---|---|---|
| `generated_path` | `generated/CookingHeaterController_powersim.py` | Path to generated ControllerBox (auto-set) |
| `auto_events` | `demo_oven` | Auto-event driver for the demo |

> **Important**: Do **not** set `gate_layer` / `gate_states` / `gate_input_port` / `gate_output_port` (leave empty). Control happens in role functions (Step 5.5).

### Step 5.5: Implement role functions (core)

Implement role functions on ControllerBox. This is the core control logic of the demo.

#### 5.5.1 What are role functions?

ControllerBox has 60+ role functions (`F_xxx`) generated from the StaTable XML. Each function's body between `# STABLE_USER_CODE_START` and `# STABLE_USER_CODE_END` is **user-editable** and is called during simulation.

> **Important**: If the state machine's `do` action doesn't invoke the role function, editing the Python body alone won't call it. See 5.5.5 for enabling calls.

#### 5.5.2 How to edit role functions (GUI)

1. **Double-click** `Controller Box` → Python editor opens
2. In the left pane **File Structure**, expand `📦 CookingHeaterController` → `MwOven`
3. Click `F_SetTopHeaterDuty` → body appears in the right pane
4. Edit between `# STABLE_USER_CODE_START` and `# STABLE_USER_CODE_END`
5. **Ctrl+S** to save

#### 5.5.3 Four functions to implement

| Function | Purpose | When called |
|---|---|---|
| `F_ReadThermistors` | Compute `thermistor_top/middle/bottom` from `oven_current_temp` | Every tick |
| `F_StartOvenPid` | Set `heater_top_on` flag | On `OvIdle → PidHeating` transition |
| `F_StopOvenPid` | Clear flag, reset duty | On `PidHeating → OvIdle` transition |
| `F_SetTopHeaterDuty` | Convert `heater_command` (0-1) → `heater_top_duty` (0-100) | Every tick (during PidHeating) |
| `F_CheckOvenOverheat` | Emergency stop at 250°C | Every tick |

#### 5.5.4 Implementation examples

**F_ReadThermistors**:

```python
def F_ReadThermistors(self):
    # STABLE_USER_CODE_START
    temp = self.inputs.get("oven_current_temp", 0) or 0
    self.globals["thermistor_top"] = temp + 5
    self.globals["thermistor_middle"] = temp
    self.globals["thermistor_bottom"] = temp - 5
    return 0
    # STABLE_USER_CODE_END
```

**F_StartOvenPid**:

```python
def F_StartOvenPid(self):
    # STABLE_USER_CODE_START
    self.globals["heater_top_on"] = 1
    return 0
    # STABLE_USER_CODE_END
```

**F_StopOvenPid**:

```python
def F_StopOvenPid(self):
    # STABLE_USER_CODE_START
    self.globals["heater_top_on"] = 0
    self.globals["heater_top_duty"] = 0
    return 0
    # STABLE_USER_CODE_END
```

**F_SetTopHeaterDuty** (core):

```python
def F_SetTopHeaterDuty(self):
    # STABLE_USER_CODE_START
    if not self.globals.get("heater_top_on", 0):
        self.globals["heater_top_duty"] = 0
        return 0
    cmd = float(self.inputs.get("heater_command", 0.0))  # 0.0 - 1.0
    duty = int(max(0, min(100, cmd * 100)))               # 0 - 100
    self.globals["heater_top_duty"] = duty
    return 0
    # STABLE_USER_CODE_END
```

**F_CheckOvenOverheat**:

```python
def F_CheckOvenOverheat(self):
    # STABLE_USER_CODE_START
    temp = self.globals.get("thermistor_top", 25)
    if temp > 250:
        self.globals["heater_top_on"] = 0
        self.globals["heater_top_duty"] = 0
        self.globals["error_code"] = 1
    return 0
    # STABLE_USER_CODE_END
```

#### 5.5.5 Enabling role function calls (important)

If the StaTable XML `PidHeating` state has an empty `do` action, role functions won't be called automatically. Enable calls via one of:

| Method | Steps | Difficulty |
|---|---|---|
| **(a) Edit XML** | In StaTable GUI, add `F_SetTopHeaterDuty` to `PidHeating` state's `do` action → re-export XML → re-import in PSB → regenerate ControllerBox | Medium |
| **(b) Edit generated Python** | Add `self.F_SetTopHeaterDuty()` etc. to `generated/CookingHeaterController_powersim.py` main loop | Medium |
| **(c) Use gating** | Use `gate_layer` / `gate_states` instead of role functions | Easy |

**Recommended**: (a) or (b).

**(b) example** — add inside `update()` of `generated/CookingHeaterController_powersim.py`, after `tick()`:

```python
def update(self, t, dt, sim_data=None):
    # ... existing code ...
    self._runtime.tick(t, dt, sim_data)

    # Role function dispatch (explicit, since XML do action is empty)
    self.F_ReadThermistors()
    if self.globals.get("heater_top_on", 0):
        self.F_SetTopHeaterDuty()
    self.F_CheckOvenOverheat()
```

#### 5.5.6 Alternative: `tools/edit_role_function.py` (CLI batch)

For CLI edits:

```powershell
python tools\\edit_role_function.py generated/CookingHeaterController_powersim.py MwOven F_ReadThermistors --body "temp = self.inputs.get('oven_current_temp', 0) or 0; self.globals['thermistor_top'] = temp + 5; self.globals['thermistor_middle'] = temp; self.globals['thermistor_bottom'] = temp - 5; return 0"
```

### Step 6: Configure PID Controller parameters

Double-click `PID Controller`:

| Parameter | Value | Meaning |
|---|---|---|
| `kp` | `0.01` | Proportional gain |
| `ki` | `0.0001` | Integral gain |
| `kd` | `0.0` | Derivative gain |
| `out_min` | `0.0` | Output lower bound |
| `out_max` | `1.0` | Output upper bound |
| `setpoint` | `200.0` | Target temperature (°C) |
| `anti_windup` | `False` | Anti-windup enable |

> **Note**: These gains are verified (ss=200.68, ovs=1.45%, settle=64.6s).

### Step 7: Configure OvenChamber parameters

Double-click `OvenChamber`:

| Parameter | Value | Meaning |
|---|---|---|
| `thermal_resistance` | `0.5` | Thermal resistance (K/W) |
| `thermal_capacity` | `200` | Thermal capacity (J/K) |
| `ambient_temperature` | `25` | Ambient temperature (°C) |
| `initial_temperature` | `25` | Initial temperature (°C) |
| `heater_power_max` | `1000` | Max heater power (W) |
| `sensor_offset_top` | `5` | Top thermistor offset |
| `sensor_offset_bottom` | `-5` | Bottom thermistor offset |

### Step 8: Set simulation time (optional)

Set the toolbar `t_end` field to **`300`** (seconds).

### Step 9: Generate simulation

Click the **"Create Simulation"** toolbar button.

**Expected**:
- `PythonEditorForm` opens
- `generated/demo_main.py` is generated
- `generated/CookingHeaterController_powersim.py` retains your role-function edits

### Step 10: Run simulation

Click **"Run"** in `PythonEditorForm`.

**Expected**:
- Simulation completion message
- `simulation_signals.png` generated
- Waveform graph with temperature curve

---

## 4. Verify results

### 4.1 Waveform graph

Open `simulation_signals.png` and verify:

| Signal | Expected |
|---|---|
| `oven_current_temp` | Rises 25°C → 200°C, settles near 200°C |
| `heater_top_duty` | Starts at 0%, converges to low duty (10–30%) |

### 4.2 Quantitative verification

```powershell
python tools\\verify_statable_psbproj.py <your-project.psbproj>
```

**Expected output**:
```
ss      = 200.68 degC   (steady-state error -0.68°C)
ovs     = 1.45 %        (overshoot)
settle  = 64.6 s        (settling time)
```

---

## 5. Save and reuse

### 5.1 Save the project

**Ctrl+S** or **File → Save As...** to save as `.psbproj`:

```
examples\\demos\\my_oven_demo.psbproj
```

### 5.2 Load

**File → Open** and select the `.psbproj`.

---

## 6. Troubleshooting

### 6.1 "Edited role function but waveform is still 0"

**Cause**: Role function not called (XML `do` action empty).

**Fix**: See 5.5.5 methods (a) or (b) to enable calls.

### 6.2 "Waveform is a flat line (constant)"

**Cause**: `plot_config` port name mismatch.

**Fix**: v1.10.1+ auto-fixes. Earlier versions: inspect `plot_config` in `generated/demo_main.py`.

### 6.3 "Simulation always outputs 0"

**Cause**: `heater_top_on` not set to 1 in role function, or `heater_command` is 0.

**Fix**: Check `F_StartOvenPid` is called and PID `setpoint` is set correctly.

### 6.4 "Add Physics missing on Controller Box"

**Cause**: ControllerBox imported from StaTable cannot take a physics model (by design).

**Fix**: Place physical models as separate components (`OvenChamber` etc.) and wire to ControllerBox.

---

## 7. Comparison with existing demos

| Item | This tutorial | `oven_control_statable.psbproj` |
|---|---|---|
| Control approach | **Role functions** (`F_SetTopHeaterDuty`) | **Gating** (`gate_layer="MwOven"`) |
| Heater control | 0-1 → 0-100 scaling inside ControllerBox | Gating passes through |
| Safety check | `F_CheckOvenOverheat` | Guaranteed by gating state |
| Quantitative result | Equivalent (ss=200.68, ovs=1.45%) | Equivalent |
| Realism | **High** (mirrors firmware) | Simple |

**Role-function approach mirrors firmware structure**, so you can verify control close to the real device.

---

## 8. Next steps

| Extension | How |
|---|---|
| **Use the MwSteam layer** | Implement `F_StartBoilerHeater` / `F_CalculateSteamPID` |
| **Use the MwGrill layer** | Implement `F_StartGrill` / `F_SetGrillDuty` |
| **Tune PID gains** | `tools/verify_pid.py` |
| **Reuse via Role Snippets** | Right-click → Register as Snippet |
| **Combine with `.psblog`** | Import Signal Log to compare with real logs (v1.10.0+) |

---

## References

- `examples/demos/oven_control_statable.psbproj` — gating-based completed demo
- `examples/demos/README.md` — all demos
- `tools/verify_statable_psbproj.py` — quantitative verification
- `tools/edit_role_function.py` — role function editor (CLI)
- `docs/SPEC_ROLE_SNIPPETS.md` — Role Snippets spec
