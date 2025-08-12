using System.ComponentModel;

namespace PowerSimBuilder
{
    // 接続プロパティ表示用クラス
    public class ConnectionProperties    {
        private readonly Connection _connection;

        public ConnectionProperties(Connection connection)
        {
            _connection = connection;
        }

        [Category("基本設定")]
        [DisplayName("接続名")]
        [Description("接続の表示名")]
        public string ConnectionName { get; set; } = "接続1";

        [Category("基本設定")]
        [DisplayName("データ型")]
        [Description("伝送するデータの型")]
        public string DataType
        {
            get => _connection.DataType;
            set => _connection.DataType = value;
        }

        [Category("接続情報")]
        [DisplayName("開始コンポーネント")]
        [ReadOnly(true)]
        public string StartComponent => _connection.StartComponent?.InstanceName;

        [Category("接続情報")]
        [DisplayName("開始ポート")]
        [ReadOnly(true)]
        public string StartPort => _connection.StartPort;

        [Category("接続情報")]
        [DisplayName("終了コンポーネント")]
        [ReadOnly(true)]
        public string EndComponent => _connection.EndComponent?.InstanceName;

        [Category("接続情報")]
        [DisplayName("終了ポート")]
        [ReadOnly(true)]
        public string EndPort => _connection.EndPort;
    }
}