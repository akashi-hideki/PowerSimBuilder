namespace PowerSimBuilder
{
    /// <summary>
    /// パラメータアイテムクラス
    /// 作成日: 2025-08-12
    /// 注意点: パラメータ情報を保持するシンプルなデータクラス
    /// </summary>
    public class ParameterItem
    {
        /// <summary>
        /// パラメータ名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// パラメータ値
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 単位
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// 文字列表現
        /// </summary>
        public override string ToString()
        {
            return $"{Name}: {Value} {Unit}";
        }
    }
}