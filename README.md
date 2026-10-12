# PowerSimBuilder

[![CI](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml/badge.svg)](https://github.com/akashi-hideki/PowerSimBuilder/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-Framework%204.7.2-512BD4)](https://dotnet.microsoft.com/)

**Visual power-system modeling + Python simulation code generation.**

## Features

- Drag & drop circuit design
- Automatic Python code generation
- Python simulation runtime (NumPy)
- XML-based component library
- Japanese / English UI

## Quick Start

```powershell
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder
msbuild PowerSimBuilder.sln -t:restore
msbuild PowerSimBuilder.sln /p:Configuration=Release
.\bin\Release\PowerSimBuilder.exe
```

## Requirements

- Windows 10/11
- .NET Framework 4.7.2
- Visual Studio 2022 (or MSBuild 17+)
- Python 3.10+ (for simulation runtime)

## Development

### Build & Test

```powershell
msbuild PowerSimBuilder.sln -t:restore
msbuild PowerSimBuilder.sln /p:Configuration=Debug
dotnet test PowerSimBuilder.Tests\PowerSimBuilder.Tests.csproj
```

### Git Operations (ops-file based)

```powershell
python tools\git_runner.py --list      # Show planned commands
python tools\git_runner.py --dry-run   # Dry run
python tools\git_runner.py             # Apply
```

### Check CI Status

```powershell
powershell -ExecutionPolicy Bypass -File tools\check_actions.ps1
powershell -ExecutionPolicy Bypass -File tools\check_actions.ps1 -Watch
```

## Project Structure

```
PowerSimBuilder/
  .github/workflows/ci.yml      CI pipeline
  CanvasForm/                   GUI (WinForms)
  ComponentEdit/                Code generation
  SimulationGenerate/           Python runtime bridge
  SimPartsManege/               Component library
  parts/                        Utilities
  PowerSimBuilder.Tests/        xUnit tests
  tools/                        Dev tools
  docs/                         Documentation
    specs/                      Design specifications
```

## Documentation

- [Design Spec](docs/specs/SPEC_POWERSIM.md)
- [StaTable Integration (planned)](docs/specs/STATABLE_INTEGRATION.md)

## License

Apache-2.0 - see [LICENSE](LICENSE)

## Contributing

Issues and pull requests are welcome!
