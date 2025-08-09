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
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// FileManager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FileManager<T> where T : new()
    {
        private string defaultFolder;
        private readonly string defaultFileName;
        private readonly string typeName;
        private List<T> listData;
        private List<string> pathList;
        private string _folder = "Setting";
        private readonly string _stringdata_folder = "StringData";
        private readonly XMLFileHandler stringFileHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileManager{T}"/>
        /// class.
        /// </summary>
        public FileManager()
        {
            stringFileHandler = new XMLFileHandler();
            defaultFolder = Path.Combine(Application.StartupPath, _folder);
            typeName = typeof(T).Name;
            defaultFileName = $"{typeName}_{"default"}.xml";
            listData = new List<T>();
        }

        /// <summary>
        /// FolderPath
        /// </summary>
        public virtual string Folder
        {
            get => _folder;
            set
            {
                _folder = value;
                string old_path = defaultFileName;
                defaultFolder = Path.Combine(Application.StartupPath, _folder);
            }
        }

        /// <summary>
        /// Gets the default folder files.
        /// </summary>
        /// <value>The default folder files.</value>
        public List<string> defaultFolderFiles => pathList = getFolderFiles(defaultFolder);

        /// <summary>
        /// getFolderFiles
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private List<string> getFolderFiles(string path)
        {
            List<string> pathlist = new List<string>();
            string[] files = Directory.GetFiles(defaultFolder, "*.xml");
            foreach (string temp in files)
            {
                pathlist.Add(temp);
            }
            return pathList;
        }

        /// <summary>
        /// Saves to file. ログファイル用
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public bool SaveToFile(List<T> data)
        {
            listData = data;
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.InitialDirectory = defaultFolder;
                saveFileDialog.FileName = $"{typeName}_{DateTime.Now:yyyyMMddHHmmss}.xml";
                saveFileDialog.Filter = "XML Files (*.xml)|*.xml";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    FlexXmlSerializer<T>.SerializeToFile(saveFileDialog.FileName, listData);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Saves to file name specified.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="data">The data.</param>
        public void SaveToFileNameSpecified(string path, List<T> data)
        {
            listData = data;
            FlexXmlSerializer<T>.SerializeToFile(path, listData);
        }

        /// <summary>
        /// Removes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        public void RemoveFile(string path)
        {
            File.Delete(path);
        }

        /// <summary>
        /// Saves the default file.
        /// </summary>
        /// <param name="data">The data.</param>
        public void SaveDefaultFile(List<T> data)
        {
            string filePath = Path.Combine(defaultFolder, defaultFileName);
            EnsureDirectoryExists(defaultFolder);
            FlexXmlSerializer<T>.SerializeToFile(filePath, data);
        }

        /// <summary>
        /// Saves the dictionary to file.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        public bool SaveDictionaryToFile<TKey>(Dictionary<TKey, T> dictionary)
        {
            List<T> list = ConvertDictionaryToList(dictionary);
            bool ret = SaveToFile(list);
            return ret;
        }

        /// <summary>
        /// Saves the default dictionary file.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        public void SaveDefaultDictionaryFile<TKey>(Dictionary<TKey, T> dictionary)
        {
            string filePath = Path.Combine(defaultFolder, defaultFileName);
            EnsureDirectoryExists(defaultFolder);
            List<T> list = ConvertDictionaryToList(dictionary);
            FlexXmlSerializer<T>.SerializeToFile(filePath, list);
        }

        /// <summary>
        /// Loads from file.
        /// </summary>
        /// <param name="prev_data">The previous data.</param>
        /// <returns></returns>
        public List<T> LoadFromFile(List<T> prev_data)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.InitialDirectory = defaultFolder;
                openFileDialog.Filter = "XML Files (*.xml)|*.xml";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    listData = decodeFile(openFileDialog.FileName);
                    prev_data = listData;
                }
            }
            return prev_data;
        }

        /// <summary>
        /// LoadStringFile
        /// </summary>
        /// <param name="fname"></param>
        /// <returns></returns>
        public Dictionary<string, List<string>> LoadStringFile(string fname)
        {
            Dictionary<string, List<string>> ret = new Dictionary<string, List<string>>();
            string filename = Path.Combine(Application.StartupPath, _stringdata_folder, fname);
            if (File.Exists(filename))
            {
                stringFileHandler.LoadFromFile(filename);
                ret = stringFileHandler.StringData;
            }
            return ret;
        }

        /// <summary>
        /// SaveStringFile
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="data"></param>
        public void SaveStringFile(string fname, Dictionary<string, List<string>> data)
        {
            stringFileHandler.StringData = data;
            string folder_path = Path.Combine(Application.StartupPath, _stringdata_folder);
            string filename = Path.Combine(Application.StartupPath, _stringdata_folder, fname);
            EnsureDirectoryExists(folder_path);
            stringFileHandler.StringData = data;
            stringFileHandler.SaveToFile(filename);
        }

        /// <summary>
        /// Loads the dictionary file.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key selector.</param>
        /// <param name="prev_data">The previous data.</param>
        /// <returns></returns>
        public Dictionary<TKey, T> LoadDictionaryFile<TKey>(Func<T, TKey> keySelector, Dictionary<TKey, T> prev_data)
        {
            List<T> p_data = ConvertDictionaryToList(prev_data);
            List<T> list = LoadFromFile(p_data);
            if (list != null)
            {
                return ConvertListToDictionary(list, keySelector);
            }
            return new Dictionary<TKey, T>();
        }

        /// <summary>
        /// Loads the default file.
        /// </summary>
        /// <returns></returns>
        public List<T> LoadDefaultFile()
        {
            string filePath = Path.Combine(defaultFolder, defaultFileName);
            if (File.Exists(filePath))
            {
                return decodeFile(filePath);
            }
            else
            {
                // ファイルが存在しない場合は新しいインスタンスを返す
                return new List<T> { new T() };
            }
        }

        /// <summary>
        /// Loads the default folder.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, List<T>> LoadDefaultFolder()
        {
            Dictionary<string, List<T>> ret = new Dictionary<string, List<T>>();
            List<string> filles = getFolderFiles(defaultFolder);
            foreach (string path in filles)
            {
                if (File.Exists(path))
                {
                    List<T> datas = decodeFile(path);
                    if (datas.Count > 0)
                    {
                        string key = Path.GetFileNameWithoutExtension(path);
                        ret[key] = datas;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Saves the default folder.
        /// </summary>
        /// <param name="datas">The datas.</param>
        public void SaveDefaultFolder(Dictionary<string, List<T>> datas)
        {
            EnsureDirectoryExists(defaultFolder);
            foreach (KeyValuePair<string, List<T>> data in datas)
            {
                string fname = Path.Combine(defaultFolder, $"{data.Key}.xml");
                FlexXmlSerializer<T>.SerializeToFile(fname, data.Value);
            }
        }

        /// <summary>
        /// Loads the specified file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        public List<T> LoadSpecifiedFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                return decodeFile(filePath);
            }
            else
            {
                // ファイルが存在しない場合は新しいインスタンスを返す
                return new List<T> { new T() };
            }
        }

        /// <summary>
        /// Loads the dictionary default.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        public Dictionary<TKey, T> LoadDictionaryDefault<TKey>(Func<T, TKey> keySelector)
        {
            List<T> list = LoadDefaultFile();
            if (list != null)
            {
                return ConvertListToDictionary(list, keySelector);
            }
            return new Dictionary<TKey, T>();
        }

        /// <summary>
        /// Converts the dictionary to list.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns></returns>
        private List<T> ConvertDictionaryToList<TKey>(Dictionary<TKey, T> dictionary)
        {
            return new List<T>(dictionary.Values);
        }

        /// <summary>
        /// Decodes the file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        private List<T> decodeFile(string filePath)
        {
            List<T> ret = new List<T>();
            try
            {
                ret = FlexXmlSerializer<T>.DeserializeFromFile(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return ret;
        }

        /// <summary>
        /// Converts the list to dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="list">The list.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        private Dictionary<TKey, T> ConvertListToDictionary<TKey>(List<T> list, Func<T, TKey> keySelector)
        {
            Dictionary<TKey, T> dictionary = new Dictionary<TKey, T>();
            foreach (T item in list)
            {
                TKey key = keySelector(item);
                if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, item);
                }
                else
                {
                    MessageBox.Show($"Duplicate key found: {key}");
                }
            }
            return dictionary;
        }

        /// <summary>
        /// EnsureDirectoryExists
        /// </summary>
        /// <param name="folder_path"></param>
        private void EnsureDirectoryExists(string folder_path)
        {
            if (!Directory.Exists(folder_path))
            {
                // LogcatTraceHelper.Instance.LogError("ViewFileManger", $"{folder_path}が作成されました.");
                Directory.CreateDirectory(folder_path);
            }
        }
    }
}