namespace PowerSimBuilder
{
    /// <summary>
    /// パラメータアイテムクラス
    /// 作成日: 2025-08-10
    /// </summary>
    public class ParameterItem
    {
        /// <summary>
        /// パラメータ名
        /// 作成日: 2025-08-10
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// パラメータ値
        /// 作成日: 2025-08-10
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// 単位
        /// 作成日: 2025-08-10
        /// </summary>
        public string Unit { get; set; }

        public override string ToString()
        {
            return $"{Name}: {Value} {Unit}";
        }
    }
}