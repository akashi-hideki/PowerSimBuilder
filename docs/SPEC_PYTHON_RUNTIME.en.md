# SPEC: Python Runtime Environment — PowerSimBuilder

> **Audience**: End users (people who install and use PowerSimBuilder)
> **Purpose**: Define the Python environment required to run simulations, eliminating setup confusion
> **Last updated**: 2026-11-02
> **Applies to**: v1.8.0 and later

---

## 1. Scope

This document defines the **Python environment required to run simulations from the PowerSimBuilder GUI**.
Development/test dependencies (e.g., pytest) are out of scope.

---

## 2. Python Interpreter

| Item | Requirement | Notes |
|---|---|---|
| Version | **3.9 or later (3.11 recommended)** | Must be on PATH because the GUI launches `python.exe` |
| OS | Windows 10 / 11 | Distribution assumes `.exe` + PowerShell tools |
| PATH | "Add Python to PATH" enabled | If disabled, the GUI cannot launch Python (see §6) |

---

## 3. Dependency Management Policy

Dependencies are managed through the **`requirements.txt` shipped with the distribution, which is the single source of truth**.
Users only need to point to this file during installation; specifying individual libraries is not required.

| File | Location | Role |
|---|---|---|
| `requirements.txt` | Distribution root | The actual dependency list. The only file users specify during installation |

> To add or update libraries, edit `requirements.txt`. Section §4 below is a reference copy; the authoritative list always lives in `requirements.txt`.

---

## 4. Dependency List (Reference)

Contents of `requirements.txt`:

```
fmpy==0.3.32
numpy>=1.24
lxml
matplotlib>=3.7
```

| Library | Version | Purpose |
|---|---|---|
| **fmpy** | **`==0.3.32` (strictly pinned)** | FMU (physical model) execution engine. **Must be pinned — API differences break execution** (Trap #57) |
| **numpy** | **`>=1.24`** | Numerical computation for simulation |
| **lxml** | Latest stable | Design XML loading (`oven_control_from_design_xml`) — *to be verified* |
| **matplotlib** | **`>=3.7`** | Result graph display — *to be verified* |

> ⚠️ **Do not change the `fmpy==0.3.32` line.** Other versions fail during FMU extraction/execution.
> 📌 Whether `lxml` / `matplotlib` are actually imported is pending verification; remove them from `requirements.txt` if unused.

---

## 5. Installation (For End Users)

Only **one file — `requirements.txt`** — is needed. It ships with the distribution.

### Steps

1. Open Command Prompt or PowerShell.
2. Navigate to the distribution folder.

   ```powershell
   cd <PowerSimBuilder distribution folder>
   ```

3. Run the single command below.

   ```powershell
   pip install -r requirements.txt
   ```

That's it. No individual libraries need to be specified.

---

## 6. Verification (Self-check)

```powershell
python -c "import fmpy, numpy; print('fmpy', fmpy.__version__); print('OK')"
```

**Expected output**:
```
fmpy 0.3.32
OK
```

If these two lines appear, setup is complete. Otherwise, see §7.

---

## 7. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| FMU execution error | Wrong fmpy version | `pip install --force-reinstall -r requirements.txt` |
| `python` not found | PATH not set | Reinstall Python with "Add to PATH" enabled |
| `pip` not found | pip missing | Run `python -m ensurepip` |
| Graphs not displayed | matplotlib missing | Re-run `pip install -r requirements.txt` |
| XML demo error | lxml missing | Re-run `pip install -r requirements.txt` |

---

## 8. Change History

| Date | Version | Change |
|---|---|---|
| 2026-11-02 | draft | Initial draft (requirements.txt approach, fmpy==0.3.32 pinned) |
