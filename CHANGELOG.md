# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-10-12

### Added

- Visual power-system modeling GUI (WinForms)
- Python simulation code generation
- Component library with XML persistence
- `PowerSimBuilder.Tests` project with 14 xUnit tests
  - PointJsonConverter tests
  - ConnectionGraph tests
  - PythonSimulationMainBuilder tests
  - LogcatTraceHelper smoke tests
- GitHub Actions CI workflow (Windows, Release build)
- `tools/git_runner.py` - declarative ops-file based git operations
- `tools/check_actions.ps1` - GitHub Actions status checker
- Apache-2.0 LICENSE
- README with CI badge and Quick Start
- `docs/specs/SPEC_POWERSIM.md` - design specification

### Fixed

- `LogcatTraceHelper.IsUserDefinedClass` null-argument bug (crashed when `appNamespace` was null)
- Duplicate `Costura.Fody` package reference (unified to 6.0.0 with `PrivateAssets=All`)

### Changed

- Migrated repository out of OneDrive (`C:\dev\PowerSimBuilder`)
- Enabled modern C# (`LangVersion=latest`, `PlatformTarget=x64`, `GenerateAssemblyInfo=true`)
- Renamed `readme.md` (spec) to `docs/specs/SPEC_POWERSIM.md`
- Added comprehensive `.gitignore`

[Unreleased]: https://github.com/akashi-hideki/PowerSimBuilder/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v0.1.0
