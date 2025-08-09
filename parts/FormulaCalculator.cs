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
using NCalc;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// FormulaCalculator クラス
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class FormulaCalculator<T>
    {
        private T _parameters;
        private string _formula;
        private Dictionary<string, tableParam> _tableparam = new Dictionary<string, tableParam>();
        private List<string> _errorstring = new List<string>();

        /// <summary>
        /// T 型のパラメータを設定するプロパティ
        /// </summary>
        public T Parameters
        {
            get { return _parameters; }
            set { _parameters = value; }
        }

        /// <summary>
        /// Gets or sets the table parameter.
        /// </summary>
        /// <value>The table parameter.</value>
        public Dictionary<string, tableParam> TableParam
        {
            get { return _tableparam; }
            set { _tableparam = value; }
        }

        /// <summary>
        /// 計算式を設定するプロパティ。 設定された時点で式内の変数がTのプロパティと一致するかチェックします。
        /// </summary>
        public string Formula
        {
            get { return _formula; }
            set
            {
                _formula = value;
                CheckFormulaVariables(); // 計算式の変数チェックをプロパティセット時に行う
            }
        }

        /// <summary>
        /// 計算式内の変数がTのプロパティと一致するか確認します。
        /// </summary>
        private void CheckFormulaVariables()
        {
            if (string.IsNullOrEmpty(_formula))
            {
                string message = "The formula is not set.";
                LogcatTraceHelper.Instance.Log("Error",message);
                MessageBox.Show(message);
                return;
            }

            // 計算式を解析して変数をチェック
            Expression expression = new Expression(_formula);

            // 計算式に含まれる変数がDictionaryのキー (TitleName) として存在するかを確認
            var usedVariables = expression.Parameters.Keys;
            foreach (string titleName in usedVariables)
            {
                if (!_tableparam.ContainsKey(titleName))
                {
                    _errorstring.Add(titleName);
                    LogcatTraceHelper.Instance.Log("info", $"Title name does not exist in the formula.{titleName}");
                }
            }
            string errorstring = $"Error: Title name does not exist in the formula. {string.Join(",", _errorstring)}";
            MessageBox.Show(errorstring);
        }

        /// <summary>
        /// 指定した Listの TitleName と一致しないものがあれば false を返すメソッド
        /// </summary>
        /// <param name="_errorstring">The errorstring.</param>
        /// <param name="tableDict">検索対象の Dictionary</param>
        /// <param name="titleList">確認する List string</param>
        /// <returns>すべて一致すれば true、そうでなければ false</returns>
        public bool AreAllTitlesPresent(ref List<string> _errorstring, Dictionary<string, tableParam> tableDict, List<string> titleList)
        {
            bool ret = false;
            // Dictionary 内のすべての TitleName が List<string> に存在するか確認
            foreach (tableParam tableItem in tableDict.Values)
            {
                if (!titleList.Contains(tableItem.TitleName))
                {
                    _errorstring.Add(tableItem.TitleName);
                }
            }
            if (_errorstring.Count == 0)
            {
                ret = true; ;
            }
            return ret; // すべて一致すれば true を返す
        }

        /// <summary>
        /// 計算式を評価し、結果を倍精度浮動小数点として返す。
        /// 計算式内でTの数値プロパティを自動的に使用し、非数値プロパティは無視されます。
        /// DictionaryのキーをTitleNameとし、KeyNameを使用して実際のデータを利用します。
        /// </summary>
        /// <returns>計算結果</returns>
        public double Calculate()
        {
            if ((string.IsNullOrEmpty(_formula)) || (_parameters == null) || (_errorstring.Count > 0))
            {
                string message = "The formula is not set";
                LogcatTraceHelper.Instance.Log("Error", message);
                MessageBox.Show(message);

                return double.NaN;
            }

            // 計算式を解析して計算を行う
            Expression expression = new Expression(_formula);

            // Tのプロパティを取得し、数値プロパティだけを追加
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo prop in properties)
            {
                object propValue = prop.GetValue(_parameters);

                // 倍精度や整数、浮動小数点など数値型のみを計算式のパラメータに追加
                if (propValue is double || propValue is int || propValue is float)
                {
                    // Dictionaryのキー (TitleName) を使用し、対応する KeyName の値を設定
                    foreach (var param in _tableparam)
                    {
                        if (param.Value.KeyName == prop.Name)
                        {
                            expression.Parameters[param.Key] = propValue; // TitleName -> KeyName の値を設定
                        }
                    }
                }
            }

            try
            {
                // 計算式を評価して結果を返す
                return Convert.ToDouble(expression.Evaluate());
            }
            catch (Exception ex)
            {
                // 計算エラーが発生した場合は例外をキャッチして適切に処理
                LogcatTraceHelper.Instance.Log("Error", $"Calculation error.: {ex.Message}");
                return double.NaN; // エラー時にはNaNを返す
            }
        }
    }
}