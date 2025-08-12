using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// Point構造体のJSONコンバータ
    /// 作成日: 2025-08-12
    /// 注意点: System.Drawing.Pointのシリアライズ/デシリアライズをカスタマイズ
    /// </summary>
    public class PointJsonConverter : JsonConverter<Point>
    {
        /// <summary>
        /// JSONからPointを読み込む
        /// 処理内容:
        ///   - JSONオブジェクトを解析しX,Y座標を抽出
        /// 注意点:
        ///   - 厳密なJSON構造を要求
        /// </summary>
        public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            int x = 0, y = 0;
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Point(x, y);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var propName = reader.GetString();
                reader.Read();

                switch (propName?.ToLower())
                {
                    case "x":
                        x = reader.GetInt32();
                        break;
                    case "y":
                        y = reader.GetInt32();
                        break;
                }
            }
            throw new JsonException();
        }

        /// <summary>
        /// PointをJSONに書き込む
        /// 処理内容:
        ///   - Pointを{X,Y}形式のJSONオブジェクトに変換
        /// 注意点:
        ///   - プロパティ名は小文字で出力
        /// </summary>
        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }
}