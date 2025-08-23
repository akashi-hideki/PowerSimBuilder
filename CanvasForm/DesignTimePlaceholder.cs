using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    // "Code" を引数として指定
    [DesignerCategory()]
    public class DesignTimePlaceholder : Control
    {
        private readonly string _label;

        public DesignTimePlaceholder(string label)
        {
            _label = label;
            SetStyle(ControlStyles.ResizeRedraw, true);
            BackColor = SystemColors.ControlLight;
            BorderStyle = BorderStyle.FixedSingle;
            MinimumSize = new Size(100, 100);
        }

        public BorderStyle BorderStyle { get; }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.Clear(BackColor);

            using (var pen = new Pen(SystemColors.ControlDark, 1))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }

            TextRenderer.DrawText(e.Graphics, _label, Font,
                new Rectangle(10, 10, Width - 20, Height - 20),
                ForeColor, TextFormatFlags.WordBreak);
        }
    }
}