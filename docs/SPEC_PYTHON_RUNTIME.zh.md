# SPEC: Python 运行环境 — PowerSimBuilder

> **适用对象**: 最终用户（安装并使用 PowerSimBuilder 的用户）
> **目的**: 明确运行仿真所需的 Python 环境，消除安装配置的困惑
> **最后更新**: 2026-11-02
> **适用版本**: v1.8.0 及以后

---

## 1. 适用范围（Scope）

本文档规定 **从 PowerSimBuilder GUI 运行仿真所需的 Python 环境**。
开发/测试用依赖（如 pytest）不在本文档范围内。

---

## 2. Python 本体

| 项目 | 要求 | 备注 |
|---|---|---|
| 版本 | **3.9 及以上（推荐 3.11）** | GUI 需启动 `python.exe`，因此必须加入 PATH |
| 操作系统 | Windows 10 / 11 | 发布形式为 `.exe` + PowerShell 工具 |
| PATH | 勾选 "Add Python to PATH" | 未勾选时 GUI 无法启动 Python（见 §6） |

---

## 3. 依赖库管理方针

依赖库以 **随发行包提供的 `requirements.txt` 为唯一权威（single source of truth）** 进行管理。
用户安装时只需指定此文件，无需逐个指定库。

| 文件 | 位置 | 作用 |
|---|---|---|
| `requirements.txt` | 发行包根目录 | 依赖的实际清单。用户安装时唯一需要指定的文件 |

> 如需新增或更新库，请编辑 `requirements.txt`。下文 §4 仅为参考副本，实际清单始终以 `requirements.txt` 为准。

---

## 4. 依赖库清单（参考）

`requirements.txt` 内容如下：

```
fmpy==0.3.32
numpy>=1.24
matplotlib>=3.7
pyyaml
```

| 库 | 版本要求 | 用途 |
|---|---|---|
| **fmpy** | **`==0.3.32`（严格固定）** | FMU（物理模型）执行引擎。**API 差异会导致无法运行，必须固定版本**（Trap #57） |
| **numpy** | **`>=1.24`** | 仿真数值计算 |
| **matplotlib** | **`>=3.7`** | 结果图表显示 |
| **pyyaml** | 最新稳定版 | YAML 清单加载（`src/python_runtime/physics/manifest.py`） |

> ⚠️ **请勿修改 `fmpy==0.3.32` 这一行。** 其他版本在 FMU 解压/执行时会失败。
> 📌 已确认使用 `matplotlib`（演示、日志）和 `pyyaml`（清单加载）。`lxml` 未被 import，改用标准库 `xml.etree`。

---

## 5. 安装步骤（面向最终用户）

所需文件 **仅有 `requirements.txt` 一个**，随发行包提供。

### 步骤

1. 打开命令提示符（cmd）或 PowerShell。
2. 进入发行包文件夹。

   ```powershell
   cd <PowerSimBuilder 发行包文件夹>
   ```

3. 执行以下单条命令。

   ```powershell
   pip install -r requirements.txt
   ```

至此完成。无需逐个指定库。

---

## 6. 运行确认（自检）

```powershell
python -c "import fmpy, numpy; print('fmpy', fmpy.__version__); print('OK')"
```

**预期输出**：
```
fmpy 0.3.32
OK
```

出现上述两行即表示准备完成。若未出现，请参阅 §7。

---

## 7. 故障排查

| 症状 | 原因 | 处理方法 |
|---|---|---|
| 运行 FMU 报错 | fmpy 版本不符 | `pip install --force-reinstall -r requirements.txt` |
| 找不到 `python` | 未设置 PATH | 重新安装 Python 并勾选 "Add to PATH" |
| 找不到 `pip` | 未安装 pip | 执行 `python -m ensurepip` |
| 图表不显示 | 未安装 matplotlib | 重新执行 `pip install -r requirements.txt` |

---

## 8. 变更记录

| 日期 | 版本 | 变更内容 |
|---|---|---|
| 2026-11-02 | draft | 初版（requirements.txt 方式，固定 fmpy==0.3.32） |
