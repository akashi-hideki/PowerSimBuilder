## v1.3.0 — Full i18n support (English / Chinese)

### New
- **Full i18n coverage** for all user-visible UI in English and Chinese:
  - MainForm toolbar + Tools menu (`导入物理模型`, `工具(&T)`)
  - PhysicsModelPickerForm (title, 4 buttons, columns, status bar)
  - PhysicsImportForm (labels, buttons, error messages)
  - PythonEditorForm right-click context menu (7 items)
  - KeyboardShortcutsForm (F1 dialog: title, headers, sections, rows)
- **Import Model button** in PhysicsModelPickerForm (opens PhysicsImportForm directly)
- **Tooltips** for Import Model and Open Model Folder buttons

### Improved
- Picker layout: wider panel (720px) and window (920px) to prevent button overlap at high DPI
- Picker columns localized (Model / In / Out / FMI)
- 63 new i18n keys added (en + zh)

### Fixed
- zh.json translation gaps (EditPanel.BtnPhysics, physics insertion messages)
- JSON i18n files: trailing comma issues repaired
- Duplicate `[OK] Released` print in `tools/release.py`
- CS8622 nullable warning in `OnImportModelClick`

### Tests
- **L2**: 431 passed, 2 skipped
- **L3 (FlaUI)**: 5 passed (LaunchApp / DoubleClick / AddPhysics / PickerImportButton / ImportButtonOpens)

### Known limitations
- Japanese UI is out of scope (English-only policy, `ja.json` not bundled)
- L3 UI tests may fail on GitHub Actions (Session 0 limitation) — `continue-on-error: true`
