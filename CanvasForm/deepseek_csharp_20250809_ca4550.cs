using System;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Text.Json;
using Python.Runtime;

namespace PowerSimBuilder
{
    public partial class MainForm : Form
    {
        private readonly CanvasManager _canvasManager;
        private PythonCodeGenerator _codeGenerator = new PythonCodeGenerator();
        
        public MainForm()
        {
            InitializeComponent();
            
            // イベントハンドラの接続
            this.addComponentToolStripButton.Click += AddComponentToolStripButton_Click;
            
            // CanvasManager の初期化
            _canvasManager = new CanvasManager(designCanvas1, componentPalette1);
            
            // イベントのサブスクライブ
            _canvasManager.CanvasModified += CanvasManager_CanvasModified;
            _canvasManager.SelectionChanged += CanvasManager_SelectionChanged;
        }
        
        private void AddComponentToolStripButton_Click(object sender, EventArgs e)
        {
            using (var form = new AddComponentForm())
            {
                if (form.ShowDialog() == DialogResult.OK && form.NewComponent != null)
                {
                    componentPalette1.AddComponent(form.NewComponent);
                }
            }
        }
        
        // ... 既存のコード (変更なし) ...
    }
    
    // Point型用カスタムコンバーター
    public class PointJsonConverter : JsonConverter<Point>
    {
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

        public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }
}