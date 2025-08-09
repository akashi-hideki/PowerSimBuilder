/// <summary>
/// Generated class source code
/// ClassName: Common
/// Generated on: 2025-08-09
/// 生成されたクラスのソースコード
/// クラス名: Common
/// 生成日: 2025-08-09
/// </summary>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Drawing;
namespace PowerSimBuilder
{
	/// <summary>
	/// ClassName: tableParam
	/// </summary>
	public class tableParam
	{
		/// Property: KeyName
		public string KeyName { get; set; } = string.Empty;
		/// Property: TitleName
		public string TitleName { get; set; } = string.Empty;
		/// Property: Width
		public int Width { get; set; } = 15;
		/// Property: ComboxStringName
		public string ComboxStringName { get; set; } = string.Empty;
		/// Property: ReadOnly
		public bool ReadOnly { get; set; } = false;
	}
	
	public class TabAttr
	{
		public string TabName { get; set; }
		public int ColumnCont { get; set; }
		public TabAttr()
		{
			TabName = string.Empty;
			ColumnCont = 1;
		}
	}
	
	public class TabNameType
	{
		public string TabName { get; set; }
		public TabNameType()
		{
			TabName = "anonymous_tab";
		}
	}
	
	public enum EnumDataType
	{
		None,
		Int,
		String,
		Bool,
		Double,
		Long,
	}
	
	// INSERTION MARKER
}
