using System.Collections.Generic;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネント定義データクラス
    /// 作成日: 2025-08-15
    /// 注意点: XMLシリアライズ可能なコンポーネント定義構造
    /// </summary>
    public class ComponentDefinition
    {
        /// <summary>
        /// コンポーネント表示名
        /// 作成日: 2025-08-15
        /// 注意点: ユーザーインターフェイスで表示される名前
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// コンポーネントタイプ識別子
        /// 作成日: 2025-08-15
        /// 注意点: プログラム内で一意である必要あり
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// カテゴリ分類
        /// 作成日: 2025-08-15
        /// 注意点: パレットでのグループ分けに使用
        /// </summary>
        public string Category { get; set; }

        /// <summary>
        /// アイコン表示色
        /// 作成日: 2025-08-15
        /// 注意点: 色名またはHEXコードで指定
        /// </summary>
        public string IconColor { get; set; }

        /// <summary>
        /// パラメータ定義リスト
        /// 作成日: 2025-08-15
        /// 注意点: コンポーネントの設定可能なパラメータ
        /// </summary>
        public List<ParameterDefinition> Parameters { get; set; } = new List<ParameterDefinition>();

        /// <summary>
        /// 入力ポート名リスト
        /// 作成日: 2025-08-15
        /// 注意点: 空のリストは入力ポートなしを示す
        /// </summary>
        public List<string> InputPorts { get; set; } = new List<string>();

        /// <summary>
        /// 出力ポート名リスト
        /// 作成日: 2025-08-15
        /// 注意点: 空のリストは出力ポートなしを示す
        /// </summary>
        public List<string> OutputPorts { get; set; } = new List<string>();
    }

    /// <summary>
    /// パラメータ定義データクラス
    /// 作成日: 2025-08-15
    /// 注意点: XMLシリアライズ可能なパラメータ定義構造
    /// </summary>
    public class ParameterDefinition
    {
        /// <summary>
        /// パラメータ名
        /// 作成日: 2025-08-15
        /// 注意点: プロパティグリッドで表示される名前
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// データ型指定
        /// 作成日: 2025-08-15
        /// 注意点: 'double', 'int', 'string', 'bool' のいずれか
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// デフォルト値
        /// 作成日: 2025-08-15
        /// 注意点: 文字列形式で保存、使用時に適切な型に変換
        /// </summary>
        public string DefaultValue { get; set; }

        /// <summary>
        /// 単位表示
        /// 作成日: 2025-08-15
        /// 注意点: プロパティグリッドで表示される単位
        /// </summary>
        public string Unit { get; set; }
    }
}