using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PowerSimBuilder.ComponentEdit
{
    public static class PhysicsBlockManager
    {
        public static List<string> GetInsertedModels(string pythonCode)
        {
            var models = new List<string>();
            
            // 修正: ^ を削除し、インデントを許容する。PhysicsSnippetBuilder の出力形式に合わせる
            var regex = new Regex(@"#\s*===\s*physics:\s*(?<name>\w+)\s*\(auto-inserted", RegexOptions.Multiline);
            
            foreach (Match match in regex.Matches(pythonCode))
            {
                string modelName = match.Groups["name"].Value;
                if (!models.Contains(modelName)) 
                {
                    models.Add(modelName);
                }
            }
            return models;
        }

        public static string RemovePhysicsBlock(string pythonCode, string modelName)
        {
            // 修正: ^ を削除し、行頭以外のインデントもマッチするようにする
            string startPattern = $@"#\s*===\s*physics:\s*{Regex.Escape(modelName)}\s*\(auto-inserted";
            string endPattern = $@"#\s*===\s*/physics:\s*{Regex.Escape(modelName)}\s*===";
            
            var lines = pythonCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var newLines = new List<string>();
            bool isInsideBlock = false;
            bool removed = false;
            
            foreach (var line in lines)
            {
                if (!isInsideBlock && Regex.IsMatch(line, startPattern)) 
                { 
                    isInsideBlock = true; 
                    removed = true; 
                    continue; 
                }
                if (isInsideBlock && Regex.IsMatch(line, endPattern)) 
                { 
                    isInsideBlock = false; 
                    continue; 
                }
                if (!isInsideBlock) 
                {
                    newLines.Add(line);
                }
            }
            
            if (!removed) 
            {
                throw new InvalidOperationException($"Model '{modelName}' block not found.");
            }
            
            return string.Join(Environment.NewLine, newLines);
        }
    }
}