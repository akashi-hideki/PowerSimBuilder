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
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// RegistryManager
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RegistryManager<T> where T : new()
    {
        private readonly JsonFileManager jsonFileManager;
        private readonly Dictionary<Type, Func<object, string>> typeToStringConverters;
        private readonly Dictionary<Type, Func<string, object>> stringToTypeConverters;

        /// <summary>
        /// Gets or sets the settings.
        /// </summary>
        /// <value>The settings.</value>
        public T Settings { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegistryManager{T}"/>
        /// class.
        /// </summary>
        public RegistryManager()
        {
            Settings = new T();
            jsonFileManager = new JsonFileManager
            {
                JsonName = typeof(T).Name
            };
            // 型ごとの処理をディクショナリに追加
            typeToStringConverters = new Dictionary<Type, Func<object, string>>
            {
                { typeof(string), value => value.ToString() },
                { typeof(int), value => value.ToString() },
                { typeof(bool), value => value.ToString() },
                { typeof(double), value=> value.ToString() },
                { typeof(long), value => value.ToString() },
                { typeof(Enum), value => value.ToString() }
            };

            stringToTypeConverters = new Dictionary<Type, Func<string, object>>
            {
                { typeof(string), str => str },
                { typeof(int), str => int.Parse(str) },
                { typeof(bool), str => bool.Parse(str) },
                { typeof(double), str => double.Parse(str)},
                { typeof(long),str => long.Parse(str) },
                { typeof(Enum), str => Enum.Parse(typeof(Enum), str) }
            };
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        /// <returns></returns>
        public bool SaveSettings()
        {
            try
            {
                // TをDictionary<string, List<string>>に変換
                Dictionary<string, List<string>> dict = ConvertToDictionary(Settings);

                // JsonFileManagerのJsonDataプロパティにデータを設定
                jsonFileManager.JsonData = dict;
                // データを保存
                jsonFileManager.SaveSetting();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Loads the settings.
        /// </summary>
        /// <returns></returns>
        public bool LoadSettings()
        {
            try
            {
                // データをロード
                jsonFileManager.LoadSetting();

                // JsonFileManagerのJsonDataプロパティからDictionary<string, List<string>>を取得し、Tに変換
                Settings = ConvertFromDictionary(jsonFileManager.JsonData);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Displays the settings.
        /// </summary>
        /// <param name="labels">The labels.</param>
        public void DisplaySettings(List<Label> labels)
        {
            if (Settings == null)
            {
                return;
            }

            Dictionary<string, List<string>> dict = ConvertToDictionary(Settings);
            int labelIndex = 0;

            foreach (KeyValuePair<string, List<string>> entry in dict)
            {
                if (labelIndex >= labels.Count)
                {
                    break;
                }

                string value = string.Join(", ", entry.Value);
                labels[labelIndex].Text = $"{entry.Key}: {value}";
                labelIndex++;
            }
        }

        /// <summary>
        /// Displays the settings tool strinp.
        /// </summary>
        /// <param name="labels">The labels.</param>
        public void DisplaySettingsToolStrinp(List<ToolStripLabel> labels)
        {
            if (Settings == null)
            {
                return;
            }

            Dictionary<string, List<string>> dict = ConvertToDictionary(Settings);
            int labelIndex = 0;

            foreach (KeyValuePair<string, List<string>> entry in dict)
            {
                if (labelIndex >= labels.Count)
                {
                    break;
                }

                string value = string.Join(", ", entry.Value);
                labels[labelIndex].Text = $"{entry.Key}: {value}";
                labelIndex++;
            }
        }

        /// <summary>
        /// Converts to dictionary.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns></returns>
        private Dictionary<string, List<string>> ConvertToDictionary(T settings)
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                object value = prop.GetValue(settings);

                if (value != null && typeToStringConverters.ContainsKey(prop.PropertyType))
                {
                    dict[prop.Name] = new List<string> { typeToStringConverters[prop.PropertyType](value) };
                }
                else if (value != null && prop.PropertyType.IsEnum)
                {
                    dict[prop.Name] = new List<string> { value.ToString() };
                }
            }

            return dict;
        }

        /// <summary>
        /// Converts from dictionary.
        /// </summary>
        /// <param name="dict">The dictionary.</param>
        /// <returns></returns>
        private T ConvertFromDictionary(Dictionary<string, List<string>> dict)
        {
            T obj = new T();

            foreach (PropertyInfo prop in typeof(T).GetProperties())
            {
                if (dict.ContainsKey(prop.Name))
                {
                    string value = dict[prop.Name][0];

                    if (stringToTypeConverters.ContainsKey(prop.PropertyType))
                    {
                        prop.SetValue(obj, stringToTypeConverters[prop.PropertyType](value));
                    }
                    else if (prop.PropertyType.IsEnum)
                    {
                        prop.SetValue(obj, Enum.Parse(prop.PropertyType, value));
                    }
                }
            }

            return obj;
        }
}
}