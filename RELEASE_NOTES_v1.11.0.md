# PowerSimBuilder v1.11.0

**Release Date**: 2026-11-23

## Highlights

### 📦 GitHub FMU Catalog Browsing

Browse FMI Cross Check-style GitHub repository layouts directly in the Physics Model Import dialog.

- **Filter by FMI version, interface directory (`cs` / `me`), and platform.**
- Listing uses the GitHub repository tree API: the platform directory is located with one contents request and its tree is read recursively. If GitHub reports the tree as `truncated`, an error is shown instead of an incomplete list.
- **Optional GitHub API token** for authenticated repository-tree requests. The token is saved to application settings when **Load** is run with a `github:` source (typing it alone does not save it). It is not used for the GitHub Releases API and is not encrypted.
- Built-in catalog source presets are defined in `Resources/catalog_sources.json`.

### 🔗 Catalog → Import Workflow

Selecting a catalog entry transfers its FMU URL to the URL tab, requests network-access approval, downloads and parses the FMU, and then presents its variables for port selection in the File tab.

- The network-access dialog is shown for every URL download. **Always allow** stores the URL in `AppSettings.AllowedUrls`, but the stored list is not consulted, so the confirmation appears again on the next download.
- **GitHub Releases source**: only `.fmu` assets of the latest release are listed; ZIP assets are not extracted.
- **Import restriction**: the current importer accepts Co-Simulation FMUs only. Model Exchange entries can be browsed but cannot be imported.

### 📊 Plot Improvements

Generated simulation graphs now use one y-scale per variable (stacked subplot layout) instead of a single shared y-axis. The figure width is 1.5x wider for better readability.

- `PythonSimulationMainBuilder` generates `plot_signals()` with stacked subplots (one per variable), preventing large-range variables from dominating the y-axis scale.
- Figure width increased from 10 to 15 inches.

### 🎯 ControllerBox Port Selection (carried from v1.10.2)

Choose input/output ports from `globals` during XML import. Auto-suggest pre-selects sensible defaults. Selection is persisted in `.psbproj` and restored on reload. Right-click any ControllerBox → **"Select Ports..."** to modify ports after import.

---

## Changed

- FMU import documentation now describes the current Catalog → URL → File workflow and the current Co-Simulation-only import restriction.

## Files changed (v1.11.0)

| File | Change |
|---|---|
| `parts/Physics/PhysicsCatalogRegistry.cs` | GitHub API client, catalog source presets, tree API |
| `tests/PhysicsCatalogRegistry.cs` | GitHub API client tests |
| `tests/PhysicsCatalogSourceTests.cs` | Catalog source preset tests |
| `Resources/catalog_sources.json` | Built-in catalog source presets |
| `SimulationGenerate/PythonSimulationMainBuilder.cs` | Stacked subplot layout + 1.5x wider figure in generated `plot_signals()` |
| `docs/FMU_CATALOG_GUIDE.md` | Catalog → URL → File workflow documentation |
| `docs/PHYSICS_MODEL_INTEGRATION.md` | Co-Simulation-only restriction clarified |

## Changelog

See [CHANGELOG.md](CHANGELOG.md#1110---2026-11-23) for the full history.

## Documentation

- [FMU Catalog and Import Guide](docs/FMU_CATALOG_GUIDE.md)
- [Physics Model Integration](docs/PHYSICS_MODEL_INTEGRATION.md)
- [StaTable XML → PSB Tutorial (ja / en / zh)](docs/TUTORIAL_STATABLE_DEMO.ja.md)

---

**Full Changelog**: https://github.com/akashi-hideki/PowerSimBuilder/compare/v1.10.2...v1.11.0# PowerSimBuilder v1.11.0

**Release Date**: 2026-11-23

## Highlights

### 📦 GitHub FMU Catalog Browsing

Browse FMI Cross Check-style GitHub repository layouts directly in the Physics Model Import dialog.

- **Filter by FMI version, interface directory (`cs` / `me`), and platform.**
- Listing uses the GitHub repository tree API: the platform directory is located with one contents request and its tree is read recursively. If GitHub reports the tree as `truncated`, an error is shown instead of an incomplete list.
- **Optional GitHub API token** for authenticated repository-tree requests. The token is saved to application settings when **Load** is run with a `github:` source (typing it alone does not save it). It is not used for the GitHub Releases API and is not encrypted.
- Built-in catalog source presets are defined in `Resources/catalog_sources.json`.

### 🔗 Catalog → Import Workflow

Selecting a catalog entry transfers its FMU URL to the URL tab, requests network-access approval, downloads and parses the FMU, and then presents its variables for port selection in the File tab.

- The network-access dialog is shown for every URL download. **Always allow** stores the URL in `AppSettings.AllowedUrls`, but the stored list is not consulted, so the confirmation appears again on the next download.
- **GitHub Releases source**: only `.fmu` assets of the latest release are listed; ZIP assets are not extracted.
- **Import restriction**: the current importer accepts Co-Simulation FMUs only. Model Exchange entries can be browsed but cannot be imported.

### 📊 Plot Improvements

Generated simulation graphs now use one y-scale per variable (stacked subplot layout) instead of a single shared y-axis. The figure width is 1.5x wider for better readability.

- `PythonSimulationMainBuilder` generates `plot_signals()` with stacked subplots (one per variable), preventing large-range variables from dominating the y-axis scale.
- Figure width increased from 10 to 15 inches.

### 🎯 ControllerBox Port Selection (carried from v1.10.2)

Choose input/output ports from `globals` during XML import. Auto-suggest pre-selects sensible defaults. Selection is persisted in `.psbproj` and restored on reload. Right-click any ControllerBox → **"Select Ports..."** to modify ports after import.

---

## Changed

- FMU import documentation now describes the current Catalog → URL → File workflow and the current Co-Simulation-only import restriction.

## Files changed (v1.11.0)

| File | Change |
|---|---|
| `parts/Physics/PhysicsCatalogRegistry.cs` | GitHub API client, catalog source presets, tree API |
| `tests/PhysicsCatalogRegistry.cs` | GitHub API client tests |
| `tests/PhysicsCatalogSourceTests.cs` | Catalog source preset tests |
| `Resources/catalog_sources.json` | Built-in catalog source presets |
| `SimulationGenerate/PythonSimulationMainBuilder.cs` | Stacked subplot layout + 1.5x wider figure in generated `plot_signals()` |
| `docs/FMU_CATALOG_GUIDE.md` | Catalog → URL → File workflow documentation |
| `docs/PHYSICS_MODEL_INTEGRATION.md` | Co-Simulation-only restriction clarified |

## Changelog

See [CHANGELOG.md](CHANGELOG.md#1110---2026-11-23) for the full history.

## Documentation

- [FMU Catalog and Import Guide](docs/FMU_CATALOG_GUIDE.md)
- [Physics Model Integration](docs/PHYSICS_MODEL_INTEGRATION.md)
- [StaTable XML → PSB Tutorial (ja / en / zh)](docs/TUTORIAL_STATABLE_DEMO.ja.md)

---

**Full Changelog**: https://github.com/akashi-hideki/PowerSimBuilder/compare/v1.10.2...v1.11.0
