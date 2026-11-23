# FMU Catalog and Import Guide

This guide describes the **Catalog**, **URL**, and **File** tabs in the Physics Model Import dialog.

## 1. Catalog source types

The Catalog tab supports the following source forms:

| Source form | Purpose |
|---|---|
| `github:<owner>/<repo>|<version>|<cs-or-me>|<platform>` | Browse an FMI Cross Check-style repository tree through the GitHub API |
| GitHub repository URL | List FMU assets published in GitHub Releases |
| JSON catalog URL | Load a catalog document through the Physics Catalog registry |

Built-in presets are defined in `Resources/catalog_sources.json`; the available list may vary by application build.

## 2. GitHub repository-tree source syntax

Use this format exactly:

```text
github:<owner>/<repo>|<FMI version>|<type>|<platform>
```

Example:

```text
github:modelica/fmi-cross-check|2.0|cs|win64
```

The browser expects this repository layout below the selected platform directory:

```text
fmus/<FMI version>/<type>/<platform>/<Tool>/<ToolVersion>/<Model>/<Model>.fmu
```

| Field | Example | Meaning |
|---|---|---|
| `<owner>/<repo>` | `modelica/fmi-cross-check` | GitHub repository owner and name |
| `<FMI version>` | `2.0` | FMI version directory |
| `<type>` | `cs` | `cs` = Co-Simulation; `me` = Model Exchange |
| `<platform>` | `win64` | Platform directory in the repository |

The client resolves the repository default branch, locates the platform directory, and reads its recursive Git tree. If GitHub marks the returned tree as truncated, the client reports an error instead of presenting a potentially incomplete list.

## 3. GitHub API token

The token is optional. For `github:` sources it is sent as an `Authorization` header with the GitHub repository API requests (default-branch lookup, platform-directory lookup and the recursive tree request) and can be used to avoid the lower unauthenticated API rate limit. It is **not** used for the GitHub Releases API.

- Enter the token in the **GitHub API Key** field at the bottom of the Catalog tab.
- Use the adjacent show/hide control when you need to inspect the entered value.
- The value is saved through `AppSettings` when you click **Load** with a `github:` source. Entering it without loading does not save it.
- The token is stored in the application settings file as plain text; it is not encrypted.
- Do not include the token in screenshots, issue reports, source files, or shared logs.
- Use a token with only the access required for the repositories you browse. Public repository browsing does not require write access.

A detected exhausted rate limit is reported separately from other HTTP 403 errors.

## 4. Catalog-to-import procedure

1. Open **Import Physics Model**.
2. Select the **Catalog** tab.
3. Choose a built-in source or enter a custom source.
4. For a GitHub repository-tree source, enter the `github:` specification. Optionally enter a GitHub API token.
5. Click **Load** and wait for the model list.
6. Select an FMU and click **Download**, or double-click the row.
7. The selected FMU URL is transferred to the **URL** tab and the download flow starts.
8. Review the network-access confirmation. The download proceeds only if access is approved. The dialog may offer an **Always allow** option; if selected, the URL is saved in application settings (`AppSettings.AllowedUrls`), but the saved list is not used to skip the dialog. The confirmation appears again the next time the same URL is downloaded.
9. After a successful download, the application parses the FMU and switches to the **File** tab.
10. Select the desired input/output variables, set the model name, and click **Import**.

**Important:** selecting an entry in the Catalog is not the same as importing it. Network approval, downloading, parsing, port selection, and the final Import action are separate steps. Ports are not automatically all selected.

## 5. Current limitations

- The importer expects an FMU archive containing `modelDescription.xml`.
- The current import dialog accepts **Co-Simulation FMUs only**.
- Model Exchange (`me`) entries can be browsed in a compatible GitHub tree, but an ME FMU is rejected by the importer.
- Only variables whose causality is `input` or `output` are offered for port selection.
- `.mo` Modelica source files are not accepted directly; build an FMU first.
- A GitHub Releases source uses the latest release only and lists only assets whose name ends in `.fmu` (case-insensitive). ZIP and other assets are not listed.
- ZIP assets are not extracted automatically, and FMUs inside a ZIP are not searched.
- The GitHub API token is not used for the GitHub Releases API.
- A repository that publishes only ZIP assets does not become a browsable FMU repository tree.

## 6. Troubleshooting

| Symptom | Likely cause | Action |
|---|---|---|
| `Invalid source` | The `github:` specification does not contain four pipe-separated fields | Check `owner/repo|version|cs-or-me|platform` |
| `Path not found` | The repository does not contain the requested FMI/type/platform path | Check the repository layout and each filter field |
| GitHub API rate limit exceeded | The unauthenticated API quota is exhausted | Enter a GitHub API token and retry |
| GitHub API 403 | An HTTP 403 other than the specifically detected exhausted rate limit | Check repository access and token configuration |
| No models found | No `.fmu` files match the selected path/layout | Check FMI version, type, platform, and repository structure |
| Unsupported FMI type | The downloaded FMU is Model Exchange | Use a Co-Simulation FMU |
| Network confirmation cancelled | Access was not approved | Retry only if the source is trusted and the download is intended |
