using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PowerSimBuilder
{
    public partial class ComponentControl : UserControl
    {
        public event EventHandler SelectedChanged;
        public event EventHandler<PortEventArgs> PortClicked;
        
        private bool _selected;
        private readonly Dictionary<string, Rectangle> _ports = new Dictionary<string, Rectangle>();
        private Point _dragOffset;
        
        public ComponentDescriptor Descriptor { get; }
        public List<Connection> Connections { get; } = new List<Connection>();
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        
        public string InstanceName { get; set; } = "comp";
        
        public bool IsSelected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    Invalidate();
                    SelectedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }
        
        public ComponentControl(ComponentDescriptor descriptor)
        {
            Descriptor = descriptor;
            InitializeComponent();
            InitializeParameters();
            InitializePorts();
        }
        
        private void InitializeParameters()
        {
            foreach (var param in Descriptor.Parameters)
            {
                Parameters[param.Name] = param.DefaultValue;
            }
        }
        
        private void InitializePorts()
        {
            _ports.Add("Input", new Rectangle(0, Height / 2 - 5, 10, 10));
            _ports.Add("Output", new Rectangle(Width - 10, Height / 2 - 5, 10, 10));
        }
        
        public Point GetPortLocation(string portName)
        {
            if (_ports.TryGetValue(portName, out var rect))
            {
                return new Point(Left + rect.X + rect.Width / 2, Top + rect.Y + rect.Height / 2);
            }
            return Location;
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            
            // 背景
            e.Graphics.FillRectangle(Brushes.LightGray, ClientRectangle);
            
            // テキスト
            e.Graphics.DrawString(Descriptor.Name, Font, Brushes.Black, 10, 10);
            
            // 選択状態
            if (IsSelected)
            {
                using (var pen = new Pen(Color.Red, 2))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            }
            
            // ポート
            foreach (var port in _ports)
            {
                e.Graphics.FillEllipse(Brushes.Green, port.Value);
            }
        }
        
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            // ポートクリックをチェック
            foreach (var port in _ports)
            {
                if (port.Value.Contains(e.Location))
                {
                    PortClicked?.Invoke(this, new PortEventArgs(port.Key));
                    return;
                }
            }
            
            // コンポーネント選択
            IsSelected = true;
            _dragOffset = e.Location;
            BringToFront();
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            if (e.Button == MouseButtons.Left && IsSelected)
            {
                Location = new Point(
                    Left + e.X - _dragOffset.X,
                    Top + e.Y - _dragOffset.Y
                );
            }
        }
    }
    
    public class PortEventArgs : EventArgs
    {
        public string PortName { get; }
        
        public PortEventArgs(string portName)
        {
            PortName = portName;
        }
    }
}