using System;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PowerSimBuilder
{
    /// <summary>
    /// Point構造体のJSONコンバータ
    /// 注意事項: Point構造体をJSONオブジェクト形式でシリアライズする
    /// 作成日: 2025-08-10
    /// </summary>
    public class PointJsonConverter : JsonConverter<Point>
    {
        /// <summary>
        /// JSONからPointを読み込む
        /// 作成日: 2025-08-10
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
        /// 作成日: 2025-08-10
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