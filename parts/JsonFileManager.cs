// The MIT License (MIT)
// 
// Copyright (c) .NET Foundation and Contributors
// 
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

/*
 * Generated class source code
 * ClassName: JsonFileManager
 * Generated on: 2024-08-11
 * 生成されたクラスのソースコード
 * クラス名:JsonFileManager
 * 生成日:2024-08-11
 */

using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// ClassName: JsonFileManager クラス名: JsonFileManager
    /// </summary>
    public class JsonFileManager
    {
        private Dictionary<string, List<string>> _jsondata;
        private string _jsonname;
        private string _jsonpath;
        private readonly string _config_path = "Config";

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonFileManager"/>
        /// class.
        /// </summary>
        public JsonFileManager()
        {
            /// Constructor implementation コンストラクタの実装
            _jsondata = new Dictionary<string, List<string>>();
            _jsonpath = Path.Combine(Application.StartupPath, _config_path, $"{GetType().Namespace}_Setting.json");
            if (!Directory.Exists(Path.Combine(Application.StartupPath, _config_path)))
            {
                Directory.CreateDirectory(Path.Combine(Application.StartupPath, _config_path));
            }
        }

        /// <summary>
        /// Gets or sets the json data.
        /// </summary>
        /// <value>The json data.</value>

        public Dictionary<string, List<string>> JsonData
        {
            get => _jsondata;
            set => _jsondata = value;
        }

        /// <summary>
        /// Property: JsonName プロパティ: JsonName
		/// </summary>
		/// <returns>string</returns>
		public string JsonName
        {
            get => _jsonname;
            set
            {
                _jsonname = value;
                _jsonpath = Path.Combine(Application.StartupPath, _config_path, $"{GetType().Namespace}_{_jsonname}_Setting.json");
                if (!Directory.Exists(Path.Combine(Application.StartupPath, _config_path)))
                {
                    Directory.CreateDirectory(Path.Combine(Application.StartupPath, _config_path));
                }
            }
        }

        /// <summary>
        /// Method: LoadSetting メソッド: LoadSetting
        /// </summary>
        /// <returns>void</returns>
        public void LoadSetting()
        {
            // Method implementation メソッドの実装
            if (!File.Exists(_jsonpath))
            {
                _jsondata = new Dictionary<string, List<string>>();
                string default_jsonString = JsonSerializer.Serialize(_jsondata);
                File.WriteAllText(_jsonpath, default_jsonString);
            }

            string jsonString = File.ReadAllText(_jsonpath);
            _jsondata = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(jsonString);
        }

        /// <summary>
        /// Method: SaveSetting メソッド: SaveSetting
        /// </summary>
        /// <returns>void</returns>
        public void SaveSetting()
        {
            // Method implementation メソッドの実装
            string jsonString = JsonSerializer.Serialize(_jsondata);
            if (!Directory.Exists(Path.Combine(Application.StartupPath, _config_path)))
            {
                Directory.CreateDirectory(Path.Combine(Application.StartupPath, _config_path));
            }
            File.WriteAllText(_jsonpath, jsonString);
        }

        // INSERTION MARKER
    }
}