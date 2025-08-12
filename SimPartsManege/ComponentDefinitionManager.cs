using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// コンポーネント定義管理クラス
    /// 作成日: 2025-08-15
    /// 注意点: XMLベースのコンポーネント定義管理システム
    /// </summary>
    public class ComponentDefinitionManager
    {
        private const string DefinitionFileName = "ComponentDefinitions.xml";
        private readonly string _definitionFilePath;

        /// <summary>
        /// ロードされたコンポーネント定義リスト
        /// 作成日: 2025-08-15
        /// 注意点: メモリ上に保持される定義データ
        /// </summary>
        public List<ComponentDefinition> Definitions { get; private set; } = new List<ComponentDefinition>();

        /// <summary>
        /// コンストラクタ
        /// 作成日: 2025-08-15
        /// 処理内容: 定義ファイルのパスを確定
        /// 注意点: 初回起動時にディレクトリを自動作成
        /// </summary>
        public ComponentDefinitionManager()
        {
            // アプリケーションディレクトリの取得
            string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            
            // 定義ファイル保存ディレクトリのパス作成
            string definesDirectory = Path.Combine(appDirectory, "defines");
            
            // 定義ファイルの完全パス
            _definitionFilePath = Path.Combine(definesDirectory, DefinitionFileName);

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(definesDirectory))
            {
                Directory.CreateDirectory(definesDirectory);
            }
        }

        /// <summary>
        /// コンポーネント定義をロード
        /// 作成日: 2025-08-15
        /// 処理内容:
        ///   - 定義ファイルが存在する場合はXMLから読み込み
        ///   - 存在しない場合はデフォルト定義を生成して保存
        /// 注意点: 初回起動時は必ずデフォルト定義が生成される
        /// </summary>
        public void LoadDefinitions()
        {
            if (File.Exists(_definitionFilePath))
            {
                // XMLファイルから定義を読み込む
                LoadFromXml();
            }
            else
            {
                // デフォルト定義を生成
                GenerateDefaultDefinitions();
                
                // 生成した定義をファイルに保存
                SaveDefinitions();
            }
        }

        /// <summary>
        /// XMLファイルから定義を読み込む
        /// 作成日: 2025-08-15
        /// 注意点: ファイルが存在しない場合は例外が発生する
        /// </summary>
        private void LoadFromXml()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ComponentDefinition>),
                    new XmlRootAttribute("ComponentDefinitions"));
                
                using (FileStream stream = new FileStream(_definitionFilePath, FileMode.Open))
                {
                    Definitions = (List<ComponentDefinition>)serializer.Deserialize(stream);
                }
            }
            catch (Exception ex)
            {
                // エラー発生時はデフォルト定義でフォールバック
                Definitions = GenerateDefaultDefinitions();
                
                // エラーログ出力（実際のアプリではロガーを使用）
                Console.WriteLine($"定義ファイルの読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// デフォルトコンポーネント定義を生成
        /// 作成日: 2025-08-15
        /// 戻り値: デフォルト定義リスト
        /// 注意点: アプリケーションに組み込まれる基本コンポーネント
        /// </summary>
        private List<ComponentDefinition> GenerateDefaultDefinitions()
        {
            return new List<ComponentDefinition>
            {
                new ComponentDefinition
                {
                    Name = "電圧形インバータ",
                    Type = "VoltageSourceInverter",
                    Category = "電源",
                    IconColor = "Red",
                    Parameters = new List<ParameterDefinition>
                    {
                        new ParameterDefinition { Name = "V_dc", Type = "double", DefaultValue = "300", Unit = "V" },
                        new ParameterDefinition { Name = "f_rated", Type = "double", DefaultValue = "60", Unit = "Hz" }
                    },
                    InputPorts = new List<string>(),
                    OutputPorts = new List<string> { "Output" }
                },
                new ComponentDefinition
                {
                    Name = "誘導電動機",
                    Type = "InductionMotor",
                    Category = "負荷",
                    IconColor = "Blue",
                    Parameters = new List<ParameterDefinition>
                    {
                        new ParameterDefinition { Name = "R1", Type = "double", DefaultValue = "0.15", Unit = "Ω" },
                        new ParameterDefinition { Name = "R2", Type = "double", DefaultValue = "0.25", Unit = "Ω" },
                        new ParameterDefinition { Name = "Lm", Type = "double", DefaultValue = "0.008", Unit = "H" },
                        new ParameterDefinition { Name = "J", Type = "double", DefaultValue = "0.02", Unit = "kg·m²" },
                        new ParameterDefinition { Name = "B", Type = "double", DefaultValue = "0.005", Unit = "N·m·s/rad" },
                        new ParameterDefinition { Name = "poles", Type = "int", DefaultValue = "4", Unit = "" }
                    },
                    InputPorts = new List<string> { "VoltageInput" },
                    OutputPorts = new List<string> { "SpeedOutput", "CurrentOutput" }
                },
                new ComponentDefinition
                {
                    Name = "故障監視システム",
                    Type = "FaultMonitor",
                    Category = "監視",
                    IconColor = "Orange",
                    Parameters = new List<ParameterDefinition>
                    {
                        new ParameterDefinition { 
                            Name = "Conditions", 
                            Type = "List<FaultCondition>", 
                            DefaultValue = "", 
                            Unit = "" 
                        }
                    },
                    InputPorts = new List<string> { "Input1", "Input2" },
                    OutputPorts = new List<string>()
                }
            };
        }

        /// <summary>
        /// 定義をXMLファイルに保存
        /// 作成日: 2025-08-15
        /// 注意点: 既存ファイルは上書きされる
        /// </summary>
        public void SaveDefinitions()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<ComponentDefinition>),
                    new XmlRootAttribute("ComponentDefinitions"));
                
                using (FileStream stream = new FileStream(_definitionFilePath, FileMode.Create))
                {
                    serializer.Serialize(stream, Definitions);
                }
            }
            catch (Exception ex)
            {
                // エラーハンドリング（実際のアプリではロガーを使用）
                Console.WriteLine($"定義ファイルの保存エラー: {ex.Message}");
            }
        }

        /// <summary>
        /// タイプを指定して定義を取得
        /// 作成日: 2025-08-15
        /// 戻り値: 該当するコンポーネント定義（存在しない場合はnull）
        /// </summary>
        public ComponentDefinition GetDefinition(string type)
        {
            return Definitions.Find(d => d.Type == type);
        }

        /// <summary>
        /// 新しいコンポーネント定義を追加
        /// 作成日: 2025-08-15
        /// 注意点: 追加後にファイル保存が必要
        /// </summary>
        public void AddDefinition(ComponentDefinition definition)
        {
            if (!Definitions.Exists(d => d.Type == definition.Type))
            {
                Definitions.Add(definition);
            }
        }

        /// <summary>
        /// カテゴリリストを取得
        /// 作成日: 2025-08-15
        /// 戻り値: ユニークなカテゴリ名のリスト
        /// </summary>
        public List<string> GetCategories()
        {
            var categories = new List<string>();
            foreach (var definition in Definitions)
            {
                if (!categories.Contains(definition.Category))
                {
                    categories.Add(definition.Category);
                }
            }
            return categories;
        }

        /// <summary>
        /// カテゴリ別コンポーネント定義を取得
        /// 作成日: 2025-08-15
        /// 戻り値: カテゴリごとの定義リスト
        /// </summary>
        public Dictionary<string, List<ComponentDefinition>> GetDefinitionsByCategory()
        {
            var categorized = new Dictionary<string, List<ComponentDefinition>>();
            
            foreach (var definition in Definitions)
            {
                if (!categorized.ContainsKey(definition.Category))
                {
                    categorized[definition.Category] = new List<ComponentDefinition>();
                }
                categorized[definition.Category].Add(definition);
            }
            
            return categorized;
        }
    }
}