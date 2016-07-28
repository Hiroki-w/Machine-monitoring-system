using System.Drawing;

namespace 装置監視システム
{
	/// <summary>
	/// 領域情報を格納するクラス
	/// </summary>
	public class Area
	{
		internal string Title { get; set; }

		internal Rectangle Rect { get; set; }

		internal Color LineColor { get; set; }

		internal Color FillColor { get; set; }

		/// <summary>
		/// コンストラクタで色が設定されたかを示すフラグ
		/// </summary>
		internal bool IsSetColor { get; }

		internal Area(string title, Rectangle rect, Color linecolor, Color fillcolor)
		{
			Title = title;
			Rect = rect;
			LineColor = linecolor;
			FillColor = fillcolor;
			IsSetColor = true;
		}

		internal Area(Rectangle rect)
		{
			Title = null;
			Rect = rect;
			IsSetColor = false;
		}

		internal Area(Area area)
		{
			Title = area.Title;
			Rect = area.Rect;
			LineColor = area.LineColor;
			FillColor = area.FillColor;
			IsSetColor = true;
		}
	}

	/// <summary>
	/// 基準色や基準色との比較を行う色データ
	/// </summary>
	public class RGB
	{
		internal int Red { get; set; }
		internal int Green { get; set; }
		internal int Blue { get; set; }

		internal RGB(int red, int green, int blue)
		{
			Red = red;
			Green = green;
			Blue = blue;
		}
	}
}
