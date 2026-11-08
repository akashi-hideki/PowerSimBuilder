# PowerSimBuilder Log Format Specification — `psblog/v1`

| 項目 | 値 |
|---|---|
| Document owner | PowerSimBuilder |
| Target implementer | **StaTable** (UART monitor writer) |
| Status | **v1 confirmed** (StaTable approved 2026-11-08) |
| Version | v1 (2026-11-08) |
| Companion | `THREAD_HANDOFF.md` / `docs/SPEC_ROLE_SNIPPETS.md` |

---

## 1. Purpose

StaTable が **UART モニタ** で出力するログファイルのフォーマットを定義する。
PowerSimBuilder はこのファイルを読み込み、以下を行う:

> **監視対象変数はログごとに異なる**。ユーザーが記録開始時に選択した
> `g_xxx` のみが VarTable に含まれる。同一ファームウェアでも、
> 目的（PID チューニング / 温度監視 / ハードウェア診断）によって変数
> セットは変化する。PSB は VarTable をそのまま信頼し、宣言済み変数の
> みを扱う。

1. **信号再生**: 記録された時系列を `RecordedSignal` コンポーネントとして Canvas に配置
2. **コンポーネント推論**: 波形形状から `ThermalMass` / `FirstOrderLag` / `PIDController` 等の候補を提示
3. **定量検証**: 実機波形とシミュレーション結果の比較

> **重要**: UART フレーミング / CRC / コマンド ID / 変数 ID の割当は **StaTable の所有物** であり、本仕様書の範囲外。本仕様は **ログファイル（テキスト）のみ** を定める。

---

## 2. Scope / Non-goals

### In scope (本仕様書が定めるもの)

- ファイルエンコーディング・改行
- セクション構造（Header / VarTable / Data / Footer）
- 変数メタデータの文法
- データ行の文法
- 型マッピング（StaTable XML → psblog）
- 破損行への耐性
- バージョニング

### Out of scope (StaTable 側の自由)

- UART フレーム（SOF/LEN/CRC/EOF）
- コマンド体系（R/W/Start/Stop 等）
- 変数 ID の割当（`g_xxx` ごとのオフセット等）
- ポーリング / 自発ストリーム / バッチ
- 再接続 / リトライ / タイムアウト
- バイナリモード（本仕様は **ASCII テキストログのみ**）

---

## 3. Design goals

| Goal | Rationale |
|---|---|
| **Human readable** | `tail` / `grep` で即確認できる |
| **Streaming append** | 後処理なしで書ける |
| **Self-describing** | 変数テーブルと型を内包する |
| **Fault tolerant** | 1 行破損で全体が死なない |
| **Order independent** | 変数の到着順・混在順を許容する |
| **Compact** | 10 Hz × 30 変数 × 300 s < 5 MB |

---

## 4. File format

| 項目 | 値 |
|---|---|
| Extension | `.psblog` |
| Encoding | **UTF-8, BOM なし** |
| Line ending | **`LF` (`\n`) のみ** |
| BOM | **付与禁止** |

### 4.1 セクション順序

```
[Header]        必須、1 回、ファイル先頭
[VarTable]      必須、1 回、Header の直後
[Data]          0 行以上
[Footer]        任意、1 回、ファイル末尾
```

### 4.2 コメント行

行頭（空白後）が `#` で始まる行はコメント。StaTable は自由に出力してよい。

---

### 4.3 Log rotation（10 MB 既定, v1 確定）

StaTable SDK は `LogOptions` の既定値として **10 MB** のサイズローテーションを行う。

| ファイル | 意味 |
|---|---|
| `<base>.psblog` | 最新 |
| `<base>.1.psblog` | 1 世代前 |
| `<base>.2.psblog` | 2 世代前 |

- PSB は `<base>*.psblog` の glob で連番を自動収集
- 各ファイルの `@captured_at` で時系列順に結合
- ローテーション境界でのサンプル欠落は許容

## 5. Header セクション

`@` で始まるメタ行。**ファイル先頭、VarTable より前** に置く。

```
@schema       psblog/v1
@captured_at  2026-11-09T10:30:00.000+09:00
@source       uart:COM3
@firmware     StaTable v1.9.0
@protocol     statable-uart/v1
@baud         115200
@period_ms    10
```

| キー | 必須 | 意味 | 例 |
|---|:---:|---|---|
| `@schema` | ✅ | 本仕様のバージョン | `psblog/v1` |
| `@captured_at` | ✅ | 記録開始時刻（ISO 8601 with TZ） | `2026-11-09T10:30:00+09:00` |
| `@source` | ✅ | 入力元識別子 | `uart:COM3`, `usb:/dev/ttyACM0` |
| `@firmware` | 推奨 | StaTable バージョン | `StaTable v1.9.0` |
| `@protocol` | 推奨 | wire protocol ID（PSB は不透明文字列として扱う） | `statable-uart/v1` |
| `@baud` | 推奨 | 整数 | `115200` |
| `@period_ms` | 推奨 | 公称サンプル周期（整数 ms） | `10` |

将来拡張キーは `@` + 小文字英数字 で追加してよい（PSB は未知キーを無視）。

---

## 6. VarTable セクション

監視対象の **各 `g_xxx` 変数につき 1 行**。Data より前に置くこと。

> **変数セットはログごとに異なる**。これは仕様の **正常な使い方** であり、
> 固定セットを要求しない。StaTable はユーザーの選択に応じて VarTable を
> 動的に構成する。
>
> 例:
> - **PID チューニング用**: `oven_target_temp` / `oven_current_temp` / `heater_top_duty` / `error_code`
> - **温度監視用**: `thermistor_top` / `thermistor_middle` / `thermistor_bottom` / `oven_current_temp`
> - **ハードウェア診断用**: `zc_count` / `ir_value` / `steam_sensor` / `mw_anode_current`
> - **全変数（30 個）**: デバッグ・回帰解析用
>
> PSB 側は **VarTable に宣言された変数のみ** を処理する。宣言にない変数が
> Data 行に現れた場合は警告のみで skip する（§10.1）。

```
@var <name> <type> <unit> <group> [<description>]
```

| フィールド | 内容 |
|---|---|
| `<name>` | C 識別子（`g_system_tick` / `error_code` / `oven_current_temp`） |
| `<type>` | 正規化型（§7） |
| `<unit>` | 物理単位、なしは `-` |
| `<group>` | StaTable の `group` 属性（`Timer` / `System` / `Hardware`） |
| `<description>` | 任意。空白含む場合は `"..."` で囲む |

### 6.1 例（cooking_heater_controller.xml の実変数）

```
@var g_system_tick      uint32_t 1ms     Timer    "System tick"
@var error_code         uint8_t  -       System   "Error code"
@var oven_target_temp   uint16_t degC    Hardware "Target oven temperature"
@var oven_current_temp  uint16_t degC    Hardware "Current oven temperature"
@var thermistor_top     uint16_t degC    Hardware "Top thermistor"
@var thermistor_middle  uint16_t degC    Hardware "Middle thermistor"
@var heater_top_duty    uint8_t  %       Hardware "Top heater PWM duty"
@var heater_middle_duty uint8_t  %       Hardware "Middle heater PWM duty"
@var fan_duty           uint8_t  %       Hardware "Convection fan duty"
@var door_open          uint8_t  -       Hardware "Door open flag"
```
### 6.2 `role` ヒント（任意, v1 拡張）

VarTable の `@var` 行末尾に `role=<role>` を任意で追加可能。

| role | 意味 |
|---|---|
| `input` | 外部入力 / センサー |
| `output` | アクチュエータ出力 |
| `state` | 内部状態 / フラグ |
| `pid_out` | PID 制御出力 |
| `target` | 目標値 / 設定値 |
| `error` | エラーコード / 診断 |
| `-` | 未指定（既定） |

PSB の利用: `role` は波形分類の精度向上に使う。未指定でも分類可能。

---

## 7. Type mapping (StaTable XML → psblog)

| StaTable XML `type` | psblog `<type>` | 値の例 |
|---|---|---|
| `uint8_t` | `uint8_t` | `0` / `255` |
| `uint16_t` | `uint16_t` | `65535` |
| `uint32_t` | `uint32_t` | `4294967295` |
| `volatile uint32_t` | `uint32_t` | 同上 |
| `int8_t` | `int8_t` | `-128` |
| `int16_t` | `int16_t` | `-32768` |
| `int32_t` | `int32_t` | `-2147483648` |
| `float` | `float32` | `1.2345`（`%.6g` 推奨） |
| `double` | `float64` | `1.2345678901`（`%.10g` 推奨） |
| `bool` | `bool` | `0` / `1` |
| その他 | `raw` | 文字列として扱う |

`volatile` / `const` は無視し、基本型に正規化する。

---

## 8. Data セクション

各受信サンプルを **1 行** に書く。1 行に **複数変数** を含めてよい（バッチ応答）。

```
@<t_ms> <name>=<value> [<name>=<value> ...]
```

| フィールド | 内容 |
|---|---|
| `<t_ms>` | `@captured_at` からの経過ミリ秒（整数、0 始まり） |
| `<name>` | VarTable で宣言済みの変数名 |
| `<value>` | 型に応じた表現（§8.1） |

### 8.1 値フォーマット

| 型 | 表現 | 備考 |
|---|---|---|
| 整数型 | 符号付き/なし 10 進 | `0`, `-1`, `65535` |
| `float32` | `%.6g` 推奨 | 桁落ち防止 |
| `float64` | `%.10g` 推奨 | 同上 |
| `bool` | `0` / `1` | `true`/`false` も可（将来） |
| `raw` | そのまま | 空白なし |

`NaN` / `Inf` は書かない（該当サンプルはスキップするか `ERR` コメント行に変換）。

### 8.2 混在ログの例

```
@0       g_system_tick=0 error_code=0 oven_target_temp=200 oven_current_temp=25 door_open=0
@10      g_system_tick=10
@10      heater_top_duty=0
@10      oven_current_temp=25
@20      g_system_tick=20 heater_top_duty=100
@20      oven_current_temp=25
@100     g_system_tick=100 heater_top_duty=85
@100     oven_current_temp=26 thermistor_top=31 thermistor_middle=26
@200     g_system_tick=200 oven_current_temp=27 thermistor_top=32
```

- 同一 `t_ms` に複数行あってよい（変数ごとに受信した場合）
- 1 行に複数 `key=value` があってよい（バッチ受信した場合）
- **到着順をそのまま反映**（時刻順にソートする必要はない）

---

## 9. Footer セクション（任意）

```
@end      2026-11-09T10:35:00.000+09:00
@samples  30000
@dropped  0
```

| キー | 意味 |
|---|---|
| `@end` | 記録終了時刻（ISO 8601 with TZ） |
| `@samples` | 書き込んだサンプル行数（PSB の sanity check 用） |
| `@dropped` | 受信側で欠落を検出した数（0 推奨、非ゼロでも PSB は警告のみ） |

---

### 9.1 `@dropped` の意味（v1 確定）

`@dropped` は **シーケンスギャップ + CRC エラー** の合計数（Q6 回答）。

- StaTable 側は SEQ コマンドで最終シーケンス番号を取得可能
- PSB 側は `@dropped != 0` で警告のみ（処理継続）

## 10. Error handling

| 状況 | StaTable の推奨動作 | PSB 側の対処 |
|---|---|---|
| 1 変数のみ到着 | 1 行に 1 変数で書く | 正常受理 |
| 複数変数が同一サンプル | 1 行にまとめる or 複数行 | どちらも受理 |
| CRC エラーで値破損 | その行を書かない（skip） | 欠落として扱う |
| タイムアウト後の復帰 | `@meta` コメント行追加を推奨 | 無視 |
| 変数が途中から出現 | VarTable に事前宣言済みなら問題なし | 正常受理 |
| 値にスペースが入る | `raw` 型以外では起こらない | — |

### 10.1 破損時の PSB フォールバック

- 未知の `@` 行 → 無視、`warnings += 1`
- 未知の変数名 → その行の該当トークンを skip
- 数値パース失敗 → その行の該当トークンを skip
- 行全体が壊れている → その行のみ skip、継続

**ファイル全体を止めない** ことが StaTable への唯一の要求。

---

## 11. Example (complete)

以下は cooking_heater_controller.xml の変数を使った **完全な例**:

```psblog
# StaTable UART Monitor Log
@schema       psblog/v1
@captured_at  2026-11-09T10:30:00.000+09:00
@source       uart:COM3
@firmware     StaTable v1.9.0
@protocol     statable-uart/v1
@baud         115200
@period_ms    10

@var g_system_tick      uint32_t 1ms     Timer    "System tick"
@var error_code         uint8_t  -       System   "Error code"
@var oven_target_temp   uint16_t degC    Hardware "Target oven temperature"
@var oven_current_temp  uint16_t degC    Hardware "Current oven temperature"
@var thermistor_top     uint16_t degC    Hardware "Top thermistor"
@var thermistor_middle  uint16_t degC    Hardware "Middle thermistor"
@var heater_top_duty    uint8_t  %       Hardware "Top heater PWM duty"
@var fan_duty           uint8_t  %       Hardware "Convection fan duty"
@var door_open          uint8_t  -       Hardware "Door open flag"

@0      g_system_tick=0 error_code=0 oven_target_temp=200 oven_current_temp=25 door_open=0
@10     g_system_tick=10 heater_top_duty=0
@10     oven_current_temp=25
@20     g_system_tick=20 heater_top_duty=100
@20     oven_current_temp=25
@30     g_system_tick=30 oven_current_temp=26 thermistor_top=31
@40     g_system_tick=40 heater_top_duty=85
@40     oven_current_temp=27
@100    g_system_tick=100 oven_current_temp=35 thermistor_top=40
@100    heater_top_duty=45
@200    g_system_tick=200 oven_current_temp=78 thermistor_top=83
@200    heater_top_duty=38
# ... (300 s)

@end      2026-11-09T10:35:00.000+09:00
@samples  30000
@dropped  0
```

---

## 12. Versioning policy

- `psblog/v1` は **ファイル形式の互換性契約**
- 破壊的変更（既存キーの意味変更、行構造の変更）は `psblog/v2` に上げる
- 追加変更（新 `@` キー、新 `<type>`）は v1 のまま拡張可能（PSB は未知キーを無視）
- StaTable は将来 `psblog/v2` を出力できるよう、フォーマットバージョンを設定で選択可能にすることを推奨

---

## 13. Resolved questions (StaTable 承認済み, 2026-11-08)

StaTable 側からの回答を反映し、**v1 仕様として確定** した。

| # | 質問 | StaTable の回答 |
|:---:|---|---|
| Q1 | 変数セットはログ生成時固定でよいか | **固定**。SDK の `logger.Start()` 時に `variables` で確定。ログ途中追加は v1 では非対応 |
| Q2 | ポーリング or 自発ストリーム | **両対応**。ログ用途は STREAM 推奨。ポーリングは `ReadAsync` で個別取得 |
| Q3 | タイムスタンプ | **PC 受信時刻** を採用（`@captured_at` + `t_ms`）。MCU tick は `g_system_tick` 変数で参照可能 |
| Q4 | 同時接続 | **許容**。`StaTableMonitor` を複数生成可能、各インスタンス独立ロガー |
| Q5 | ファイルサイズ上限 | **10 MB ローテーション** を SDK 既定 `LogOptions` に設定、変更可 |
| Q6 | `@dropped` の計算方法 | **シーケンスギャップ + CRC エラー**。`SEQ` コマンドで最終シーケンス確認可 |
| Q7 | 役割ヒント（入力/出力等） | v1 拡張として INFO 末尾に `role=<role>` 追加可能。未指定時 `-`、PSB は未知キー無視 |

### 13.1 回答から導かれる v1 仕様の追記

- **(a) `role` ヒント**: §6.2 で正式化（末尾任意 `role=<role>`）
- **(b) ログローテーション**: §4.3 で正式化（`<base>.N.psblog`、10 MB 既定）
- **(c) `@dropped` の意味**: §9.1 で正式化（seq gap + CRC error）


## 14. PowerSimBuilder 側の受け入れ準備状況

StaTable 仕様確定後、PSB は以下を実装予定:

| Phase | 内容 | 規模 |
|:---:|---|---:|
| 1 | `docs/SPEC_PSBLOG.md` 確定（本ドキュメント） | ✅ 本セッション |
| 2 | `src/python_runtime/uart_monitor.py` (parser) + 19 tests | S |
| 3 | `RecordedSignal` コンポーネント（Canvas 統合） | M |
| 4 | `signal_classifier.py`（波形分類 → 候補提示） | M |
| 5 | `File → Import UART Log` UI | S |

StaTable 側の実装状況と並走できるよう、Phase 2 は **本仕様のみで着手可能** です。

---

## 15. Reference: 監視対象変数一覧（cooking_heater_controller.xml）

VarTable の `<name>` として使用できる変数（30 変数）:

```
g_system_tick      volatile uint32_t  Timer    1ms
error_code         uint8_t            System   -
zc_count           uint32_t           Hardware count
seq_step           uint8_t            Hardware -
seq_total          uint8_t            Hardware -
ir_value           uint16_t           Hardware ADC
steam_sensor       uint16_t           Hardware ADC
thermistor_top     uint16_t           Hardware degC
thermistor_middle  uint16_t           Hardware degC
thermistor_bottom  uint16_t           Hardware degC
humidity           uint8_t            Hardware %
heater_top_duty    uint8_t            Hardware %
heater_middle_duty uint8_t            Hardware %
heater_bottom_duty uint8_t            Hardware %
fan_duty           uint8_t            Hardware %
mw_power           uint16_t           Hardware W
mw_anode_current   uint16_t           Hardware mA
boiler_temp        uint16_t           Hardware degC
pump_pulse_count   uint8_t            Hardware -
mw_target_power    uint16_t           Hardware W
oven_target_temp   uint16_t           Hardware degC
oven_current_temp  uint16_t           Hardware degC
steam_target_temp  uint16_t           Hardware degC
stage_elapsed      uint32_t           Hardware ms
stage_duration     uint32_t           Hardware ms
stage_mode         uint8_t            Hardware -
door_open          uint8_t            Hardware -
rx_buf             uint8_t            Hardware -
heater_top_on      uint8_t            Hardware -
heater_middle_on   uint8_t            Hardware -
heater_bottom_on   uint8_t            Hardware -
fan_on             uint8_t            Hardware -
pump_on            uint8_t            Hardware -
mag_on             uint8_t            Hardware -
```

StaTable 側は、ユーザーが選択した変数のみ VarTable に含めればよい（全 30 変数を出す必要はない）。

---

## 16. Contact / Agreement

| 項目 | 内容 |
|---|---|
| Document owner | PowerSimBuilder (`akashi.hideki@gmail.com`) |
| Review requested from | **StaTable maintainer** |
| Agreed version | _（StaTable 承認後に記入）_ |
| Agreed date | _（同上）_ |

**StaTable 側の承認をもって `psblog/v1` を確定** とする。

---

*This document is the contract between StaTable (writer) and PowerSimBuilder (reader) for `.psblog` log files.*
