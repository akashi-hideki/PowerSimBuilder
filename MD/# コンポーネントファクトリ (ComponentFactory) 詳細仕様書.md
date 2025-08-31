# コンポーネントファクトリ (ComponentFactory) 詳細仕様書

## 1. 基本情報

- **クラス名**: `ComponentFactory`
- **タイプ**: 静的ファクトリクラス
- **責任**: コンポーネントの作成と設定管理

## 2. 主要機能

### 2.1 コンポーネント作成機能
```python
@staticmethod
def create_component(comp_type: str, comp_id: str) -> Component:
```
指定されたタイプとIDに基づいてコンポーネントインスタンスを作成します。

**対応コンポーネントタイプ**:
- `"LCFilter"` - LCフィルタ回路
- `"DiodeBridge"` - 3相ダイオードブリッジ整流回路
- `"IGBTRectifier"` - 3ゲートIGBT制御整流回路
- `"PWMChopper"` - PWMチョッパ制御回路

### 2.2 設定ファイル読み込み機能
```python
@staticmethod
def load_configuration(xml_file: str) -> Dict[str, Component]:
```
XML設定ファイルからコンポーネント設定を読み込み、コンポーネントの辞書を返します。

## 3. XML設定ファイル仕様

### 3.1 基本構造
```xml
<?xml version="1.0" encoding="UTF-8"?>
<SimulationConfig>
    <Components>
        <!-- コンポーネント定義 -->
    </Components>
    <Connections>
        <!-- 接続定義 -->
    </Connections>
</SimulationConfig>
```

### 3.2 コンポーネント定義例
```xml
<LCFilter id="filter1">
    <Parameters>
        <inductance>0.01</inductance>
        <capacitance>0.001</capacitance>
        <esr_inductor>0.1</esr_inductor>
        <esr_capacitor>0.01</esr_capacitor>
        <filter_type>low_pass</filter_type>
    </Parameters>
    <Connections>
        <input_voltage source="rectifier1" output="dc_voltage"/>
        <load_impedance value="10.0"/>
    </Connections>
</LCFilter>
```

### 3.3 接続定義例
```xml
<Connections>
    <Connection from="generator.phase_voltages" to="rectifier1.ac_voltage"/>
    <Connection from="rectifier1.dc_voltage" to="filter1.input_voltage"/>
    <Connection from="filter1.output_voltage" to="load.voltage"/>
</Connections>
```

## 4. パラメータ管理

コンポーネントファクトリは以下のパラメータを管理します：

| パラメータタイプ | 処理方法 | 例 |
|-----------------|----------|-----|
| 数値パラメータ | `float()` で変換 | `<inductance>0.01</inductance>` |
| 文字列パラメータ | そのまま代入 | `<filter_type>low_pass</filter_type>` |
| 接続情報 | 接続マップとして保存 | `<input_voltage source="rectifier1" output="dc_voltage"/>` |

## 5. エラーハンドリング

- 未知のコンポーネントタイプが指定された場合: `ValueError` を発生
- XMLファイルの形式が不正な場合: `ET.ParseError` を捕捉し適切なエラーメッセージを表示
- 必須パラメータが欠けている場合: デフォルト値を使用して処理を継続

## 6. 使用例

### 6.1 基本的な使用方法
```python
# 単一コンポーネントの作成
filter_component = ComponentFactory.create_component("LCFilter", "filter1")

# XML設定からの一括読み込み
components = ComponentFactory.load_configuration("simulation_config.xml")
```

### 6.2 シミュレーション環境での統合
```python
# コンポーネントの作成と設定
components = ComponentFactory.load_configuration("power_system_config.xml")

# シミュレーションループ
for t in np.arange(0, simulation_time, time_step):
    for comp_id, component in components.items():
        component.update(t, time_step, simulation_data)
```

## 7. 拡張性

### 7.1 新しいコンポーネントの追加
新しいコンポーネントを追加するには：
1. 新しいコンポーネントクラスを実装
2. `ComponentFactory.create_component()` メソッドに新しいタイプを追加
3. XMLスキーマを拡張（必要な場合）

### 7.2 カスタムパラメータ処理
特定のコンポーネントに固有のパラメータ処理が必要な場合、各コンポーネントクラス内でパラメータ検証と処理を実装します。

## 8. パフォーマンス考慮事項

- コンポーネント作成は起動時のみ発生
- パラメータ解析はXML読み込み時のみ実行
- 実行時のオーバーヘッドは最小限

## 9. 関連コンポーネントとの連携

コンポーネントファクトリは以下のコンポーネントと連携します：

| コンポーネント | 役割 | 連携方法 |
|---------------|------|----------|
| LCFilter | LCフィルタ回路 | パラメータ設定、接続管理 |
| DiodeBridge | 整流回路 | パラメータ設定、接続管理 |
| IGBTRectifier | IGBT制御整流器 | パラメータ設定、接続管理 |
| PWMChopper | PWMチョッパ制御 | パラメータ設定、接続管理 |

このコンポーネントファクトリは、電力系統シミュレーションシステムにおいて、コンポーネントの作成と設定管理を効率化するための中心的な役割を果たします。