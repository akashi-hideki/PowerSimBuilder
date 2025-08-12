using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;

namespace PowerSimBuilder
{
    /// <summary>
    /// 接続情報を表すクラス（座標情報を含まない）
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// 接続開始コンポーネント
        /// </summary>
        public ComponentControl StartComponent { get; set; }

        /// <summary>
        /// 接続開始ポート名
        /// </summary>
        public string StartPort { get; set; }

        /// <summary>
        /// 接続終了コンポーネント
        /// </summary>
        public ComponentControl EndComponent { get; set; }

        /// <summary>
        /// 接続終了ポート名
        /// </summary>
        public string EndPort { get; set; }

        /// <summary>
        /// 接続のデータタイプ
        /// </summary>
        public string DataType { get; set; } = "double";

        /// <summary>
        /// 接続の視覚的表現
        /// </summary>
        public ConnectionVisual Visual { get; set; } = new ConnectionVisual();
        public bool IsSelected { get; set; }  

    }

    /// <summary>
    /// 接続の視覚的表現を管理するクラス
    /// </summary>
    public class ConnectionVisual
    {
        /// <summary>
        /// 接続経路のポイントリスト
        /// </summary>
        public List<Point> PathPoints { get; set; } = new List<Point>();

        /// <summary>
        /// 接続が選択されているかどうか
        /// </summary>

        [Browsable(false)] // プロパティグリッドに表示しない
        public bool IsSelected { get; set; } // 直接プロパティを定義
    }
}