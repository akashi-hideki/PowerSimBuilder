# FMU目录与导入指南

本文介绍 Physics Model Import 窗口中的 **Catalog / URL / File** 选项卡。

## 1. 目录来源格式

| 来源格式 | 用途 |
|---|---|
| `github:<owner>/<repo>|<version>|<cs-or-me>|<platform>` | 通过 GitHub API 浏览 FMI Cross Check 风格的仓库目录树 |
| GitHub 仓库 URL | 列出 GitHub Releases 中发布的 FMU 资源 |
| JSON 目录 URL | 通过 Physics Catalog Registry 加载 JSON 目录 |

内置预设定义在 `Resources/catalog_sources.json` 中。不同应用程序构建版本中的预设列表可能不同。

## 2. GitHub 仓库目录语法

```text
github:<owner>/<repo>|<FMI version>|<type>|<platform>
```

示例：

```text
github:modelica/fmi-cross-check|2.0|cs|win64
```

浏览器要求仓库采用以下目录结构：

```text
fmus/<FMI version>/<type>/<platform>/<Tool>/<ToolVersion>/<Model>/<Model>.fmu
```

- `<owner>/<repo>`：GitHub 仓库所有者和仓库名
- `<FMI version>`：FMI 版本目录，例如 `2.0`
- `<type>`：`cs` 表示 Co-Simulation，`me` 表示 Model Exchange
- `<platform>`：仓库中的平台目录，例如 `win64`

客户端会先获取仓库的默认分支，再定位平台目录并读取递归 Git Tree。如果 GitHub 返回的 Tree 被标记为 truncated（不完整），客户端会报告错误，而不会显示可能不完整的列表。

## 3. GitHub API Token

Token 为可选项。使用 `github:` 来源时，它会作为 Authorization 请求头用于 GitHub 仓库 API 请求（获取默认分支、定位平台目录、读取递归 Tree），可用于避免未认证请求较低的 API 速率限制。GitHub Releases API 不使用该 Token。

- 在 Catalog 选项卡底部的 **GitHub API Key** 输入框中填写。
- 可使用旁边的显示／隐藏控件检查输入值。
- 使用 `github:` 来源点击 **Load** 时，应用程序会通过 `AppSettings` 保存该值。仅输入而不执行 Load 不会保存。
- Token 不会被加密，而是以明文形式保存在应用程序设置文件中。
- 不要在截图、问题报告、源代码或共享日志中暴露 Token。
- 请使用满足仓库浏览需求的最小权限 Token。浏览公开仓库不需要写入权限。

检测到速率限制耗尽时，程序会与其他 HTTP 403 错误分别报告。

## 4. 从目录导入 FMU

1. 打开 **Import Physics Model**。
2. 选择 **Catalog** 选项卡。
3. 选择内置预设，或输入自定义目录来源。
4. 使用 GitHub 仓库目录树时，输入上述 `github:` 格式；也可以选择输入 GitHub API Token。
5. 点击 **Load**，等待模型列表加载。
6. 选择一个 FMU，点击 **Download**（也可以双击该行）。
7. 所选 FMU 的 URL 会传递到 **URL** 选项卡，并开始下载流程。
8. 在网络访问确认对话框中进行授权。未获授权时不会继续下载。如果选择 **Always allow**（始终允许），该 URL 会保存到应用程序设置（`AppSettings.AllowedUrls`）中。但程序不会根据已保存的 URL 跳过确认，下次下载时仍会显示确认对话框。
9. 下载成功后，程序解析 FMU 并切换到 **File** 选项卡。
10. 选择要公开为端口的输入／输出变量，设置模型名称，然后点击 **Import**。

**注意：** 选择目录条目并不代表已经完成导入。网络授权、下载、解析 FMU、选择端口以及最终 Import 操作是独立步骤。程序不会自动选中全部端口。

## 5. 当前限制

- FMU 压缩包中必须包含 `modelDescription.xml`。
- 当前导入窗口仅接受 **Co-Simulation FMU**。
- 可以在目录中浏览 Model Exchange (`me`) 条目，但导入器会拒绝 ME FMU。
- 端口选择列表只显示 causality 为 `input` 或 `output` 的变量。
- `.mo` Modelica 源文件不能直接导入；请先生成 FMU。
- GitHub Releases 来源仅使用最新的 Release，只列出名称以 `.fmu` 结尾的资源（不区分扩展名大小写）。ZIP 等其他资源不会显示。
- 不支持自动解压 ZIP 资源，也不会搜索 ZIP 内部的 FMU。
- GitHub Releases API 不使用 GitHub API Token。
- 仅发布 ZIP 资源的仓库不能通过 GitHub 目录树方式浏览。

## 6. 故障排查

| 现象 | 可能原因 | 处理方法 |
|---|---|---|
| `Invalid source` | `github:` 格式没有包含四个字段 | 检查 `owner/repo|version|cs-or-me|platform` 格式 |
| `Path not found` | 仓库中不存在指定的 FMI／type／platform 路径 | 检查仓库目录结构及各筛选字段 |
| GitHub API rate limit exceeded | 未认证 API 配额已耗尽 | 输入 GitHub API Token 后重试 |
| GitHub API 403 | 非速率限制导致的 HTTP 403 | 检查仓库访问权限和 Token 设置 |
| No models found | 指定路径下没有匹配的 `.fmu` 文件 | 检查 FMI 版本、type、platform 和仓库结构 |
| Unsupported FMI type | 下载的 FMU 是 Model Exchange 类型 | 改用 Co-Simulation FMU |
| Network confirmation cancelled | 网络访问未获授权 | 确认来源可信后再重试 |
