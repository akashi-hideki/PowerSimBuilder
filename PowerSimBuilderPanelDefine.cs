/// <summary>
/// Generated class source code
/// Generated on: 2025-08-09
/// 生成されたクラスのソースコード
/// 生成日: 2025-08-09
/// </summary>

using System.Collections.Generic;
using System.ComponentModel;
namespace PowerSimBuilder
{
    /// <summary>
    /// ClassName: SimKind
    /// クラス名: SimKind
    /// </summary>
    public class SimKind : INotifyPropertyChanged
    {
        private string _name;   //	Name;
        private string _descript;   //	Descript;
        /// Property: Name
        /// プロパティ: Name
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
        /// Property: Descript
        /// プロパティ: Descript
        public string Descript
        {
            get => _descript;
            set
            {
                if (_descript == value) return;
                _descript = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Descript)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
    /// <summary>
    /// ClassName: Parts
    /// クラス名: Parts
    /// </summary>
    public class Parts : INotifyPropertyChanged
    {
        private string _name;   //	Name;
        private string _simname;    //	SimName;
        private string _descript;   //	Descript;
        /// Property: Name
        /// プロパティ: Name
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }
        /// Property: SimName
        /// プロパティ: SimName
        public string SimName
        {
            get => _simname;
            set
            {
                if (_simname == value) return;
                _simname = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SimName)));
            }
        }
        /// Property: Descript
        /// プロパティ: Descript
        public string Descript
        {
            get => _descript;
            set
            {
                if (_descript == value) return;
                _descript = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Descript)));
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
    public class tableClassPowerSimBuilder
    {
        /// <summary>
        /// The table name.: SimKind
        /// テーブル名: SimKind
        /// </summary>
        public Dictionary<string, tableParam> SimKindSetting = new Dictionary<string, tableParam>
        {
            { "Name", new tableParam { KeyName = "Name", TitleName = I18n.T("PanelDefine.ColControlBoxName"), Width = 15, ComboxStringName = "", ReadOnly = false } },
            { "Descript", new tableParam { KeyName = "Descript", TitleName = I18n.T("PanelDefine.ColDescription"), Width = 100, ComboxStringName = "", ReadOnly = false } },
        };
        /// <summary>
        /// The table name.: Parts
        /// テーブル名: Parts
        /// </summary>
        public Dictionary<string, tableParam> PartsSetting = new Dictionary<string, tableParam>
        {
            { "Name", new tableParam { KeyName = "Name", TitleName = I18n.T("PanelDefine.ColPartName"), Width = 15, ComboxStringName = "", ReadOnly = false } },
            { "SimName", new tableParam { KeyName = "SimName", TitleName = I18n.T("PanelDefine.ColControlBoxName"), Width = 15, ComboxStringName = "", ReadOnly = false } },
            { "Descript", new tableParam { KeyName = "Descript", TitleName = I18n.T("PanelDefine.ColDescription"), Width = 100, ComboxStringName = "", ReadOnly = false } },
        };
    }
}
