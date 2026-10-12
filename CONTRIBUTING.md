# Contributing to PowerSimBuilder

Thank you for your interest in contributing! This document outlines the
process for contributing to the project.

## Ways to Contribute

- Report bugs via [Issues](https://github.com/akashi-hideki/PowerSimBuilder/issues)
- Suggest features via [Issues](https://github.com/akashi-hideki/PowerSimBuilder/issues)
- Submit pull requests
- Improve documentation
- Add tests

## Development Setup

### Requirements

- Windows 10/11
- Visual Studio 2022 (Community or higher)
  - OR MSBuild 17+
  - .NET Framework 4.7.2 targeting pack
- Python 3.10+ (for simulation runtime and dev tools)
- Git

### Clone and Build

```powershell
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder

# Restore NuGet packages
msbuild PowerSimBuilder.sln -t:restore

# Build
msbuild PowerSimBuilder.sln /p:Configuration=Debug

# Run tests
dotnet test PowerSimBuilder.Tests\PowerSimBuilder.Tests.csproj
```

### Git Operations (ops-file based)

We use a declarative ops file to make git operations explicit and reviewable:

```powershell
# 1. Edit tools/git_ops.txt
notepad tools\git_ops.txt

# 2. Review planned commands
python tools\git_runner.py --list

# 3. Dry-run
python tools\git_runner.py --dry-run

# 4. Apply
python tools\git_runner.py
```

**Never commit directly without using the ops file** for multi-step changes.

## Pull Request Process

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/your-feature`
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass: `dotnet test PowerSimBuilder.Tests`
6. Update `CHANGELOG.md` under `[Unreleased]`
7. Commit using the ops file (see above)
8. Push and open a Pull Request

### PR Title Convention

```
<type>: <short description>

Types:
  feat:     New feature
  fix:      Bug fix
  docs:     Documentation only
  test:     Test additions
  refactor: Code restructuring
  chore:    Maintenance tasks
  build:    Build system / CI
```

### PR Description

Include:
- **What** was changed
- **Why** it was changed
- **How** to test it
- Any breaking changes

## Coding Standards

- Follow existing code style (see `.editorconfig`)
- Use C# 7.3+ features where appropriate
- Add XML doc comments for public APIs
- Keep methods small and focused
- Write tests for new code

## Testing

- All new features must have unit tests
- Tests use xUnit 2.9.2
- Run tests before pushing: `dotnet test PowerSimBuilder.Tests`

## Code of Conduct

Be respectful, constructive, and professional. We welcome contributors of
all backgrounds and experience levels.

## Questions?

Open an issue or contact the maintainer.

## License

By contributing, you agree that your contributions will be licensed under
the Apache License 2.0.
