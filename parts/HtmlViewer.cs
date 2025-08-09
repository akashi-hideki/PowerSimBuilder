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
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    /// <summary>
    /// HTMLビューア
    /// </summary>
    /// <seealso cref="System.Windows.Forms.Control" />
    public class HtmlViewer : Control
    {
        private WebBrowser _browser;
        private List<string> _htmlTextList;
        private GroupBox _groupBox;
        private string groupBoxTitle = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="HtmlViewer"/> class.
        /// </summary>
        public HtmlViewer()
        {
            // InitializeComponentHeader
            _browser = new WebBrowser();
            _browser.Dock = DockStyle.Fill;

            // gray background
            this.BackColor = Color.LightGray;
        }

        // HTML Text List set
        /// <summary>
        /// Gets or sets the HTML text.
        /// </summary>
        /// <value>The HTML text.</value>
        public List<string> HTMLText
        {
            get => _htmlTextList;
            set
            {
                _htmlTextList = value;
                RenderHTML();
            }
        }

        /// <summary>
        /// UserGroup
        /// </summary>
        // GroupBox set
        public GroupBox UserGroup
        {
            get => _groupBox;
            set
            {
                if (_groupBox != null)
                {
                    _groupBox.Controls.Clear();
                }

                _groupBox = value;
                if (_groupBox != null)
                {
                    if (!string.IsNullOrEmpty(groupBoxTitle))
                    {
                        _groupBox.Text = $"Regarding {groupBoxTitle}";
                    }
                    // GroupBoxWebBrowserを追加
                    _groupBox.Controls.Add(_browser);

                    // GroupBox
                    _groupBox.BackColor = Color.LightGray;
                }
            }
        }

        /// <summary>
        /// RenderHTML
        /// </summary>

        private void RenderHTML()
        {
            if (_htmlTextList != null && _htmlTextList.Count > 0)
            {
                // HTMLリストの各要素を結合
                string combinedHtml = string.Join("\n", _htmlTextList);

                // タイトル (H1 から H4) を抽出して GroupBox.Text に設定
                SetGroupBoxTitle(combinedHtml);

                // preventDefaultサポートの互換性を考慮したJavaScriptを含めたHTML
                string noEditHtml = $@"
        <html>
        <head>
        <style>
            /* 全体のスタイル */
            body {{
                background-color: lightgray;
                font-family: Arial, sans-serif;
                font-size: 10px; /* すべてのテキストのフォントサイズを12pxに設定 */
                font-size: 60%; /* 通常の文字サイズ */
                line-height: 0.0; /* 通常の行間 */
            }}

            /* セクションごとのスタイル */
            section {{
                margin-bottom: 4px; /* セクション間の余白を狭く設定 */
            }}

            /* 水平線 (ボーダーライン) のスタイル */
            hr {{
                margin-top: 4px; /* 水平線の上の余白を狭く */
                margin-bottom: 4px; /* 水平線の下の余白を狭く */
            }}
        </style>
        <script>
            window.onload = function() {{
                var addEvent = function(element, event, handler) {{
                    if (element.addEventListener) {{
                        element.addEventListener(event, handler, false);
                    }} else if (element.attachEvent) {{
                        element.attachEvent('on' + event, handler);
                    }}
                }};

                var preventEventDefault = function(event) {{
                    if (event.preventDefault) {{
                        event.preventDefault();
                    }} else {{
                        event.returnValue = false;  // 旧IE用の対策
                    }}
                }};

                addEvent(document, 'contextmenu', function(event) {{
                    preventEventDefault(event);
                }});

                addEvent(document, 'selectstart', function(event) {{
                    preventEventDefault(event);
                }});

                addEvent(document, 'keydown', function(event) {{
                    preventEventDefault(event);
                }});
            }};
        </script>
        </head>
        <body>
            <section>{combinedHtml}</section> <!-- セクションごとの表示 -->
        </body>
        </html>";

                // WebBrowserに変換されたHTMLを表示
                _browser.DocumentText = noEditHtml;
            }
        }

        /// <summary>
        /// SetGroupBoxTitle
        /// </summary>
        /// <param name="htmlContent"></param>
        private void SetGroupBoxTitle(string htmlContent)
        {
            // H1～H4タグを順番に探す
            groupBoxTitle = ExtractHtmlTagContent(htmlContent, "h1")
                        ?? ExtractHtmlTagContent(htmlContent, "h2")
                        ?? ExtractHtmlTagContent(htmlContent, "h3")
                        ?? ExtractHtmlTagContent(htmlContent, "h4");

            // GroupBoxのタイトルに設定
            if (_groupBox != null)
            {
                _groupBox.Text = !string.IsNullOrEmpty(groupBoxTitle) ? $"Regarding {groupBoxTitle}" : "Default Title";
            }
        }

        /// <summary>
        /// ExtractHtmlTagContent
        /// </summary>
        /// <param name="htmlContent"></param>
        /// <param name="tagName"></param>
        /// <returns></returns>

        private string ExtractHtmlTagContent(string htmlContent, string tagName)
        {
            // 正規表現で指定されたタグの中身を抽出
            string pattern = $@"<{tagName}[^>]*>(.*?)<\/{tagName}>";
            var match = Regex.Match(htmlContent, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }
    }
}