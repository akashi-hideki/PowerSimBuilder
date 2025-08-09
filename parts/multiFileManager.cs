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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms; // FolderBrowserDialog のために必要

namespace PowerSimBuilder
{
    /// <summary>
    /// multiFileManager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class multiFileManager<T> : FileManager<T> where T : new()
    {
        private string folderPath;
        private readonly string default_datafolder = "Tabs";
        private string specified_datafolder;
        private Dictionary<string, List<T>> _filedata;
        // public Dictionary<string, List<string>> OrderMap { get; set; }

        /// <summary>
        ///      Property Name - FileData.
        ///  </summary>
        public Dictionary<string, List<T>> FileData
        {
            get => _filedata;
            set => _filedata = value ?? new Dictionary<string, List<T>>();
        }

        /// <summary>
        ///  Function Name :  multiFileManager.
        /// </summary>
        /// <returns>void.</returns>
        public multiFileManager() : base()
        {
            folderPath = Path.Combine(Application.StartupPath, default_datafolder);
            _filedata = new Dictionary<string, List<T>>();
        }

        /// <summary>
        ///      Property Name - Folder.
        ///  </summary>
        public override string Folder
        {
            get => specified_datafolder;
            set
            {
                specified_datafolder = value;
                folderPath = Path.Combine(Application.StartupPath, specified_datafolder);
            }
        }

        /// <summary>
        ///  Function Name :  EnsureDirectoryExists.
        /// </summary>
        /// <param name="folder_path">This folder_path's Datatype is : string.
        ///     </param>
        /// <returns>void.</returns>
        private void EnsureDirectoryExists(string folder_path)
        {
            if (!Directory.Exists(folder_path))
            {
                LogcatTraceHelper.Instance.LogError("ViewFileManger", $"{folder_path}が作成されました.");
                Directory.CreateDirectory(folder_path);
            }
        }

        /// <summary>
        ///  Function Name :  SetFolderPathWithDialog.
        /// </summary>
        /// <returns>bool.</returns>
        public bool SetFolderPathWithDialog()
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder for storing data files.";
                dialog.SelectedPath = folderPath;  // 現在のフォルダパスを初期選択として設定
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        ///  Function Name :  RemoveData.
        /// </summary>
        /// <param name="name">This name's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void RemoveData(string name)
        {
            if (_filedata.ContainsKey(name))
            {
                _filedata.Remove(name);
            }
            SaveAllData();
        }

        /// <summary>
        ///  Function Name :  RenameData.
        /// </summary>
        /// <param name="old_name">This old_name's Datatype is : string.</param>
        /// <param name="new_name">This new_name's Datatype is : string.</param>
        /// <returns>void.</returns>
        public void RenameData(string old_name, string new_name)
        {
            List<T> data;
            if (_filedata.ContainsKey(old_name))
            {
                data = _filedata[old_name];
            }
            else
            {
                data = new List<T>();
            }
            _filedata[new_name] = data;
            if (_filedata.ContainsKey(old_name))
            {
                _filedata.Remove(old_name);
            }
            LogcatTraceHelper.Instance.LogError("ViewFileManger", $"{folderPath}が作成されました.");
            SaveAllData();
        }

        /// <summary>
        ///  Function Name :  SaveAllData.
        /// </summary>
        /// <returns>void.</returns>
        public void SaveAllData()
        {
            List<string> save_path = new List<string>();
            EnsureDirectoryExists(folderPath);
            string[] files = Directory.GetFiles(folderPath, "*.xml");

            foreach (KeyValuePair<string, List<T>> entry in _filedata)
            {
                string filePath = Path.Combine(folderPath, $"{entry.Key}.xml");
                save_path.Add(filePath);
                base.SaveToFileNameSpecified(filePath, entry.Value);
            }

            foreach (string exist_file in files)
            {
                if (!check_file_exist(exist_file, save_path))
                {
                    if (File.Exists(exist_file))
                    {
                        File.Delete(exist_file);
                    }
                }
            }
        }

        /// <summary>
        ///  Function Name :  LoadAllData.
        /// </summary>
        /// <returns>void.</returns>
        public void LoadAllData()
        {
            string tabName = $"anonymous_{typeof(T).Name}";
            if (true == Directory.Exists(folderPath))
            {
                string[] files = Directory.GetFiles(folderPath, "*.xml");

                _filedata.Clear();
                if (files.Length > 0)
                {
                    foreach (string file in files)
                    {
                        tabName = Path.GetFileNameWithoutExtension(file);
                        List<T> data = base.LoadSpecifiedFile(file);
                        if (data != null)
                        {
                            _filedata[tabName] = data;
                        }
                    }
                }
                else
                {
                    _filedata[tabName] = new List<T>();
                }
            }
            else
            {
                _filedata.Clear();
                _filedata[tabName] = new List<T>();
            }
        }

        /// <summary>
        ///  Function Name :  isExistFile.
        /// </summary>
        /// <returns>bool.</returns>
        public bool isExistFile()
        {
            bool ret = true;
            string Key = null;

            if (FileData.Count == 1)
            {
                Key = FileData.First().Key;
                if (Key == $"anonymous_{typeof(T).Name}")
                {
                    ret = false;
                }
            }
            return ret;
        }

        /// <summary>
        ///  Function Name :  check_file_exist.
        /// </summary>
        /// <param name="path">This path's Datatype is : string.</param>
        /// <param name="check_path">This check_path's Datatype is :
        ///     System.Collections.Generic.List string>.</param>
        /// <returns>bool.</returns>
        public bool check_file_exist(string path, List<string> check_path)
        {
            bool ret = false;
            foreach (string check in check_path)
            {
                if (check == path)
                {
                    ret = true;
                    break;
                }
            }
            return ret;
        }

        /// <summary>
        ///  Function Name :  TabFileRename.
        /// </summary>
        /// <param name="old_tab">This old_tab's Datatype is : string.</param>
        /// <param name="new_tab">This new_tab's Datatype is : string.</param>
        /// <param name="data">This data's Datatype is :
        ///     System.Collections.Generic.List T>.</param>
        /// <returns>void.</returns>
        public void TabFileRename(string old_tab, string new_tab, List<T> data)
        {
            string old_filePath = Path.Combine(folderPath, $"{old_tab}.xml");
            string new_filePath = Path.Combine(folderPath, $"{new_tab}.xml");
            if (File.Exists(old_filePath))
            {
                File.Copy(old_filePath, new_filePath);
                File.Delete(old_filePath);
            }
        }
    }
}