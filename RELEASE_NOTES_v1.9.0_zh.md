## 亮点

### 角色片段 — 注册

将光标处的角色函数提取为可复用片段，然后插入到画布上的任何其他目标函数中。

- **右键单击**函数内部 -> `注册为片段...`
- **工具栏** `注册片段`（仅当文件包含 `STABLE_USER_CODE_START` 标记时可见 — 与组件类型无关）
- **插入片段**，支持 `(片段自身)` / 跨函数模式
- 新增 12 个 i18n 键（英/中，共 374 个）

### 端到端自动化

| 测试 | 结果 |
|---|---|
| `RoleSnippetRegistrarTests` | 8（提取 / 注册 / 验证） |
| `RoleSnippetStorageTests` | 6（index.json + .py 文件 I/O） |
| `RoleSnippetVisibilityTests` | 15（L1 标记检查 + L2 表单可见性） |
| `RoleSnippetRegisterE2ETests` | 1（FlaUI E2E，注册对话框 -> 文件创建） |

`verify_all.ps1` 现在在每次验证时运行 `[4c]` RoleSnippet 单元测试和 `[4d]` Register FlaUI E2E。

### 工具加固

- **Trap #80 缓解**：`gcommit.ps1` 在 git 操作前停止任何正在运行的 `PowerSimBuilder.exe`（防止文件锁定）。
- **Trap #85 缓解**：`verify_all.ps1` `[4/6]` E2E 在失败时休眠 3 秒并重试一次 — 在本次发布中已实际观察到。

## 修复

- `SetPythonCode` / `LoadFile` 现在自动刷新 Register Snippet 可见性（B-3c）。编程代码更改后不再隐藏按钮。
- `menuItemRoleSnippets` 跟随标记存在（之前始终可见）。
- **Trap #84**：内联正则表达式（双反斜杠）破坏了标记检测 — 提取到 `RoleSnippetRegistrar.ContainsRoleMarkers`。
- **Trap #86**：`ShowDialog` + `ShowInTaskbar=false` 模态对话框不在 UIA3 桌面树中 — 添加 Win32 `EnumWindows` 回退。
- **Trap #88**：`PSB_DUMP_CSV` 支持内置到生成的 `demo_main.py` 中（无需手动补丁）。
- **Trap #89**：`PSB_DUMP_CSV_HZ` / `PSB_DUMP_CSV_COLUMNS` — 写入时过滤（145MB -> 53KB，99.96% 减少）。
- **Trap #90**：`*>&1` 捕获 `Write-Host` 输出（verify_all.log 从 491B -> 3.9KB）。
- **Trap #92**：`qa_snapshot.ps1` 预检查和 `demo_main.py` 保留/恢复（避免 0MB CSV 从陈旧文件生成）。

## 测试

| 套件 | 结果 |
|---|---|
| C# 单元测试 | 599（597 通过 / 2 跳过 / 0 失败） |
| UI E2E | 16（15 通过 / 1 跳过 / 0 失败） |
| 角色片段测试 | 30 |
| Python | 460 通过 |
| CI | 全绿（Python 3.10/3.11/3.12/3.13 + Windows .NET 10） |

## 变更日志

见 [CHANGELOG.md](CHANGELOG.md#190---2026-11-07)。

## 从 v1.8.0 升级

无破坏性更改。`pip install -r requirements.txt` 更新依赖。

## 安装
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder
dotnet build -c Debug
python -m pip install -e .

text

## 链接

- GitHub Release: https://github.com/akashi-hideki/PowerSimBuilder/releases/tag/v1.9.0
- 问题反馈: https://github.com/akashi-hideki/PowerSimBuilder/issues
