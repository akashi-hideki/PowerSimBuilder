using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PowerSimBuilder
{
    #region Usage
    /*
    // =====================================================================
    // ListDictionaryConverter 使用例
    // =====================================================================
    
    // 1. コンバーターのインスタンス化
    var converter = new ListDictionaryConverter<Person, Employee>();
    
    // 2. 共通変換関数の登録
    converter.RegisterCommonConversionFunctions();
    
    // 3. カスタム変換関数の登録（オプション）
    converter.RegisterConversionFunction("CalculateAge", value => 
        DateTime.Now.Year - ((DateTime)value).Year);
    
    // 4. プロパティマッピングの設定
    converter.MappingTable.AddT1ToT2Mapping("Id", "EmployeeId");
    converter.MappingTable.AddT1ToT2Mapping("FullName", "Name", "ToUpper");
    converter.MappingTable.AddT1ToT2Mapping("BirthDate", "Age", "CalculateAge");
    
    // 5. 文字列変換の設定
    converter.ConvertT1ToStringList = p => new List<string> 
    { 
        p.Id.ToString(), 
        p.FullName, 
        p.BirthDate.ToString("yyyy-MM-dd") 
    };
    
    converter.ConvertStringListToT1 = list => new Person
    {
        Id = int.Parse(list[0]),
        FullName = list[1],
        BirthDate = DateTime.Parse(list[2])
    };
    
    // 6. 変換実行
    Dictionary<string, List<Person>> personDict = ...;
    
    // T1 → T2 変換
    Dictionary<string, List<Employee>> employeeDict = 
        converter.ConvertT1ToT2Dictionary(personDict);
    
    // T1 → 文字列リスト 変換
    Dictionary<string, List<string>> stringDict = 
        converter.ConvertT1ToStringDictionary(personDict);
    
    // 7. チャンクサイズのカスタマイズ（必要に応じて）
    public class CustomConverter : ListDictionaryConverter<Person, Employee>
    {
        protected override int GetChunkSize(List<string> list, int startIndex)
        {
            return 3; // 固定サイズのチャンク分割
        }
    }
    */
    #endregion

    /// <summary>
    /// プロパティ変換定義を保持するクラス
    /// <para>ソースプロパティからターゲットプロパティへのマッピングと、適用する変換関数名を定義します。</para>
    /// </summary>
    public class PropertyConversion
    {
        /// <summary>
        /// ソースオブジェクトのプロパティ名
        /// </summary>
        public string SourceProperty { get; set; }

        /// <summary>
        /// ターゲットオブジェクトのプロパティ名
        /// </summary>
        public string TargetProperty { get; set; }

        /// <summary>
        /// 適用する変換関数の名前（登録済みの関数名）
        /// </summary>
        public string ConversionFunctionName { get; set; }
    }

    /// <summary>
    /// T1とT2の間のプロパティマッピングテーブルを管理するクラス
    /// <para>双方向（T1→T2およびT2→T1）のマッピングを保持します。</para>
    /// </summary>
    /// <typeparam name="T1">ソース/ターゲット型1</typeparam>
    /// <typeparam name="T2">ソース/ターゲット型2</typeparam>
    public class MappingTable<T1, T2>
    {
        /// <summary>
        /// T1 → T2 方向のプロパティマッピングリスト
        /// </summary>
        public List<PropertyConversion> T1ToT2Mappings { get; } = new List<PropertyConversion>();

        /// <summary>
        /// T2 → T1 方向のプロパティマッピングリスト
        /// </summary>
        public List<PropertyConversion> T2ToT1Mappings { get; } = new List<PropertyConversion>();

        /// <summary>
        /// T1 → T2 マッピングを追加
        /// </summary>
        /// <param name="source">T1のプロパティ名</param>
        /// <param name="target">T2のプロパティ名</param>
        /// <param name="conversionFuncName">変換関数名（オプション）</param>
        public void AddT1ToT2Mapping(string source, string target, string conversionFuncName = null)
        {
            T1ToT2Mappings.Add(new PropertyConversion
            {
                SourceProperty = source,
                TargetProperty = target,
                ConversionFunctionName = conversionFuncName
            });
        }

        /// <summary>
        /// T2 → T1 マッピングを追加
        /// </summary>
        /// <param name="source">T2のプロパティ名</param>
        /// <param name="target">T1のプロパティ名</param>
        /// <param name="conversionFuncName">変換関数名（オプション）</param>
        public void AddT2ToT1Mapping(string source, string target, string conversionFuncName = null)
        {
            T2ToT1Mappings.Add(new PropertyConversion
            {
                SourceProperty = source,
                TargetProperty = target,
                ConversionFunctionName = conversionFuncName
            });
        }
    }

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
    public class ListDictionaryConverter<T1, T2>
    {
        #region 変換関数管理

        // 変換関数の辞書（名前 → 関数）
        private readonly Dictionary<string, Func<object, object>> _conversionFunctions =
            new Dictionary<string, Func<object, object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// コンバーターコンストラクタ
        /// <para>デフォルトの変換関数を登録します。</para>
        /// </summary>
        public ListDictionaryConverter()
        {
            // 基本型変換関数
            RegisterConversionFunction("ToUpper", value => value?.ToString()?.ToUpper());
            RegisterConversionFunction("ToLower", value => value?.ToString()?.ToLower());
            RegisterConversionFunction("ToString", value => value?.ToString());
            RegisterConversionFunction("ToInt", value => Convert.ToInt32(value));
            RegisterConversionFunction("ToLong", value => Convert.ToInt64(value));
            RegisterConversionFunction("ToDouble", value => Convert.ToDouble(value));
            RegisterConversionFunction("ToDecimal", value => Convert.ToDecimal(value));
            RegisterConversionFunction("ToDateTime", value => Convert.ToDateTime(value));
            RegisterConversionFunction("ToBoolean", value => Convert.ToBoolean(value));
        }

        /// <summary>
        /// カスタム変換関数を登録
        /// </summary>
        /// <param name="name">関数名（大文字小文字を区別しない）</param>
        /// <param name="conversionFunc">変換関数（object → object）</param>
        /// <exception cref="ArgumentException">関数名が空の場合</exception>
        public void RegisterConversionFunction(string name, Func<object, object> conversionFunc)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Function name cannot be empty", nameof(name));

            _conversionFunctions[name] = conversionFunc;
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// T1とT2のプロパティマッピングテーブル
        /// <para>プロパティの対応関係と使用する変換関数を定義します。</para>
        /// </summary>
        public MappingTable<T1, T2> MappingTable { get; set; } = new MappingTable<T1, T2>();

        /// <summary>
        /// T1 → T2 変換関数（自動生成）
        /// <para>MappingTableの設定に基づいて自動生成されます。</para>
        /// </summary>
        public Func<T1, T2> ConvertT1ToT2 => CreateT1ToT2Converter();

        /// <summary>
        /// T2 → T1 変換関数（自動生成）
        /// <para>MappingTableの設定に基づいて自動生成されます。</para>
        /// </summary>
        public Func<T2, T1> ConvertT2ToT1 => CreateT2ToT1Converter();

        /// <summary>
        /// T1 → List&lt;string&gt; 変換関数
        /// <para>手動設定が必要です。</para>
        /// </summary>
        public Func<T1, List<string>> ConvertT1ToStringList { get; set; }

        /// <summary>
        /// List&lt;string&gt; → T1 変換関数
        /// <para>手動設定が必要です。</para>
        /// </summary>
        public Func<List<string>, T1> ConvertStringListToT1 { get; set; }

        /// <summary>
        /// T2 → List&lt;string&gt; 変換関数
        /// <para>手動設定が必要です。</para>
        /// </summary>
        public Func<T2, List<string>> ConvertT2ToStringList { get; set; }

        /// <summary>
        /// List&lt;string&gt; → T2 変換関数
        /// <para>手動設定が必要です。</para>
        /// </summary>
        public Func<List<string>, T2> ConvertStringListToT2 { get; set; }

        #endregion

        #region マッピングベースの変換関数生成

        /// <summary>
        /// T1 → T2 変換関数を生成
        /// </summary>
        /// <returns>生成された変換関数</returns>
        private Func<T1, T2> CreateT1ToT2Converter()
        {
            return source =>
            {
                if (source == null) return default;

                T2 target = Activator.CreateInstance<T2>();
                ApplyMappings(source, target, MappingTable.T1ToT2Mappings);
                return target;
            };
        }

        /// <summary>
        /// T2 → T1 変換関数を生成
        /// </summary>
        /// <returns>生成された変換関数</returns>
        private Func<T2, T1> CreateT2ToT1Converter()
        {
            return source =>
            {
                if (source == null) return default;

                T1 target = Activator.CreateInstance<T1>();
                ApplyMappings(source, target, MappingTable.T2ToT1Mappings);
                return target;
            };
        }

        /// <summary>
        /// マッピングを適用してオブジェクトを変換
        /// </summary>
        /// <typeparam name="TSource">ソース型</typeparam>
        /// <typeparam name="TTarget">ターゲット型</typeparam>
        /// <param name="source">ソースオブジェクト</param>
        /// <param name="target">ターゲットオブジェクト</param>
        /// <param name="mappings">適用するマッピングリスト</param>
        private void ApplyMappings<TSource, TTarget>(
            TSource source,
            TTarget target,
            List<PropertyConversion> mappings)
        {
            Type sourceType = typeof(TSource);
            Type targetType = typeof(TTarget);

            foreach (PropertyConversion mapping in mappings)
            {
                PropertyInfo sourceProp = sourceType.GetProperty(mapping.SourceProperty);
                PropertyInfo targetProp = targetType.GetProperty(mapping.TargetProperty);

                if (sourceProp == null || targetProp == null) continue;

                object value = sourceProp.GetValue(source);

                // 変換関数が指定されている場合
                if (!string.IsNullOrEmpty(mapping.ConversionFunctionName))
                {
                    if (_conversionFunctions.TryGetValue(
                        mapping.ConversionFunctionName,
                        out Func<object, object> conversionFunc))
                    {
                        value = conversionFunc(value);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Conversion function '{mapping.ConversionFunctionName}' not found");
                    }
                }

                // 型変換が必要な場合
                if (value != null && targetProp.PropertyType != value.GetType())
                {
                    value = Convert.ChangeType(value, targetProp.PropertyType);
                }

                targetProp.SetValue(target, value);
            }
        }

        #endregion

        #region 主要変換メソッド

        /// <summary>
        /// T1ディクショナリ → T2ディクショナリ 変換
        /// </summary>
        /// <param name="input">入力ディクショナリ</param>
        /// <returns>変換されたディクショナリ</returns>
        /// <exception cref="InvalidOperationException">変換関数が未設定の場合</exception>
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
        /// <param name="input">入力ディクショナリ</param>
        /// <returns>変換されたディクショナリ</returns>
        /// <exception cref="InvalidOperationException">変換関数が未設定の場合</exception>
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
        /// <param name="input">入力ディクショナリ</param>
        /// <returns>変換されたディクショナリ</returns>
        /// <exception cref="InvalidOperationException">変換関数が未設定の場合</exception>
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
        /// <param name="input">入力ディクショナリ</param>
        /// <returns>変換されたディクショナリ</returns>
        /// <exception cref="InvalidOperationException">変換関数が未設定の場合</exception>
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
        /// <param name="input">入力ディクショナリ</param>
        /// <returns>変換されたディクショナリ</returns>
        /// <exception cref="InvalidOperationException">変換関数が未設定の場合</exception>
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
        /// <param name="input">入力ディクショナリ</param>
        /// <returns>変換されたディクショナリ</returns>
        /// <exception cref="InvalidOperationException">変換関数が未設定の場合</exception>
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
        /// フラットな文字列リストをチャンク分割してオブジェクトに変換
        /// </summary>
        /// <typeparam name="T">変換先の型</typeparam>
        /// <param name="flatList">フラットな文字列リスト</param>
        /// <param name="converter">チャンク→オブジェクト変換関数</param>
        /// <returns>変換されたオブジェクトのシーケンス</returns>
        private IEnumerable<T> ChunkList<T>(List<string> flatList, Func<List<string>, T> converter)
        {
            int index = 0;
            while (index < flatList.Count)
            {
                int chunkSize = GetChunkSize(flatList, index);
                List<string> chunk = flatList.GetRange(index, chunkSize);
                index += chunkSize;
                yield return converter(chunk);
            }
        }

        /// <summary>
        /// チャンクサイズを決定（オーバーライド可能）
        /// </summary>
        /// <param name="list">全体の文字列リスト</param>
        /// <param name="startIndex">処理開始インデックス</param>
        /// <returns>チャンクサイズ</returns>
        protected virtual int GetChunkSize(List<string> list, int startIndex)
        {
            return 1; // デフォルト: 1要素=1オブジェクト
        }

        /// <summary>
        /// 変換関数の有効性を検証
        /// </summary>
        /// <param name="converter">検証対象の変換関数</param>
        /// <param name="conversionName">変換名（エラーメッセージ用）</param>
        /// <exception cref="InvalidOperationException">変換関数がnullの場合</exception>
        private void ValidateConversion(Delegate converter, string conversionName)
        {
            if (converter == null)
            {
                throw new InvalidOperationException(
                    $"{conversionName} converter is not initialized");
            }
        }

        #endregion

        #region 共通変換関数

        /// <summary>
        /// 一般的な変換関数を一括登録
        /// <para>登録される関数:</para>
        /// <list type="bullet">
        /// <item>Trim, TrimStart, TrimEnd</item>
        /// <item>ToBase64, FromBase64</item>
        /// <item>ToCamelCase, ToPascalCase, ToTitleCase</item>
        /// <item>ToJson</item>
        /// </list>
        /// </summary>
        public void RegisterCommonConversionFunctions()
        {
            RegisterConversionFunction("Trim", value => value?.ToString()?.Trim());
            RegisterConversionFunction("TrimStart", value => value?.ToString()?.TrimStart());
            RegisterConversionFunction("TrimEnd", value => value?.ToString()?.TrimEnd());
            RegisterConversionFunction("ToBase64", value =>
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value.ToString());
                return Convert.ToBase64String(bytes);
            });
            RegisterConversionFunction("FromBase64", value =>
            {
                byte[] bytes = Convert.FromBase64String(value.ToString());
                return System.Text.Encoding.UTF8.GetString(bytes);
            });
            RegisterConversionFunction("ToCamelCase", value =>
                char.ToLowerInvariant(value.ToString()[0]) + value.ToString().Substring(1));
            RegisterConversionFunction("ToPascalCase", value =>
                char.ToUpperInvariant(value.ToString()[0]) + value.ToString().Substring(1));
            RegisterConversionFunction("ToTitleCase", value =>
                System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToString()));
            RegisterConversionFunction("ToJson", value =>
                System.Text.Json.JsonSerializer.Serialize(value));
        }

        #endregion
    }

    #region サンプルエンティティ（使用例で利用）
    public class Person
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }
    #endregion
}