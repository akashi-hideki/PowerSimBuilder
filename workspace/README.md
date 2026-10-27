# workspace

PowerSimBuilder の作業ディレクトリ。プロジェクト単位で整理されます。

## 構造

```
workspace/
├── projects/                  # プロジェクト保存先
│   └── <name>/
│       ├── project.psbproj    # ZIP（保存・配布用）
│       ├── project.json       # 展開済み（開発用）
│       ├── components/*.py    # 展開済み
│       └── generated/demo_main.py
└── components/                # 共有コンポーネント（将来実装）
```

## 使い方

1. **File → Save** で `workspace/projects/<name>/` に保存
2. 同ディレクトリに ZIP が自動展開される（開発用）
3. **File → Open** で `project.psbproj` を選択してロード

## Git 管理

- `workspace/projects/*` は `.gitignore` 対象（ユーザーデータ）
- 各 `<name>/project.psbproj` は配布可能な自己完結 ZIP
