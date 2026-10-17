# Publication Readiness Audit

**Date:** 2026-10-17
**Positioning:** A - Companion to StaTable (Apache 2.0 ecosystem)
**Budget:** 0 yen (VS Community + OSS only)

## 1. Current Status

| Metric | Value |
|---|---|
| C# tests ([Fact]/[Theory]) | 143 |
| Python tests (def test_) | 389 |
| Japanese string literals | ~195 |
| CI workflows | 2 (ci.yml=C#, python-tests.yml=Python) |
| .bak files on disk | 66 (gitignored, not committed) |
| bin/Debug size | 1.88 MB |
| Platform | .NET Framework 4.7.2 (Windows only) |
| License | Apache 2.0 |

## 2. Quick Wins Completed (Phase 1)

- [x] LICENSE (Apache 2.0 full text)
- [x] NOTICE
- [x] .github/ISSUE_TEMPLATE/{bug_report,feature_request}.md
- [x] .github/PULL_REQUEST_TEMPLATE.md
- [x] .gitignore: fix corrupted scenario-data block (3 patterns now active)
- [x] PowerSimBuilder.csproj: PackageLicenseExpression, Copyright, Authors, RepositoryUrl, RepositoryType
- [x] docs/PUBLICATION_AUDIT.md (this file)

## 3. Gaps vs StaTable

| Area | StaTable | PowerSimBuilder | Priority |
|---|---|---|---|
| License | Apache 2.0 | Apache 2.0 | DONE |
| README | Multi-lang + demo GIF | Missing | P0 |
| i18n | EN/ZH 97.4% | JA only (195 strings) | P0 |
| CI | 7 jobs | 2 jobs (C# + Python) | P1 |
| Platform | Win/Linux/mac (Python) | Windows only (.NET Fx 4.7.2) | P1 |
| Docs site | None | None | P2 |
| Packaging | PyPI | .exe | P1 |

## 4. Roadmap (4 weeks, 0 yen)

### Week 1: Quick Wins + Audit (this session)
- [x] LICENSE, NOTICE, GitHub templates
- [x] .gitignore fix, csproj metadata
- [x] docs/PUBLICATION_AUDIT.md

### Week 2: .NET 8 migration
- [ ] Install .NET 8 SDK (free, VS Community compatible)
- [ ] PowerSimBuilder.csproj: net8.0-windows
- [ ] PowerSimBuilder.Tests.csproj: net8.0-windows
- [ ] Verify 143 C# tests pass
- [ ] ci.yml: update to .NET 8 + windows-latest
- [ ] Single-file publish config

### Week 3: i18n foundation + English
- [ ] i18n mechanism (JSON-based, VS-independent)
- [ ] Extract 195 JA strings -> Resources/i18n/ja.json
- [ ] Translate to Resources/i18n/en.json
- [ ] Language switch UI
- [ ] i18n completeness test (all keys covered)

### Week 4: Docs + public release
- [ ] README.md (EN primary, JA secondary)
- [ ] Demo GIF / screenshots
- [ ] docs/QUICKSTART.md, docs/ARCHITECTURE.md
- [ ] GitHub Pages (MkDocs, free)
- [ ] v1.0.0 tag + GitHub Release

## 5. Decisions

| Decision | Choice | Rationale |
|---|---|---|
| Positioning | A: StaTable companion | Ecosystem synergy |
| License | Apache 2.0 | Unified with StaTable |
| Platform | B: .NET 8 + WinForms | Free SDK, CI-friendly |
| Language | EN primary + JA, ZH later | 195 strings hand-translatable |
| Timeline | 1 month to v1.0.0 | Staged release |
| Budget | 0 yen | VS Community + OSS |

## 6. Known Issues

| Issue | Impact | Fix |
|---|---|---|
| .NET Framework 4.7.2 | Windows-only, VS-dependent | Week 2: .NET 8 |
| Only Japanese UI | Limits audience | Week 3: i18n |
| README missing | No public entry point | Week 4 |
| 66 .bak files on disk | Clutter (not in git) | Optional cleanup |
