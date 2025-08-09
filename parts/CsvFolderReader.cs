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
using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
namespace PowerSimBuilder
{
    public class CsvFolderReader
    {
        // プロパティでフォルダパスを指定
        public string FolderPath { get; set; }

        // コンストラクタは引数を使用しない
        public CsvFolderReader() { }

        // 指定したフォルダ内のCSVファイルを読み込み、Dictionaryに変換するメソッド
        public Dictionary<string, List<List<string>>> ReadCsvFiles()
        {
            var csvDataDictionary = new Dictionary<string, List<List<string>>>();

            // フォルダ内のすべてのCSVファイルを取得
            if (Directory.Exists(FolderPath))
            {
                string[] csvFiles = Directory.GetFiles(FolderPath, "*.csv");

                foreach (var filePath in csvFiles)
                {
                    List<List<string>> csvRecords = ReadCsvFile(filePath);
                    string fileName = Path.GetFileName(filePath);
                    csvDataDictionary.Add(fileName, csvRecords);
                }
            }
            else
            {
                throw new DirectoryNotFoundException($"Folder is mot found : {FolderPath}");

            }

            return csvDataDictionary;
        }

        // CsvHelperを使ってCSVファイルを読み込むヘルパーメソッド
        /// <summary>
        /// Read CSV file using CsvHelper
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private List<List<string>> ReadCsvFile(string filePath)
        {
            List<List<string>> records = new List<List<string>>();

            CsvConfiguration config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false, // ヘッダーがある場合はtrueに設定
                BadDataFound = null, // 不正データが見つかったときの処理を無効化
                Delimiter = "," // デフォルトの区切り文字（カンマ）
            };

            using (StreamReader reader = new StreamReader(filePath))
            using (CsvReader csv = new CsvReader(reader, config))
            {
                while (csv.Read())
                {
                    List<string> row = new List<string>();
                    for (int i = 0; csv.TryGetField(i, out string field); i++)
                    {
                        row.Add(field);
                    }
                    records.Add(row);
                }
            }

            return records;
        }
    }
}
