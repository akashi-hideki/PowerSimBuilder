# FMUカタログとインポートガイド

本書では、Physics Model Import画面の **Catalog / URL / File** タブの操作を説明します。

## 1. カタログの入力形式

| 入力形式 | 用途 |
|---|---|
| `github:<owner>/<repo>|<version>|<cs-or-me>|<platform>` | FMI Cross Check形式のGitHubリポジトリをAPIで検索 |
| GitHubリポジトリURL | GitHub Releasesに公開されたFMUアセットを一覧表示 |
| JSONカタログURL | Physics Catalog Registry経由でJSONカタログを読み込み |

プリセットは `Resources/catalog_sources.json` で定義されています。アプリケーションのビルドによって一覧が異なる場合があります。

## 2. GitHubリポジトリ指定形式

```text
github:<owner>/<repo>|<FMI version>|<type>|<platform>
```

例：

```text
github:modelica/fmi-cross-check|2.0|cs|win64
```

検索対象のディレクトリ構成：

```text
fmus/<FMI version>/<type>/<platform>/<Tool>/<ToolVersion>/<Model>/<Model>.fmu
```

- `<owner>/<repo>`：GitHubリポジトリ
- `<FMI version>`：FMIバージョン（例：`2.0`）
- `<type>`：`cs` はCo-Simulation、`me` はModel Exchange
- `<platform>`：リポジトリ内のプラットフォームディレクトリ（例：`win64`）

クライアントはリポジトリのデフォルトブランチを取得し、対象プラットフォームの再帰Git Treeを読み込みます。GitHubがTreeをtruncated（不完全）と返した場合は、不完全な一覧を表示せずエラーにします。

## 3. GitHub APIトークン

トークンの入力は任意です。`github:` 形式のソースでは、GitHubリポジトリAPIリクエスト（デフォルトブランチの取得、プラットフォームディレクトリの特定、再帰Treeの取得）のAuthorizationヘッダーに設定され、未認証時のAPIレート制限を回避するために利用できます。GitHub Releases APIには使用されません。

- Catalogタブ下部の **GitHub API Key** 欄に入力します。
- 隣接する表示／非表示コントロールで入力値を確認できます。
- `github:` 形式のソースで **Load** を実行すると、`AppSettings` 経由でアプリケーション設定に保存されます。入力しただけでは保存されません。
- トークンは暗号化されず、アプリケーション設定ファイルに平文で保存されます。
- トークンをスクリーンショット、問い合わせ資料、ソースコード、共有ログに含めないでください。
- 閲覧対象に必要な最小限の権限を持つトークンを使用してください。公開リポジトリの参照に書き込み権限は不要です。

レート制限超過と、それ以外のHTTP 403エラーは区別して通知されます。

## 4. カタログからインポートする手順

1. **Import Physics Model** を開きます。
2. **Catalog** タブを選択します。
3. プリセットを選ぶか、カタログソースを入力します。
4. GitHubリポジトリツリーを使う場合は `github:` 形式を入力します。必要に応じてAPIトークンも入力します。
5. **Load** をクリックして一覧を読み込みます。
6. FMUを選択し、**Download** をクリックします（行のダブルクリックでも実行できます）。
7. 選択したFMUのURLが **URL** タブに渡され、ダウンロード処理が始まります。
8. ネットワークアクセス確認ダイアログで許可します。許可されない場合、ダウンロードは行われません。**Always allow**（常時許可）を選択した場合、URLはアプリケーション設定（`AppSettings.AllowedUrls`）に保存されます。ただし、保存済みURLを参照して確認を省略する処理はなく、次回のダウンロードでも確認ダイアログが表示されます。
9. ダウンロード後、FMUが解析され、**File** タブに切り替わります。
10. 入出力変数を選択し、モデル名を設定して **Import** をクリックします。

**注意：** カタログ項目を選択しただけではインポートは完了しません。ネットワーク許可、ダウンロード、FMU解析、ポート選択、Import操作がそれぞれ必要です。ポートが自動的にすべて選択されるわけではありません。

## 5. 現在の制限事項

- FMUアーカイブ内に `modelDescription.xml` が必要です。
- 現在のインポート画面が受け付けるのは **Co-Simulation FMUのみ** です。
- Model Exchange (`me`) はカタログで一覧表示できますが、インポート時に拒否されます。
- ポート選択に表示されるのは causality が `input` または `output` の変数のみです。
- `.mo` のModelicaソースファイルは直接インポートできません。先にFMUを生成してください。
- GitHub Releases形式のソースは最新Releaseのみを対象とし、名前が `.fmu` で終わるアセットだけを一覧表示します（拡張子の大文字・小文字は区別しません）。ZIPなどのアセットは表示されません。
- ZIPアセットの自動展開、およびZIP内部のFMU検索には対応していません。
- GitHub APIトークンはGitHub Releases APIには使用されません。
- ZIPアセットしか公開していないリポジトリは、GitHubツリー検索の対象にはなりません。

## 6. トラブルシューティング

| 症状 | 主な原因 | 対処 |
|---|---|---|
| `Invalid source` | `github:` の区切り項目が4つではない | `owner/repo|version|cs-or-me|platform` の形式を確認 |
| `Path not found` | 指定したFMI／type／platformのパスが存在しない | リポジトリ構成と指定値を確認 |
| GitHub API rate limit exceeded | 未認証APIの利用上限に到達 | APIトークンを入力して再試行 |
| GitHub API 403 | レート制限超過以外のHTTP 403 | リポジトリアクセス権とトークン設定を確認 |
| No models found | 対象パスに一致するFMUがない | FMIバージョン、type、platform、ディレクトリ構成を確認 |
| Unsupported FMI type | ダウンロードしたFMUがModel Exchange | Co-Simulation FMUを使用 |
| Network confirmation cancelled | ネットワークアクセスが許可されなかった | 信頼できるソースであることを確認してから再試行 |
