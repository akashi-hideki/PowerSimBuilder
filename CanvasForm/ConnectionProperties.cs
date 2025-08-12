using System.ComponentModel;

namespace PowerSimBuilder
{
    /// <summary>
    /// 接続プロパティ表示用クラス
    /// 作成日: 2025-08-12
    /// 注意点: プロパティグリッド表示用ラッパークラス
    /// </summary>
    public class ConnectionProperties
    {
        private readonly Connection _connection;

        /// <summary>
        /// コンストラクタ
        /// 注意点: 接続オブジェクトを内部保持
        /// </summary>
        public ConnectionProperties(Connection connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// 接続表示名
        /// 注意点: ユーザー編集可能
        /// </summary>
        [Category("基本設定")]
        [DisplayName("接続名")]
        [Description("接続の表示名")]
        public string ConnectionName { get; set; } = "接続1";

        /// <summary>
        /// データ型プロパティ
        /// 注意点: 接続オブジェクトと連動
        /// </summary>
        [Category("基本設定")]
        [DisplayName("データ型")]
        [Description("伝送するデータの型")]
        public string DataType
        {
            get => _connection.DataType;
            set => _connection.DataType = value;
        }

        /// <summary>
        /// 開始コンポーネント表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("接続情報")]
        [DisplayName("開始コンポーネント")]
        [ReadOnly(true)]
        public string StartComponent => _connection.StartComponent?.InstanceName;

        /// <summary>
        /// 開始ポート表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("接続情報")]
        [DisplayName("開始ポート")]
        [ReadOnly(true)]
        public string StartPort => _connection.StartPort;

        /// <summary>
        /// 終了コンポーネント表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("接続情報")]
        [DisplayName("終了コンポーネント")]
        [ReadOnly(true)]
        public string EndComponent => _connection.EndComponent?.InstanceName;

        /// <summary>
        /// 終了ポート表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("接続情報")]
        [DisplayName("終了ポート")]
        [ReadOnly(true)]
        public string EndPort => _connection.EndPort;
    }
}