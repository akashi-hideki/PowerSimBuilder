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
        [Category("Basic Settings")]
        [DisplayName("Connection Name")]
        [Description("Display name for the connection")]
        public string ConnectionName { get; set; } = "Connection 1";

        /// <summary>
        /// データ型プロパティ
        /// 注意点: 接続オブジェクトと連動
        /// </summary>
        [Category("Basic Settings")]
        [DisplayName("Data Type")]
        [Description("Data type to transmit")]
        public string DataType
        {
            get => _connection.DataType;
            set => _connection.DataType = value;
        }

        /// <summary>
        /// 開始コンポーネント表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("Connection Info")]
        [DisplayName("Source Component")]
        [ReadOnly(true)]
        public string StartComponent => _connection.StartComponent?.InstanceName;

        /// <summary>
        /// 開始ポート表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("Connection Info")]
        [DisplayName("Source Port")]
        [ReadOnly(true)]
        public string StartPort => _connection.StartPort;

        /// <summary>
        /// 終了コンポーネント表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("Connection Info")]
        [DisplayName("Target Component")]
        [ReadOnly(true)]
        public string EndComponent => _connection.EndComponent?.InstanceName;

        /// <summary>
        /// 終了ポート表示
        /// 注意点: 読み取り専用
        /// </summary>
        [Category("Connection Info")]
        [DisplayName("Target Port")]
        [ReadOnly(true)]
        public string EndPort => _connection.EndPort;
    }
}