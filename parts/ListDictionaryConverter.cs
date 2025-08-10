using System;
using System.Collections.Generic;
using System.Linq;

namespace PowerSimBuilder
{
    /// <summary>
    /// ListDictionaryConverterクラスは、ジェネリック型T1とT2の間でディクショナリ変換を行うためのクラスです。
    /// </summary>
    /// <summary>
    /// ジェネリック型T1とT2の間でディクショナリ変換を行うコンバータークラス
    /// <para>主な機能:</para>
    /// <list type="bullet">
    /// <item>Dictionary&lt;string, List&lt;T1&gt;&gt; ⇄ Dictionary&lt;string, List&lt;T2&gt;&gt;</item>
    /// <item>Dictionary&lt;string, List&lt;T1&gt;&gt; ⇄ Dictionary&lt;string, List&lt;string&gt;&gt;</item>
    /// <item>Dictionary&lt;string, List&lt;T2&gt;&gt; ⇄ Dictionary&lt;string, List&lt;string&gt;&gt;</item>
    /// </list>
    /// </summary>
    /// <typeparam name="T1">変換元/変換先の型1</typeparam>
    /// <typeparam name="T2">変換元/変換先の型2</typeparam>
    /// <remarks>
    /// 使用前に必要な変換関数をプロパティに設定する必要があります。
    /// チャンク分割が必要な場合はGetChunkSizeメソッドをオーバーライドしてください。
    /// </remarks>
    public class ListDictionaryConverter<T1, T2>
    {
        #region 変換プロパティ

        /// <summary>
        /// T1 → T2 変換関数
        /// <para>[必須設定] T1オブジェクトをT2オブジェクトに変換するロジック</para>
        /// <example>
        /// converter.ConvertT1ToT2 = t1 => new T2 { ... };
        /// </example>
        /// </summary>
        public Func<T1, T2> ConvertT1ToT2 { get; set; }

        /// <summary>
        /// T2 → T1 変換関数
        /// <para>[必須設定] T2オブジェクトをT1オブジェクトに変換するロジック</para>
        /// <example>
        /// converter.ConvertT2ToT1 = t2 => new T1 { ... };
        /// </example>
        /// </summary>
        public Func<T2, T1> ConvertT2ToT1 { get; set; }

        /// <summary>
        /// T1 → List&lt;string&gt; 変換関数
        /// <para>[オプション] T1オブジェクトを文字列リストにシリアライズするロジック</para>
        /// <example>
        /// converter.ConvertT1ToStringList = t1 => new List&lt;string&gt; { t1.Id, t1.Name };
        /// </example>
        /// </summary>
        public Func<T1, List<string>> ConvertT1ToStringList { get; set; }

        /// <summary>
        /// List&lt;string&gt; → T1 変換関数
        /// <para>[オプション] 文字列リストをT1オブジェクトにデシリアライズするロジック</para>
        /// <example>
        /// converter.ConvertStringListToT1 = list => new T1 { 
        ///     Id = int.Parse(list[0]), 
        ///     Name = list[1] 
        /// };
        /// </example>
        /// </summary>
        public Func<List<string>, T1> ConvertStringListToT1 { get; set; }

        /// <summary>
        /// T2 → List&lt;string&gt; 変換関数
        /// <para>[オプション] T2オブジェクトを文字列リストにシリアライズするロジック</para>
        /// <example>
        /// converter.ConvertT2ToStringList = t2 => new List&lt;string&gt; { t2.Code, t2.Value };
        /// </example>
        /// </summary>
        public Func<T2, List<string>> ConvertT2ToStringList { get; set; }

        /// <summary>
        /// List&lt;string&gt; → T2 変換関数
        /// <para>[オプション] 文字列リストをT2オブジェクトにデシリアライズするロジック</para>
        /// <example>
        /// converter.ConvertStringListToT2 = list => new T2 { 
        ///     Code = list[0], 
        ///     Value = decimal.Parse(list[1]) 
        /// };
        /// </example>
        /// </summary>
        public Func<List<string>, T2> ConvertStringListToT2 { get; set; }

        #endregion

        #region 主要変換メソッド

        /// <summary>
        /// T1ディクショナリ → T2ディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ (キー: string, 値: List&lt;T1&gt;)</param>
        /// <returns>変換済みディクショナリ (キー: string, 値: List&lt;T2&gt;)</returns>
        /// <exception cref="InvalidOperationException">
        /// ConvertT1ToT2が未設定の場合にスロー
        /// </exception>
        /// <remarks>
        /// 各T1オブジェクトをConvertT1ToT2関数でT2に変換します。
        /// ディクショナリのキー構造は維持されます。
        /// </remarks>
        public Dictionary<string, List<T2>> ConvertT1ToT2Dictionary(Dictionary<string, List<T1>> input)
        {
            ValidateConversion(ConvertT1ToT2, nameof(ConvertT1ToT2));
            return input.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(ConvertT1ToT2).ToList()
            );
        }

        /// <summary>
        /// T2ディクショナリ → T1ディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ (キー: string, 値: List&lt;T2&gt;)</param>
        /// <returns>変換済みディクショナリ (キー: string, 値: List&lt;T1&gt;)</returns>
        /// <exception cref="InvalidOperationException">
        /// ConvertT2ToT1が未設定の場合にスロー
        /// </exception>
        /// <remarks>
        /// 各T2オブジェクトをConvertT2ToT1関数でT1に変換します。
        /// ディクショナリのキー構造は維持されます。
        /// </remarks>
        public Dictionary<string, List<T1>> ConvertT2ToT1Dictionary(Dictionary<string, List<T2>> input)
        {
            ValidateConversion(ConvertT2ToT1, nameof(ConvertT2ToT1));
            return input.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.Select(ConvertT2ToT1).ToList()
            );
        }

        /// <summary>
        /// T1ディクショナリ → 文字列リストディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ (キー: string, 値: List&lt;T1&gt;)</param>
        /// <returns>変換済みディクショナリ (キー: string, 値: List&lt;string&gt;)</returns>
        /// <exception cref="InvalidOperationException">
        /// ConvertT1ToStringListが未設定の場合にスロー
        /// </exception>
        /// <remarks>
        /// 各T1オブジェクトをConvertT1ToStringList関数で文字列リストに変換し、
        /// 結果をフラットな文字列リストとして結合します。
        /// </remarks>
        public Dictionary<string, List<string>> ConvertT1ToStringDictionary(Dictionary<string, List<T1>> input)
        {
            ValidateConversion(ConvertT1ToStringList, nameof(ConvertT1ToStringList));
            return input.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.SelectMany(ConvertT1ToStringList).ToList()
            );
        }

        /// <summary>
        /// 文字列リストディクショナリ → T1ディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ (キー: string, 値: List&lt;string&gt;)</param>
        /// <returns>変換済みディクショナリ (キー: string, 値: List&lt;T1&gt;)</returns>
        /// <exception cref="InvalidOperationException">
        /// ConvertStringListToT1が未設定の場合にスロー
        /// </exception>
        /// <remarks>
        /// 文字列リストをチャンク分割し、各チャンクをConvertStringListToT1関数でT1に変換します。
        /// チャンクサイズはGetChunkSizeメソッドで決定されます。
        /// </remarks>
        public Dictionary<string, List<T1>> ConvertStringToT1Dictionary(Dictionary<string, List<string>> input)
        {
            ValidateConversion(ConvertStringListToT1, nameof(ConvertStringListToT1));
            return input.ToDictionary(
                kv => kv.Key,
                kv => ChunkList(kv.Value, ConvertStringListToT1).ToList()
            );
        }

        /// <summary>
        /// T2ディクショナリ → 文字列リストディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ (キー: string, 値: List&lt;T2&gt;)</param>
        /// <returns>変換済みディクショナリ (キー: string, 値: List&lt;string&gt;)</returns>
        /// <exception cref="InvalidOperationException">
        /// ConvertT2ToStringListが未設定の場合にスロー
        /// </exception>
        /// <remarks>
        /// 各T2オブジェクトをConvertT2ToStringList関数で文字列リストに変換し、
        /// 結果をフラットな文字列リストとして結合します。
        /// </remarks>
        public Dictionary<string, List<string>> ConvertT2ToStringDictionary(Dictionary<string, List<T2>> input)
        {
            ValidateConversion(ConvertT2ToStringList, nameof(ConvertT2ToStringList));
            return input.ToDictionary(
                kv => kv.Key,
                kv => kv.Value.SelectMany(ConvertT2ToStringList).ToList()
            );
        }

        /// <summary>
        /// 文字列リストディクショナリ → T2ディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ (キー: string, 値: List&lt;string&gt;)</param>
        /// <returns>変換済みディクショナリ (キー: string, 値: List&lt;T2&gt;)</returns>
        /// <exception cref="InvalidOperationException">
        /// ConvertStringListToT2が未設定の場合にスロー
        /// </exception>
        /// <remarks>
        /// 文字列リストをチャンク分割し、各チャンクをConvertStringListToT2関数でT2に変換します。
        /// チャンクサイズはGetChunkSizeメソッドで決定されます。
        /// </remarks>
        public Dictionary<string, List<T2>> ConvertStringToT2Dictionary(Dictionary<string, List<string>> input)
        {
            ValidateConversion(ConvertStringListToT2, nameof(ConvertStringListToT2));
            return input.ToDictionary(
                kv => kv.Key,
                kv => ChunkList(kv.Value, ConvertStringListToT2).ToList()
            );
        }

        #endregion

        #region ヘルパーメソッド

        /// <summary>
        /// フラットな文字列リストをオブジェクトに変換
        /// </summary>
        /// <typeparam name="T">変換先のオブジェクト型</typeparam>
        /// <param name="flatList">フラットな文字列リスト</param>
        /// <param name="converter">チャンク→オブジェクト変換関数</param>
        /// <returns>変換されたオブジェクトのシーケンス</returns>
        /// <remarks>
        /// GetChunkSizeで決定されたサイズごとにチャンク分割し、
        /// 各チャンクをconverter関数でオブジェクトに変換します。
        /// </remarks>
        private IEnumerable<T> ChunkList<T>(
            List<string> flatList,
            Func<List<string>, T> converter)
        {
            int index = 0;
            while (index < flatList.Count)
            {
                int chunkSize = GetChunkSize(flatList, index);
                var chunk = flatList.GetRange(index, chunkSize);
                index += chunkSize;
                yield return converter(chunk);
            }
        }

        /// <summary>
        /// チャンクサイズ決定ロジック
        /// </summary>
        /// <param name="list">全体の文字列リスト</param>
        /// <param name="startIndex">チャンク開始インデックス</param>
        /// <returns>チャンクサイズ（要素数）</returns>
        /// <remarks>
        /// デフォルト実装：1要素=1オブジェクト
        /// オーバーライド例：
        /// <code>
        /// protected override int GetChunkSize(List&lt;string&gt; list, int startIndex)
        /// {
        ///     // 最初の要素にチャンクサイズが格納されている場合
        ///     return int.Parse(list[startIndex]);
        /// }
        /// </code>
        /// </remarks>
        protected virtual int GetChunkSize(List<string> list, int startIndex)
        {
            return 1;
        }

        /// <summary>
        /// 変換関数の有効性を検証
        /// </summary>
        /// <param name="converter">検証対象の変換関数</param>
        /// <param name="conversionName">変換名（エラーメッセージ用）</param>
        /// <exception cref="InvalidOperationException">
        /// 変換関数がnullの場合にスロー
        /// </exception>
        private void ValidateConversion(Delegate converter, string conversionName)
        {
            if (converter == null)
            {
                throw new InvalidOperationException(
                    $"{conversionName} コンバーターが初期化されていません。使用前にプロパティを設定してください。");
            }
        }

        #endregion
    }
}