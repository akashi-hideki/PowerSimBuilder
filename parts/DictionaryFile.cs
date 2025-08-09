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
 * ClassName: DictionaryXmlSerializer
 * Generated on: 2024-08-02
 * 生成されたクラスのソースコード
 * クラス名:DictionaryXmlSerializer
 * 生成日:2024-08-02
 */

using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// ClassName: DictionaryXmlSerializer クラス名: DictionaryXmlSerializer
    /// </summary>
    public class DictionaryFile
    {
        private Dictionary<string, List<string>> _filedata;
        private string _foldername;
        private string _filename;
        private string _fullpath_filename;
        private readonly string _folder_path;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryFile"/>
        /// class.
        /// </summary>
        public DictionaryFile()
        {
            _filedata = new Dictionary<string, List<string>>();
            _foldername = "dictinary";
            _filename = "string_data";
            _fullpath_filename = Path.Combine(Application.StartupPath, _foldername, $"{_filename}.xml");
            _folder_path = Path.Combine(Application.StartupPath, _foldername);
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="DictionaryFile"/> class.
        /// </summary>
        ~DictionaryFile()
        {
        }

        /// <summary>
        /// Gets or sets the file data.
        /// </summary>
        /// <value>
        /// The file data.
        /// </value>
        public Dictionary<string, List<string>> FileData
        {
            get => _filedata;
            set => _filedata = value;
        }

        /// <summary>
        /// Property: FolderPath プロパティ: FolderPath
        /// </summary>
        /// <returns>string</returns>
        public string Folder
        {
            get => _foldername;
            set => _foldername = value;
        }

        /// <summary>
        /// Property: FileName プロパティ: FileName
        /// </summary>
        /// <returns>string</returns>
        public string FileName
        {
            get => _filename;
            set => _filename = value;
        }

        /// <summary>
        /// Method: Load メソッド: Load
        /// </summary>
        /// <returns>void</returns>
        public void LoadFile()
        {
            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();
            _fullpath_filename = Path.Combine(Application.StartupPath, _foldername, $"{_filename}.xml");
            // Method implementation メソッドの実装
            if (true == File.Exists(_fullpath_filename))
            {
                ret = LoadDictionaryFromFile(_fullpath_filename);
            }
            _filedata = ret;
            LogcatTraceHelper.Instance.LogDictionaryOfListsString("load", _filedata);
        }

        /// <summary>
        /// Method: Save メソッド: Save
        /// </summary>
        /// <returns>void</returns>
        public void Save()
        {
            _fullpath_filename = Path.Combine(Application.StartupPath, _foldername, $"{_filename}.xml");
            string folder = Path.Combine(Application.StartupPath, _foldername);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            LogcatTraceHelper.Instance.LogDictionaryOfListsString("save", _filedata);
            SaveDictionaryToFile(_filedata, _fullpath_filename);
        }

        /// <summary>
        /// Saves the dictionary to file.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="filePath">The file path.</param>
        private void SaveDictionaryToFile(Dictionary<string, List<string>> dictionary, string filePath)
        {
            SerializableDictionary serializableDictionary = new SerializableDictionary();
            foreach (KeyValuePair<string, List<string>> kvp in dictionary)
            {
                serializableDictionary.Items.Add(new SerializableKeyValuePair { Key = kvp.Key, Value = kvp.Value });
            }

            XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary));
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, serializableDictionary);
            }
        }

        /// <summary>
        /// Loads the dictionary from file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        private Dictionary<string, List<string>> LoadDictionaryFromFile(string filePath)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(SerializableDictionary));
            using (StreamReader reader = new StreamReader(filePath))
            {
                SerializableDictionary serializableDictionary = (SerializableDictionary)serializer.Deserialize(reader);
                Dictionary<string, List<string>> dictionary = new Dictionary<string, List<string>>();
                foreach (SerializableKeyValuePair item in serializableDictionary.Items)
                {
                    dictionary[item.Key] = item.Value;
                }
                return dictionary;
            }
        }

        /// <summary>
        /// 内部クラスを使用してシリアライズ/デシリアライズを行う
        /// </summary>
        [XmlRoot("dictionary")]
        public class SerializableDictionary
        {
            /// <summary>
            /// Gets or sets the items.
            /// </summary>
            /// <value>The items.</value>
            [XmlElement("item")]
            public List<SerializableKeyValuePair> Items { get; set; } = new List<SerializableKeyValuePair>();
        }

        /// <summary>
        /// Key Pair
        /// </summary>
        public class SerializableKeyValuePair
        {
            /// <summary>
            /// Gets or sets the key.
            /// </summary>
            /// <value>The key.</value>
            [XmlAttribute("key")]
            public string Key { get; set; }

            /// <summary>
            /// Gets or sets the value.
            /// </summary>
            /// <value>The value.</value>
            [XmlElement("value")]
            public List<string> Value { get; set; }
        }

        // INSERTION MARKER
    }
}