## Highlights

### Role Snippets — Register

Extract the role function at the caret as a reusable snippet, then
insert it into any other target function on the canvas.

- **Right-click** inside a function -> `Register as Snippet...`
- **Toolbar** `Register Snippet` (visible only when the file contains
  `STABLE_USER_CODE_START` markers — component-agnostic)
- **Insert Snippet** with `(snippet's own)` / cross-function modes
- i18n 12 new keys (en / zh, 374 total)

### End-to-end automation

| Test | Result |
|---|---|
| `RoleSnippetRegistrarTests` | 8 (extract / register / validation) |
| `RoleSnippetStorageTests` | 6 (index.json + .py I/O) |
| `RoleSnippetVisibilityTests` | 15 (L1 marker check + L2 form visibility) |
| `RoleSnippetRegisterE2ETests` | 1 (FlaUI E2E, register dialog -> file creation) |

`verify_all.ps1` now runs `[4c]` RoleSnippet unit tests and `[4d]`
Register FlaUI E2E on every verification pass.

### Tooling hardening

- **Trap #80 mitigation**: `gcommit.ps1` stops any running
  `PowerSimBuilder.exe` before git operations (prevents file locks).
- **Trap #85 mitigation**: `verify_all.ps1` `[4/6]` E2E sleeps 3 s and
  retries once on failure — observed in practice during this release.

## Fixed

- `SetPythonCode` / `LoadFile` now refresh Register Snippet visibility
  automatically (B-3c). No more hidden button after programmatic code changes.
- `menuItemRoleSnippets` follows marker presence (was always visible).
- **Trap #84**: inline regex `@"^\\\\s*..."` (double backslash) broke
  marker detection — extracted to `RoleSnippetRegistrar.ContainsRoleMarkers`.
- **Trap #86**: `ShowDialog` + `ShowInTaskbar=false` modals are not in the
  UIA3 desktop tree — Win32 `EnumWindows` fallback added
  (Trap #15 recurrence). `TextBox.Name` returns the AutomationId, not the
  value; use `AsTextBox().Text`.

## Testing

| Suite | Result |
|---|---|
| C# unit tests | 599 (597 pass / 2 skip / 0 fail) |
| UI E2E | 16 (15 pass / 1 skip / 0 fail) |
| RoleSnippet tests | 30 |
| Python | 463 pass |

## Changelog

See [CHANGELOG.md](CHANGELOG.md#190---2026-11-07).
