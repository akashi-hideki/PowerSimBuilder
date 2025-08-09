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
// substantial portions of the Software. The above copyright notice and this permission notice shall
// be included in all copies or
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// FlexXmlSerializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class FlexXmlSerializer<T>
    {
        /// <summary>
        /// Deserializes from file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns></returns>
        public static List<T> DeserializeFromFile(string filePath)
        {
            string xml = File.ReadAllText(filePath);
            // スキーマ検証に成功したらデシリアライズ
            return DeserializeList(xml);
        }

        /// <summary>
        /// Deserializes the list.
        /// </summary>
        /// <param name="xml">The XML.</param>
        /// <returns></returns>
        public static List<T> DeserializeList(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<T>));

            using (StringReader stringReader = new StringReader(xml))
            {
                using (XmlReader reader = XmlReader.Create(stringReader))
                {
                    return (List<T>)serializer.Deserialize(reader);
                }
            }
        }

        /// <summary>
        /// Serializes the list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <returns></returns>
        public static string SerializeList(List<T> list)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<T>));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (XmlWriter writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true }))
                {
                    serializer.Serialize(writer, list);
                    writer.Flush();
                }

                // メモリストリームからバイト配列を取得し、文字列に変換する
                return System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="list">The list.</param>
        public static void SerializeToFile(string filePath, List<T> list)
        {
            string xml = SerializeList(list);
            File.WriteAllText(filePath, xml);

            // スキーマ定義ファイルを生成
            string schemaFilePath = Path.ChangeExtension(filePath, "xsd");
            GenerateSchema(schemaFilePath);
        }

        /// <summary>
        /// Generates the schema.
        /// </summary>
        /// <param name="schemaFilePath">The schema file path.</param>
        private static void GenerateSchema(string schemaFilePath)
        {
            XmlReflectionImporter importer = new XmlReflectionImporter();
            XmlTypeMapping mapping = importer.ImportTypeMapping(typeof(List<T>));
            XmlSchemas schemas = new XmlSchemas();
            XmlSchemaExporter exporter = new XmlSchemaExporter(schemas);
            exporter.ExportTypeMapping(mapping);

            using (StreamWriter writer = new StreamWriter(schemaFilePath))
            {
                foreach (XmlSchema schema in schemas)
                {
                    schema.Write(writer);
                }
            }
        }
    }
}