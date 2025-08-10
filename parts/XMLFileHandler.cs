// The MIT License (MIT)
// 
// Copyright (c) .NET Foundation and Contributors
// 
// All rights reserved.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

/*
 * Generated class source code
 * 生成されたクラスのソースコード
 * ClassName: XMLFileHandler
 * クラス名:XMLFileHandler
 * Generated on: 2024-07-13
 * 生成日:2024-07-13
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// ClassName: XMLFileHandler クラス名:{ClassName}: XMLFileHandler
    /// </summary>
    public class XMLFileHandler
    {
        private Dictionary<string, List<string>> _stringdata;

        /// <summary>
        /// Initializes a new instance of the <see cref="XMLFileHandler"/>
        /// class.
        /// </summary>
        public XMLFileHandler()
        {
            /// Constructor implementation コンストラクタの実装
            _stringdata = new Dictionary<string, List<string>>();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="XMLFileHandler"/> class.
        /// </summary>
        ~XMLFileHandler()
        {
            //// Destructor implementation
            /// Destructor implementation
            /// デストラクタの実装
        }

        /// <summary>
        /// Gets or sets the string data.
        /// </summary>
        /// <value>The string data.</value>
        public Dictionary<string, List<string>> StringData
        {
            get => _stringdata;
            set => _stringdata = value;
        }

        /// <summary>
        /// Method: UpDateList メソッド: UpDateList
        /// </summary>
        /// <returns>void</returns>
        public void UpDateList(string name, List<string> data)
        {
            _stringdata[name] = data;
            // Method implementation
            // メソッドの実装
        }

        /// <summary>
        /// Method: SaveToFile メソッド: SaveToFile
        /// </summary>
        /// <returns>void</returns>
        public void SaveToFile(string filePath)
        {
            // Method implementation
            // メソッドの実装
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Dictionary<string, List<string>>));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    serializer.Serialize(writer, _stringdata);
                }

                MessageBox.Show($"{filePath} Write Complete");
            }
            catch (Exception ex)
            {
                LogcatTraceHelper.Instance.LogError("Error", $"Error occurred while saving data: {ex.Message}");
            }
        }

        /// <summary>
        /// Method: LoadFromFile メソッド: LoadFromFile
        /// </summary>
        /// <returns>void</returns>
        public void LoadFromFile(string filePath)
        {
            // Method implementation
            // メソッドの実装
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Dictionary<string, List<string>>));
                using (StreamReader reader = new StreamReader(filePath))
                {
                    _stringdata = (Dictionary<string, List<string>>)serializer.Deserialize(reader);
                }
                LogcatTraceHelper.Instance.LogError("Info", $"{filePath} is read ");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{filePath} Load Fail{ex.Message} ");
            }
        }

        // INSERTION MARKER
    }
}