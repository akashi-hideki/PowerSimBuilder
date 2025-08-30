# VC# ネットリスト変換クラス設計書（Markdown版）

## 1. 目的

Pythonで作成したシミュレーションの **コンポーネント情報（parameters、inputs、outputs）** と **接続情報（connections）** を読み込み、
SPICE風ネットリスト文字列に変換し、ファイル出力するクラスを設計する。

---

## 2. クラス構成

### 2.1 `NetlistConverter`

**概要**: PythonのJSON/辞書形式データを読み込み、SPICEネットリスト文字列を生成するメインクラス。

#### プロパティ

| プロパティ名          | 型                    | 説明             |
| --------------- | -------------------- | -------------- |
| `Components`    | List<ComponentData>  | すべての部品情報       |
| `Connections`   | List<ConnectionData> | 出力→入力接続情報      |
| `NetlistString` | string               | 生成されたネットリスト文字列 |

#### メソッド

| メソッド名                           | 引数         | 戻り値           | 説明                                              |
| ------------------------------- | ---------- | ------------- | ----------------------------------------------- |
| `LoadFromJson(string jsonPath)` | JSONファイルパス | void          | Python側で作成した辞書形式ファイルを読み込む                       |
| `ConvertToNetlist()`            | なし         | string        | 内部データからSPICE風ネットリスト文字列を生成し `NetlistString` にセット |
| `SaveNetlist(string filePath)`  | ファイルパス     | void          | `NetlistString` を指定パスに保存                        |
| `GetComponentById(string id)`   | コンポーネントID  | ComponentData | 指定IDの部品情報を返す                                    |

---

### 2.2 `ComponentData`

**概要**: 各コンポーネントを表すデータクラス

#### プロパティ

| プロパティ名       | 型                           | 説明                                          |
| ------------ | --------------------------- | ------------------------------------------- |
| `Id`         | string                      | コンポーネント識別ID                                 |
| `Type`       | string                      | コンポーネント種別（PowerSource, Welder, Generatorなど） |
| `Parameters` | Dictionary\<string, double> | 初期パラメータ                                     |
| `Inputs`     | List<string>                | 入力ポート名                                      |
| `Outputs`    | List<string>                | 出力ポート名                                      |

#### メソッド

* 基本的には **データ保持用**。変換時は `NetlistConverter` で参照。

---

### 2.3 `ConnectionData`

**概要**: 出力→入力の接続情報を保持

#### プロパティ

| プロパティ名          | 型      | 説明           |
| --------------- | ------ | ------------ |
| `FromComponent` | string | 出力元コンポーネントID |
| `FromPort`      | string | 出力ポート名       |
| `ToComponent`   | string | 入力先コンポーネントID |
| `ToPort`        | string | 入力ポート名       |

---

## 3. ネットリスト生成ルール

1. 各コンポーネントは以下の形式で出力

```
<COMP_ID> <TYPE> <PARAM1>=<VALUE1> <PARAM2>=<VALUE2> ...
```

2. 接続は以下の形式で出力

```
* CONNECTIONS
<TO_COMPONENT>.<TO_PORT> <- <FROM_COMPONENT>.<FROM_PORT>
```

3. ファイル例（簡易）

```
* COMPONENTS
engine Engine power=10
generator Generator rated_voltage=100
bridge DiodeBridge diode_drop=0.7
welder Welder welding_current_set=100

* CONNECTIONS
generator.torque <- engine.torque
bridge.ac_voltage <- generator.voltage
welder.dc_voltage <- bridge.dc_voltage
```

---

## 4. 処理フロー

1. `LoadFromJson` で JSON/辞書形式を読み込み、`Components` と `Connections` に格納
2. `ConvertToNetlist` で

   * 各コンポーネントを文字列化
   * 接続情報を文字列化
   * コメントとして各部品・接続の凡例を付加
3. `SaveNetlist` でファイルに出力
4. 必要に応じて `GetComponentById` で部品情報を参照可能

---

## 5. 拡張ポイント

* コンポーネント種別に応じたSPICEモデルテンプレートの適用
* 複雑なパラメータ（例: 故障シナリオ、負荷ON/OFF）の反映
* SPICEシミュレーション用のコントロール命令追加（.tran, .dcなど）

---

これで **VC# ネットリスト変換クラス設計書** が完成です。
Python側の設計と接続情報をそのまま活用できる形になっています。
