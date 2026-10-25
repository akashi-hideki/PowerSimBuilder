# PowerSimBuilder 想定質問と回答（FAQ）

**作成日**: 2026-10-25
**用途**: 利用者・技術者からの問い合わせ対応
**対象**: GitHub Issues、メール

---

## 1. 製品概要

### Q1-1: PowerSimBuilder とは何ですか？

**A**: PowerSimBuilder は、**StaTable のシミュレーション・コンパニオン**です。
StaTable で設計した状態機械に対して、物理モデル（FMU）を組み合わせ、
PC 上で動作シミュレーションを行うためのデスクトップツールです。

- **本体**: .NET 10 WinForms（Windows 専用）
- **FMU 実行**: Python ランタイム（`fmpy`）を内部で呼び出し
- **設計ツール**: StaTable（別プロジェクト）

### Q1-2: StaTable との関係は？

**A**: StaTable が「状態機械を設計し C コードを生成する」ツールであるのに対し、
PowerSimBuilder は「設計した状態機械を、物理モデルと一緒に PC 上で動かして
検証する」ツールです。**補完関係**にあります。

| 項目 | StaTable | PowerSimBuilder |
|------|----------|-----------------|
| 役割 | 設計・コード生成 | シミュレーション |
| 出力 | C99 コード | シミュレーション結果 |
| 実行環境 | MCU / Eclipse | Windows PC |
| 言語 | Python (GUI: PySide6) | C# (.NET 10) |

### Q1-3: 主な機能は？

**A**:
- **物理モデルの取り込み**: FMU（Functional Mock-up Unit）ファイルを読み込み
- **モデルカタログ**: FMI Cross Check などのカタログから 1 クリックで取得
- **ネットワークアクセス制御**: 外部 URL 取得前に明示同意ダイアログを表示
- **シミュレーション**: `fmpy` による FMU 実行（**開発中**）
- **多言語 UI**: 英語 / 簡体中文（日本語は明示的スコープ外）

### Q1-4: 対応言語は？

**A**: 英語（en）と簡体中文（zh）の 2 言語です。
日本語は明示的スコープ外です（`DetectOsLanguage` が `ja` を返さない設計）。

---

## 2. インストール・環境

### Q2-1: 対応 OS は？

**A**: **Windows 10/11 のみ**です。
.NET 10 WinForms を使用しているため、Linux / macOS では動作しません。

### Q2-2: 必要なランタイムは？

**A**:
- **.NET 10 Runtime**（Windows）
- **Python 3.10 〜 3.13**（`fmpy` 実行用）
- **`fmpy`** Python パッケージ

### Q2-3: インストール方法は？

**A**: （**準備中**）
現在はソースからのビルドのみです。ポータブル版の配布は今後のリリースで予定しています。

```powershell
git clone https://github.com/akashi-hideki/PowerSimBuilder.git
cd PowerSimBuilder
dotnet build PowerSimBuilder.csproj -c Debug
```

---

## 3. 技術仕様

### Q3-1: 対応 FMI バージョンは？

**A**: 内部的に `fmpy` を使用しているため、**FMI 1.0 / 2.0 / 3.0** に対応予定です。
（**実機検証は未完了** — シミュレーション機能は現在開発中）

### Q3-2: 対応 FMU の種類は？

**A**: `fmpy` がサポートする **Co-Simulation (CS)** および
**Model Exchange (ME)** の両方を扱う予定です（**開発中**）。

### Q3-3: モデルカタログとは？

**A**: 外部の FMU カタログ（JSON 形式）から、モデル一覧を取得する機能です。

対応ソース:
- **FMI Cross Check**（`modelica/fmi-cross-check`） — 226 モデル
- **GitHub Releases**（`modelica/Reference-FMUs`） — 0 モデル（zip のみ配布のため）
- **任意の GitHub リポジトリ** — `.fmu` を列挙

### Q3-4: ネットワークアクセスの制御は？

**A**: すべての外部 URL 取得前に **`NetworkAccessDialog`** を表示し、
ユーザーの明示同意を得ます。「常に許可」を選択した URL のみ
`AppSettings.AllowedUrls` に保存されます（個別削除可能）。

### Q3-5: テスト状況は？

**A**:
- **L2（C# ユニットテスト）**: 496 passed
- **L3（FlaUI UI テスト）**: 5 passed
- **Python ツールテスト**: 42 passed
- **シミュレーション実機テスト**: **未実施**（今後の課題）

---

## 4. ライセンス・価格

### Q4-1: ライセンスは？

**A**: **Apache License 2.0** です。
商用利用、改変、配布、再許諾が可能です。

### Q4-2: 価格は？

**A**: 現時点では無償公開予定です。

### Q4-3: 商用利用は可能？

**A**: はい、可能です（Apache-2.0 のため）。

---

## 5. 開発状況

### Q5-1: 現在のバージョンは？

**A**: **v1.6.0**（2026-10-25 リリース）です。

主な機能:
- Catalog タブ（FMI Cross Check から 226 モデル取得）
- Sources ドロップダウン（カタログソース選択）
- NetworkAccessDialog（明示同意）
- 多言語 UI（en / zh）

### Q5-2: シミュレーション機能は使えますか？

**A**: **開発中**です。
物理モデルの取り込み（FMU 読み込み・パース）までは動作しますが、
実際のシミュレーション実行は今後のリリースで提供予定です。

### Q5-3: ロードマップは？

**A**:
| フェーズ | 内容 | 状態 |
|---------|------|------|
| MVP-1.2 | 物理モデル挿入（FMU） | ✅ |
| MVP-β | モデルダウンロード補助 | ✅ |
| MVP-γ | Catalog タブ + カタログ取得 | ✅ v1.6.0 |
| **MVP-2** | **シミュレーション実行** | **開発中** |
| MVP-3 | Modelica `.mo` ソース取り込み | 予定 |
| MVP-4 | サブプロセス分離 | 予定 |

---

## 6. 既知の制限

### Q6-1: 既知の制限は？

**A**:
- **シミュレーション未実装** — FMU の読み込み・パースまで
- **Windows 専用** — Linux / macOS 非対応
- **日本語非対応** — en / zh のみ
- **Reference-FMUs は 0 models** — zip のみ配布のため（正常動作）
- **private リポジトリ** — カタログの公開配布は public 化後に再開予定

### Q6-2: Reference-FMUs が 0 models になるのはなぜ？

**A**: `modelica/Reference-FMUs` は GitHub Releases に **zip のみ**を
配布しており、個別の `.fmu` ファイルを置いていません。
PowerSimBuilder の Catalog タブは `.fmu` 単体アセットのみを列挙するため、
0 models になります。これは**正常動作**です。

代替: `FMI Cross Check (Latest)` ソース（226 モデル）をご利用ください。

---

## 7. 技術サポート

### Q7-1: バグ報告は？

**A**: GitHub Issues にて受け付けています。
https://github.com/akashi-hideki/PowerSimBuilder/issues

### Q7-2: 機能リクエストは？

**A**: 同上。GitHub Issues でお願いします。

---

## 8. 連絡先

- **GitHub**: https://github.com/akashi-hideki/PowerSimBuilder
- **Email**: akashi.hideki@gmail.com

---

*本 FAQ は、PowerSimBuilder の利用者・技術者からの問い合わせ対応のために作成されました。*
