using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace PowerSimBuilder
{
    /// <summary>
    /// 接続情報を表すクラス（座標情報を含まない）
    /// 作成日: 2025-08-12
    /// 注意点: 接続の論理情報のみ保持、視覚情報はConnectionVisualで管理
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// 接続開始コンポーネント
        /// 注意点: nullチェック必須
        /// </summary>
        public ComponentControl StartComponent { get; set; }

        /// <summary>
        /// 接続開始ポート名
        /// 注意点: 空文字列禁止
        /// </summary>
        public string StartPort { get; set; }

        /// <summary>
        /// 接続終了コンポーネント
        /// 注意点: nullチェック必須
        /// </summary>
        public ComponentControl EndComponent { get; set; }

        /// <summary>
        /// 接続終了ポート名
        /// 注意点: 空文字列禁止
        /// </summary>
        public string EndPort { get; set; }

        /// <summary>
        /// 接続のデータタイプ
        /// 注意点: デフォルトは"double"
        /// </summary>
        public string DataType { get; set; } = "double";

        /// <summary>
        /// 接続の視覚的表現
        /// 注意点: 座標情報はここで管理
        /// </summary>
        public ConnectionVisual Visual { get; set; } = new ConnectionVisual();

        /// <summary>
        /// 選択状態フラグ
        /// 注意点: UI表示制御に使用
        /// </summary>
        public bool IsSelected { get; set; }
    }

    /// <summary>
    /// 接続の視覚的表現を管理するクラス
    /// 作成日: 2025-08-12
    /// 注意点: プロパティグリッド表示制御あり
    /// </summary>
    public class ConnectionVisual
    {
        /// <summary>
        /// 接続経路のポイントリスト
        /// 注意点: 少なくとも2点必要
        /// </summary>
        public List<Point> PathPoints { get; set; } = new List<Point>();

        /// <summary>
        /// 接続が選択されているかどうか
        /// 注意点: プロパティグリッド非表示
        /// </summary>
        [Browsable(false)]
        public bool IsSelected { get; set; }
        public bool IsHovered { get; set; }
        public bool IsConnected { get; set; } = true;
    }
}