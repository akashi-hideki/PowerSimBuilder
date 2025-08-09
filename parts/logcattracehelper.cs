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
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

/// <summary>
///       Namespace Name - CSgen.
/// </summary>
namespace PowerSimBuilder
{
    /// <summary>
    /// LogcatTraceHelper クラス
    /// </summary>
    public class LogcatTraceHelper
    {
        private static string traceFilePath;
        private static bool traceEnabled;
        private static bool debugOutputEnabled;
        private static string appNamespace;

        private static readonly LogcatTraceHelper _instance = new LogcatTraceHelper();

        /// <summary>
        ///      Property Name - Instance.
        ///  </summary>
        public static LogcatTraceHelper Instance => _instance;

        /// <summary>
        ///      Property Name - TraceEnabled.
        ///  </summary>
        public bool TraceEnabled
        {
            get => traceEnabled;
            set => traceEnabled = value;
        }

        /// <summary>
        ///      Property Name - DebugOutputEnabled.
        ///  </summary>
        public bool DebugOutputEnabled
        {
            get => debugOutputEnabled;
            set => debugOutputEnabled = value;
        }

        /// <summary>
        ///  Function Name :  LogcatTraceHelper.
        /// </summary>
        /// <returns>void.</returns>
        private LogcatTraceHelper()
        {
            traceEnabled = false;
            debugOutputEnabled = true; traceFilePath = GenerateTraceFileName();
            appNamespace = GetApplicationNamespace();
        }

        /// <summary>
        ///  Function Name :  Log.
        /// </summary>
        /// <param name="tag">This tag's Datatype is : string.</param>
        /// <param name="message">This message's Datatype is : string.</param>
        /// <param name="tracecnt">This tracecnt's Datatype is : int.</param>
        /// <returns>void.</returns>
        public void Log(string tag, string message, int tracecnt = 8)
        {
            string formattedMessage = FormatLogMessage(tag, message, tracecnt);
            OutputDebugMessage(formattedMessage); WriteTrace(formattedMessage);
        }

        /// <summary>
        ///  Function Name :  LogError.
        /// </summary>
        /// <param name="tag">This tag's Datatype is : string.</param>
        /// <param name="message">This message's Datatype is : string.</param>
        /// <param name="tracecnt">This tracecnt's Datatype is : int.</param>
        /// <returns>void.</returns>
        public void LogError(string tag, string message, int tracecnt = 8)
        {
            string formattedMessage = FormatLogMessage(tag, message, tracecnt, "ERROR");
            OutputDebugMessage(formattedMessage); WriteTrace(formattedMessage);
        }

        /// <summary>
        ///  Function Name :  LogWarning.
        /// </summary>
        /// <param name="tag">This tag's Datatype is : string.</param>
        /// <param name="message">This message's Datatype is : string.</param>
        /// <param name="tracecnt">This tracecnt's Datatype is : int.</param>
        /// <returns>void.</returns>
        public void LogWarning(string tag, string message, int tracecnt = 8)
        {
            string formattedMessage = FormatLogMessage(tag, message, tracecnt, "WARNING");
            OutputDebugMessage(formattedMessage); WriteTrace(formattedMessage);
        }
        /// <summary>
        /// SimpeMessage
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="messeage"></param>
        public void SimpleMessage(string tag, string messeage)
        {
            string formattedMessage = $"\t[S/{tag}]\t{messeage}";
            OutputDebugMessage(formattedMessage); WriteTrace(formattedMessage);

        }

        /// <summary>
        ///  Function Name :  FormatLogMessage.
        /// </summary>
        /// <param name="tag">This tag's Datatype is : string.</param>
        /// <param name="message">This message's Datatype is : string.</param>
        /// <param name="tracecnt">This tracecnt's Datatype is : int.</param>
        /// <param name="level">This level's Datatype is : string.</param>
        /// <returns>string.</returns>
        public string FormatLogMessage(string tag, string message, int tracecnt, string level = "INFO")
        {
            StackTrace stackTrace = new StackTrace(true); HashSet<string> uniqueLines = new HashSet<string>(); List<string> stackInfoList = new List<string>();
            for (int i = 0; i < stackTrace.FrameCount; i++)
            {
                StackFrame frame = stackTrace.GetFrame(i);
                System.Reflection.MethodBase method = frame.GetMethod();
                Type declaringType = method.DeclaringType;

                if (declaringType != null)
                {
                    if (declaringType == typeof(LogcatTraceHelper))
                    {
                        continue;
                    }
                    string info = $"[{declaringType.FullName}.{method.Name}()]-[{frame.GetFileName()}({frame.GetFileLineNumber()}):]";
                    if (IsUserDefinedClass(declaringType) || IsFormClass(declaringType))
                    {
                        if (uniqueLines.Add(info))
                        {
                            stackInfoList.Add(info);
                        }
                    }
                }
            }

            string formattedStackInfo = string.Join("\n", stackInfoList);
            return $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")}\t{message}[{level}/{tag}]\n[TraceInfo]\n{formattedStackInfo}\n";
        }

        /// <summary>
        ///  Function Name :  OutputDebugMessage.
        /// </summary>
        /// <param name="message">This message's Datatype is : string.</param>
        /// <returns>void.</returns>
        private void OutputDebugMessage(string message)
        {
            if (debugOutputEnabled)
            {
                Debug.WriteLine(message);
            }
        }

        /// <summary>
        ///  Function Name :  LogDictionaryOfLists.
        /// </summary>
        /// <param name="comment">This comment's Datatype is : string.</param>
        /// <param name="dictionary">This dictionary's Datatype is :
        ///     System.Collections.Generic.Dictionary string,
        ///     System.Collections.Generic.List T&gt;&gt;.</param>
        /// <returns>void.</returns>
        public void LogDictionaryOfLists<T>(string comment, Dictionary<string, List<T>> dictionary)
        {
            string formattedMessage = FormatLogMessage("Data", $"Dictionary contains {dictionary.Count} keys.<<{comment}>>", 8);
            OutputDebugMessage(formattedMessage);
            foreach (KeyValuePair<string, List<T>> kvp in dictionary)
            {
                string key = kvp.Key;
                List<T> list = kvp.Value;

                OutputDebugMessage($"Key: {key}, contains {list.Count} elements:");

                for (int i = 0; i < list.Count; i++)
                {
                    T item = list[i];
                    OutputDebugMessage($"\tElement {i}: {item?.ToString() ?? "null"}");
                }
            }
        }

        /// <summary>
        ///  Function Name :  LogDictionaryOfListsString.
        /// </summary>
        /// <param name="comment">This comment's Datatype is : string.</param>
        /// <param name="dictionary">This dictionary's Datatype is :
        ///     System.Collections.Generic.Dictionary string,
        ///     System.Collections.Generic.List string&gt;&gt;.</param>
        /// <returns>void.</returns>
        public void LogDictionaryOfListsString(string comment, Dictionary<string, List<string>> dictionary)
        {
            string formattedMessage = FormatLogMessage("Data", $"Dictionary contains {dictionary.Count} keys.<<{comment}>>", 8);
            OutputDebugMessage(formattedMessage);
            foreach (KeyValuePair<string, List<string>> kvp in dictionary)
            {
                string key = kvp.Key;
                List<string> list = kvp.Value;

                OutputDebugMessage($"Key: {key}, contains {list.Count} elements:");

                for (int i = 0; i < list.Count; i++)
                {
                    string item = list[i];
                    OutputDebugMessage($"\tElement {i}: {item?.ToString() ?? "null"}");
                }
            }
        }

        /// <summary>
        ///  Function Name :  LogDictionaryOfListsT.
        /// </summary>
        /// <param name="comment">This comment's Datatype is : string.</param>
        /// <param name="data">This data's Datatype is : T.</param>
        /// <returns>void.</returns>
        public void LogDictionaryOfListsT<T>(string comment, T data)
        {
            string formattedMessage = FormatLogMessage("Data", $"Class {nameof(T)} contains keys.<<{comment}>>", 8);
            OutputDebugMessage(formattedMessage); List<string> resultList = ConvertToList<T>(data);
            foreach (var item in resultList)
            {
                OutputDebugMessage(item);
            }
        }

        /// <summary>
        ///  Function Name :  ConvertToList.
        /// </summary>
        /// <param name="obj">This obj's Datatype is : T.</param>
        /// <returns>System.Collections.Generic.List string&gt;.</returns>
        private List<string> ConvertToList<T>(T obj)
        {
            List<string> resultList = new List<string>();

            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (var property in properties)
            {
                object value = property.GetValue(obj);
                resultList.Add($"{property.Name}: {value}");
            }

            FieldInfo[] fields = typeof(T).GetFields();

            foreach (var field in fields)
            {
                object value = field.GetValue(obj);
                resultList.Add($"{field.Name}: {value}");
            }

            return resultList;
        }

        /// <summary>
        ///  Function Name :  IsUserDefinedClass.
        /// </summary>
        /// <param name="declaringType">This declaringType's Datatype is :
        ///     System.Type.</param>
        /// <returns>bool.</returns>
        private bool IsUserDefinedClass(Type declaringType)
        {
            if (declaringType.Namespace == null)
            {
                return true;
            }

            return declaringType.Namespace.StartsWith(appNamespace);
        }

        /// <summary>
        ///  Function Name :  IsFormClass.
        /// </summary>
        /// <param name="declaringType">This declaringType's Datatype is :
        ///     System.Type.</param>
        /// <returns>bool.</returns>
        private bool IsFormClass(Type declaringType)
        {
            return declaringType.IsSubclassOf(typeof(Form));
        }

        /// <summary>
        ///  Function Name :  GetApplicationNamespace.
        /// </summary>
        /// <returns>string.</returns>
        private string GetApplicationNamespace()
        {
            var entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (entryAssembly != null)
            {
                var entryType = entryAssembly.EntryPoint?.DeclaringType;
                if (entryType != null)
                {
                    return entryType.Namespace;
                }
            }
            return null;
        }

        /// <summary>
        ///  Function Name :  GenerateTraceFileName.
        /// </summary>
        /// <returns>string.</returns>
        private static string GenerateTraceFileName()
        {
            string fileName = $"Trace_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.log";
            string appFolder = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appFolder, fileName);
        }

        /// <summary>
        ///  Function Name :  WriteTrace.
        /// </summary>
        /// <param name="message">This message's Datatype is : string.</param>
        /// <returns>void.</returns>
        private static void WriteTrace(string message)
        {
            if (traceEnabled)
            {
                try
                {
                    using (FileStream fileStream = new FileStream(traceFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(fileStream))
                    {
                        writer.WriteLine(message);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"トレースファイルの書き込み中にエラーが発生しました: {ex.Message}");
                }
            }
        }
    }
}