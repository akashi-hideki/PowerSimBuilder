# PowerSimBuilder Technical White Paper

**Version**: 1.9
**Date**: 2026-11-08
**Author**: Hideki Akashi
**Status**: Confidential — For Evaluation Purposes Only

---

## 1. Executive Summary

PowerSimBuilder is a **simulation companion for StaTable**. It enables
embedded engineers to verify state machine designs against physical
models (FMU) on a Windows PC before deploying to MCU hardware.

**Key Value Propositions:**
- **StaTable integration** — design in StaTable, simulate in PowerSimBuilder
- **FMI Cross Check catalog** — 226 models accessible in one click
- **Network access control** — explicit consent dialog before any URL fetch
- **Multi-language** — English / Simplified Chinese
- **Apache-2.0 licensed** — commercially usable

**Current Status:** v1.9.0 released (2026-11-08).
- Python tests: 457 passed
- C# tests: 599 passed (2 skipped)
- UI E2E: 16 passed (1 skipped)
- RoleSnippet tests: 30 passed
- **Simulation execution: operational** (StaTable 7-layer + OvenChamber + PID validated at ss=200.68degC / ovs=1.45% / settle=64.6s)

---

## 2. Technical Overview

### 2.1 Architecture

- **UI**: .NET 10 WinForms (Windows-only)
- **FMU parsing**: `fmpy` via Python subprocess
- **Catalog**: JSON-based source list (`catalog_sources.json`)
- **i18n**: JSON resources (`Resources/i18n/en.json`, `zh.json`)

### 2.2 Workflow

1. Design state machine in StaTable
2. Import physics model (FMU) in PowerSimBuilder
3. (Under development) Run simulation

### 2.3 Model Catalog

| Source | Content | License |
|--------|---------|---------|
| FMI Cross Check | 226 models (FMI 1.0/2.0/3.0, multiple tools) | Modelica Association |
| Reference-FMUs | 0 models (zip-only distribution) | Modelica Association |
| Custom GitHub repo | User-specified | varies |

---

## 3. Market Background

[To be written — compare with StaTable's version]

---

## 4. Technical Advantages

| Feature | PowerSimBuilder | Typical Competitor |
|---------|-----------------|-------------------|
| StaTable integration | Native | None |
| FMI catalog | 226 models, one click | Manual download |
| Network consent dialog | Built-in | Varies |
| License | Apache-2.0 | Proprietary |

---

## 5. Intellectual Property

### 5.1 Development Timeline

All commits are timestamped via Git. Public release history:
- v1.5.0: 2026-10-24 — Catalog tab UI
- v1.5.1: 2026-10-24 — catalog_probe + sources
- v1.6.0: 2026-10-25 — Sources dropdown + Release asset

### 5.2 OSS License Compliance

- **No GPL/AGPL in runtime dependencies**
- .NET 10 (MIT), fmpy (BSD-2-Clause)
- Full statement: `docs/ip/OSS_COMPLIANCE.md`

### 5.3 Copyright

- **Author**: Hideki Akashi
- **License**: Apache-2.0

---

## 6. Roadmap

| Phase | Content | Status |
|-------|---------|--------|
| MVP-1.2 | Physics model insertion (FMU) | ✅ |
| MVP-β | Model download helper | ✅ |
| MVP-γ | Catalog tab | ✅ v1.6.0 |
| **MVP-2** | **Simulation execution** | **Under development** |
| MVP-3 | Modelica `.mo` source import | Planned |
| MVP-4 | Subprocess isolation | Planned |

---

## 7. Contact

**Author**: Hideki Akashi
**Repository**: https://github.com/akashi-hideki/PowerSimBuilder
**Email**: akashi.hideki@gmail.com

---

*This document is confidential and provided for evaluation purposes only.*
