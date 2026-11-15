# 教程：从 StaTable XML 构建 PowerSimBuilder 演示

> **目标读者**: 首次使用 PowerSimBuilder (PSB) 的用户
> **所需时间**: 约 20 分钟
> **前提条件**: PSB v1.10.1 或更高版本、StaTable XML (`cooking_heater_controller.xml` 等)
> **参考演示**: `examples/demos/oven_control_statable.psbproj`

---

## 1. 概述

PowerSimBuilder 读取在 StaTable 中设计的 7 层状态机 (XML)，并与**物理模型**组合，运行 Python 仿真。

```
StaTable (设计)
   → cooking_heater_controller.xml
   → PSB (ControllerBox 配置 + 物理模型接线 + role 函数实现)
   → 生成 demo_main.py
   → 执行仿真
   → simulation_signals.png (温度控制波形)
```

本教程使用 **MwOven 层** (烤箱 PID 加热)，并通过 **ControllerBox 的 role 函数实现加热控制**。

### 控制架构（重要）

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

- **PID 输出 (0-1)** 输入到 ControllerBox 的 `heater_command` 端口
- **Role 函数 `F_SetTopHeaterDuty`** 将 0-1 缩放为 0-100
- **Role 函数 `F_CheckOvenOverheat`** 实现紧急停止
- **Role 函数 `F_ReadThermistors`** 计算温度传感器值
- **不使用**状态机 gating 参数（控制逻辑完全在 role 函数中）

### 完成时的性能

- `oven_current_temp` 从 25°C 升至 200°C
- PID 控制在 200°C 附近稳定 (ss ≈ 200.68°C, ovs ≈ 1.45%, settle ≈ 64.6s)
- 可通过 CLI 进行定量验证

---

## 2. 前提条件

### 2.1 必需文件

| 文件 | 位置 | 用途 |
|---|---|---|
| `cooking_heater_controller.xml` | `tests\\data\\scenarios\\cooking_heater\\` | StaTable XML |
| `PowerSimBuilder.exe` | `bin\\Debug\\net10.0-windows\\` | PSB 可执行文件 |

### 2.2 启动 PSB

```powershell
cd C:\\dev\\PowerSimBuilder
.\\bin\\Debug\\net10.0-windows\\PowerSimBuilder.exe
```

---

## 3. 分步操作

### Step 1: 新建项目

按 **Ctrl+N** 或选择 **File → New Project**。

**预期**: Canvas 完全清空。

### Step 2: 导入 StaTable XML

1. 点击工具栏 **"StaTable Import"** 按钮
2. 在 **"XML File"** 栏中点击 **[Browse...]**，选择 `cooking_heater_controller.xml`
3. 在预览中确认 XML 结构 (7 层状态机)
4. 点击 **[OK]**

**预期**: Canvas 上出现 **`Controller Box`** 组件。

> **注意**: `Controller Box` 的端口由 StaTable XML 自动派生。包括 `heater_command` (输入)、`heater_top_duty` (输出)、`oven_current_temp` (输入)、`error_code` (输出) 等。

### Step 3: 放置物理模型

从左侧组件面板拖放以下两个组件到 Canvas:

| 组件 | 类别 | 用途 |
|---|---|---|
| **PID Controller** | Control | 温度 PID 控制 |
| **OvenChamber** | Thermal | 烤箱热模型 |

**预期**: Canvas 上排列 3 个组件。

### Step 4: 接线 (4 条)

点击端口拖出连接线。**注意方向** (输出 → 输入):

| # | 源 (输出) | 目标 (输入) | 含义 |
|:---:|---|---|---|
| 1 | `PID Controller.control` | `Controller Box.heater_command` | PID 输出 (0-1) → ControllerBox |
| 2 | `Controller Box.heater_top_duty` | `OvenChamber.heater_top_duty` | 加热指令 (0-100) → 烤箱 |
| 3 | `OvenChamber.oven_current_temp` | `Controller Box.oven_current_temp` | 当前温度 → ControllerBox |
| 4 | `OvenChamber.oven_current_temp` | `PID Controller.measured` | 当前温度 → PID 反馈 |

**预期**: Canvas 上绘制 4 条连接线。

### Step 5: 设置 Controller Box 参数

**双击** `Controller Box` → 打开参数编辑界面。

| 参数 | 值 | 含义 |
|---|---|---|
| `generated_path` | `generated/CookingHeaterController_powersim.py` | 生成的 ControllerBox 路径 (自动设置) |
| `auto_events` | `demo_oven` | 演示用自动事件驱动 |

> **重要**: **不要设置** `gate_layer` / `gate_states` / `gate_input_port` / `gate_output_port` (保持为空)。控制逻辑在 Step 5.5 的 role 函数中实现。

### Step 5.5: 实现 Role 函数（核心）

实现 ControllerBox 的 role 函数，这是本演示控制逻辑的核心。

#### 5.5.1 什么是 Role 函数

ControllerBox 从 StaTable XML 生成 60 多个 role 函数 (`F_xxx`)。每个函数在 `# STABLE_USER_CODE_START` 与 `# STABLE_USER_CODE_END` 之间的内容**可由用户编辑**，仿真执行时被调用。

> **重要**: 如果状态机的 `do` 动作没有配置 role 函数调用，仅编辑 Python 函数体**不会被调用**。启用调用见 5.5.5。

#### 5.5.2 编辑 Role 函数的方法 (GUI)

1. **双击** `Controller Box` → 打开 Python 编辑器
2. 在左侧 **File Structure** 中展开 `📦 CookingHeaterController` → `MwOven`
3. 点击 `F_SetTopHeaterDuty` → 右侧显示函数体
4. 编辑 `# STABLE_USER_CODE_START` 与 `# STABLE_USER_CODE_END` 之间的内容
5. **Ctrl+S** 保存

#### 5.5.3 需实现的 5 个函数

| 函数 | 作用 | 调用时机 |
|---|---|---|
| `F_ReadThermistors` | 从 `oven_current_temp` 计算 `thermistor_top/middle/bottom` | 每个 tick |
| `F_StartOvenPid` | 设置 PID 控制启动标志 `heater_top_on` | `OvIdle → PidHeating` 转换时 |
| `F_StopOvenPid` | 停止控制、duty=0 | `PidHeating → OvIdle` 转换时 |
| `F_SetTopHeaterDuty` | 将 `heater_command` (0-1) → `heater_top_duty` (0-100) | 每个 tick (PidHeating 期间) |
| `F_CheckOvenOverheat` | 超过 250°C 时紧急停止 | 每个 tick |

#### 5.5.4 实现示例

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

**F_SetTopHeaterDuty** (核心):

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

#### 5.5.5 启用 Role 函数调用（重要）

如果 StaTable XML 中 `PidHeating` 状态的 `do` 动作为空，role 函数不会被自动调用。可通过以下方式之一启用:

| 方法 | 步骤 | 难度 |
|---|---|---|
| **(a) 编辑 XML** | 在 StaTable GUI 中，将 `F_SetTopHeaterDuty` 添加到 `PidHeating` 状态的 `do` 动作 → 重新导出 XML → PSB 重新导入 → 重新生成 ControllerBox | 中 |
| **(b) 编辑生成的 Python** | 在 `generated/CookingHeaterController_powersim.py` 的主循环中添加 `self.F_SetTopHeaterDuty()` 等 | 中 |
| **(c) 使用 Gating** | 不使用 role 函数，通过 `gate_layer` / `gate_states` 控制 | 易 |

**推荐**: (a) 或 (b)。

**(b) 示例** — 在 `generated/CookingHeaterController_powersim.py` 的 `update()` 内 `tick()` 之后添加:

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

#### 5.5.6 替代方案: `tools/edit_role_function.py` (CLI 批量编辑)

不使用 GUI 而用 CLI 批量编辑时:

```powershell
python tools\\edit_role_function.py generated/CookingHeaterController_powersim.py MwOven F_ReadThermistors --body "temp = self.inputs.get('oven_current_temp', 0) or 0; self.globals['thermistor_top'] = temp + 5; self.globals['thermistor_middle'] = temp; self.globals['thermistor_bottom'] = temp - 5; return 0"
```

### Step 6: 设置 PID Controller 参数

双击 `PID Controller`:

| 参数 | 值 | 含义 |
|---|---|---|
| `kp` | `0.01` | 比例增益 |
| `ki` | `0.0001` | 积分增益 |
| `kd` | `0.0` | 微分增益 |
| `out_min` | `0.0` | 输出下限 |
| `out_max` | `1.0` | 输出上限 |
| `setpoint` | `200.0` | 目标温度 (°C) |
| `anti_windup` | `False` | 抗积分饱和 |

> **注**: 此增益已通过定量验证 (ss=200.68, ovs=1.45%, settle=64.6s)。

### Step 7: 设置 OvenChamber 参数

双击 `OvenChamber`:

| 参数 | 值 | 含义 |
|---|---|---|
| `thermal_resistance` | `0.5` | 热阻 (K/W) |
| `thermal_capacity` | `200` | 热容 (J/K) |
| `ambient_temperature` | `25` | 环境温度 (°C) |
| `initial_temperature` | `25` | 初始温度 (°C) |
| `heater_power_max` | `1000` | 加热器最大功率 (W) |
| `sensor_offset_top` | `5` | 顶部热敏电阻偏移 |
| `sensor_offset_bottom` | `-5` | 底部热敏电阻偏移 |

### Step 8: 设置仿真时间 (可选)

在工具栏 `t_end` 输入框中设置 **`300`** (秒)。

### Step 9: 生成仿真

点击工具栏 **"Create Simulation"** 按钮。

**预期**:
- 打开 `PythonEditorForm`
- 生成 `generated/demo_main.py`
- `generated/CookingHeaterController_powersim.py` 保留 role 函数编辑内容

### Step 10: 执行仿真

在 `PythonEditorForm` 中点击 **"Run"** 按钮。

**预期**:
- 仿真完成消息
- 生成 `simulation_signals.png`
- 波形图显示温度控制曲线

---

## 4. 确认结果

### 4.1 波形图

打开 `simulation_signals.png`，确认以下内容:

| 信号 | 预期 |
|---|---|
| `oven_current_temp` | 从 25°C 升至 200°C，在 200°C 附近稳定 |
| `heater_top_duty` | 从 0% 开始，稳定后收敛到低 duty (10–30%) |

### 4.2 定量验证

```powershell
python tools\\verify_statable_psbproj.py <your-project.psbproj>
```

**预期输出**:
```
ss      = 200.68 degC   (稳态误差 -0.68°C)
ovs     = 1.45 %        (超调量)
settle  = 64.6 s        (稳定时间)
```

---

## 5. 保存与复用

### 5.1 保存项目

按 **Ctrl+S** 或选择 **File → Save As...** 保存为 `.psbproj`:

```
examples\\demos\\my_oven_demo.psbproj
```

### 5.2 加载

选择 **File → Open**，选中 `.psbproj`。

---

## 6. 故障排查

### 6.1 "编辑了 role 函数但波形仍为 0"

**原因**: role 函数未被调用 (XML `do` 动作为空)。

**解决**: 参见 5.5.5 的方法 (a) 或 (b) 启用调用。

### 6.2 "波形为直线 (恒定值)"

**原因**: `plot_config` 的 `port` 名称与实际输出端口不一致。

**解决**: v1.10.1 及以后已自动修复。较早版本请检查 `generated/demo_main.py` 的 `plot_config`。

### 6.3 "仿真结果始终为 0"

**原因**: role 函数未将 `heater_top_on` 设为 1，或 `heater_command` 为 0。

**解决**: 确认 `F_StartOvenPid` 被调用，且 PID `setpoint` 设置正确。

### 6.4 "Controller Box 上没有 Add Physics 按钮"

**原因**: 从 StaTable 导入的 ControllerBox 无法附加物理模型 (设计如此)。

**解决**: 将物理模型作为独立组件 (`OvenChamber` 等) 放置，并与 ControllerBox 接线。

---

## 7. 与现有演示的对比

| 项目 | 本教程 | `oven_control_statable.psbproj` |
|---|---|---|
| 控制方式 | **Role 函数** (`F_SetTopHeaterDuty`) | **Gating** (`gate_layer="MwOven"`) |
| 加热控制 | ControllerBox 内部 0-1 → 0-100 缩放 | Gating 直通 |
| 安全检查 | `F_CheckOvenOverheat` | 由 Gating 状态保证 |
| 定量性能 | 相当 (ss=200.68, ovs=1.45%) | 相当 |
| 现实性 | **高**（与固件相同的结构） | 简单 |

**Role 函数方式与固件结构相同**，可验证接近实际设备的控制。

---

## 8. 后续步骤

| 扩展 | 方法 |
|---|---|
| **使用 MwSteam 层** | 实现 `F_StartBoilerHeater` / `F_CalculateSteamPID` |
| **使用 MwGrill 层** | 实现 `F_StartGrill` / `F_SetGrillDuty` |
| **PID 增益优化** | `tools/verify_pid.py` 比较多个增益 |
| **使用 Role Snippets** | GUI 右键 → Register as Snippet 复用 |
| **结合 `.psblog`** | Import Signal Log 与实机日志对比 (v1.10.0+) |

---

## 参考

- `examples/demos/oven_control_statable.psbproj` — Gating 方式的完整示例
- `examples/demos/README.md` — 全部演示列表
- `tools/verify_statable_psbproj.py` — 定量验证工具
- `tools/edit_role_function.py` — role 函数编辑 (CLI)
- `docs/SPEC_ROLE_SNIPPETS.md` — Role Snippets 规格
