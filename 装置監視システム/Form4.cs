using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace 装置監視システム
{
	public partial class Form4 : Form
	{
		internal Area area;
		private List<RGB> colorList = new List<RGB>();

		public Form4(Area ar)
		{
			InitializeComponent();
			area = ar;
			if (ar.IsSetColor == false)
			{
				area.FillColor = SystemColors.Control;
				area.LineColor = Color.Black;
			}
			if(area.Title == "")
				area.Title = "タイトル";
			textBox1.Text = area.Title;
		}

		private void Form4_Load(object sender, EventArgs e)
		{
			// カラーパレットの基本色を入れる
			colorList.Add(new RGB(128, 255, 0));
			colorList.Add(new RGB(255, 128, 128));
			colorList.Add(new RGB(255, 255, 128));
			colorList.Add(new RGB(128, 255, 128));
			colorList.Add(new RGB(0, 255, 128));
			colorList.Add(new RGB(128, 255, 255));
			colorList.Add(new RGB(0, 128, 255));
			colorList.Add(new RGB(255, 128, 192));
			colorList.Add(new RGB(255, 128, 255));
			colorList.Add(new RGB(255, 0, 0));
			colorList.Add(new RGB(255, 255, 0));
			colorList.Add(new RGB(0, 255, 64));
			colorList.Add(new RGB(0, 255, 255));
			colorList.Add(new RGB(0, 128, 192));
			colorList.Add(new RGB(128, 128, 192));
			colorList.Add(new RGB(255, 0, 255));
			colorList.Add(new RGB(128, 64, 64));
			colorList.Add(new RGB(255, 128, 64));
			colorList.Add(new RGB(0, 255, 0));
			colorList.Add(new RGB(0, 128, 128));
			colorList.Add(new RGB(0, 64, 128));
			colorList.Add(new RGB(128, 128, 255));
			colorList.Add(new RGB(128, 0, 64));
			colorList.Add(new RGB(255, 0, 128));
			colorList.Add(new RGB(128, 0, 0));
			colorList.Add(new RGB(255, 128, 0));
			colorList.Add(new RGB(0, 128, 0));
			colorList.Add(new RGB(0, 128, 64));
			colorList.Add(new RGB(0, 0, 255));
			colorList.Add(new RGB(0, 0, 160));
			colorList.Add(new RGB(128, 0, 128));
			colorList.Add(new RGB(128, 0, 255));
			colorList.Add(new RGB(64, 0, 0));
			colorList.Add(new RGB(128, 64, 0));
			colorList.Add(new RGB(0, 64, 0));
			colorList.Add(new RGB(0, 64, 64));
			colorList.Add(new RGB(0, 0, 128));
			colorList.Add(new RGB(0, 0, 64));
			colorList.Add(new RGB(64, 0, 64));
			colorList.Add(new RGB(64, 0, 128));
			colorList.Add(new RGB(0, 0, 0));
			colorList.Add(new RGB(128, 128, 0));
			colorList.Add(new RGB(128, 128, 64));
			colorList.Add(new RGB(128, 128, 128));
			colorList.Add(new RGB(64, 128, 128));
			colorList.Add(new RGB(192, 192, 192));
			colorList.Add(new RGB(64, 0, 64));
			colorList.Add(new RGB(255, 255, 255));

			textBox2.Text = area.Rect.X.ToString();
			textBox3.Text = area.Rect.Y.ToString();
			textBox4.Text = area.Rect.Width.ToString();
			textBox5.Text = area.Rect.Height.ToString();

			// Graphicsオブジェクトの作成
			using (Graphics g = panel1.CreateGraphics())
			using (SolidBrush fillbrush = new SolidBrush(area.FillColor))
			using (Pen linePen = new Pen(area.LineColor, 2))
			using (SolidBrush titlebrush = new SolidBrush(Color.Black))
			{
				g.FillRectangle(fillbrush, 20, 20, 150, 140);
				g.DrawRectangle(linePen, 20, 20, 150, 140);
				g.DrawString(area.Title, this.Font, titlebrush, new Point(15, 10));
			}
		}

		/// <summary>
		/// 線の色設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click(object sender, EventArgs e)
		{
			using (ColorDialog cd = new ColorDialog())
			{
				RGB rgb = new RGB(area.LineColor.R, area.LineColor.G, area.LineColor.B);
				// 標準色でなければカスタムカラーの領域へ表示
				if (colorList.Any(c => c.Red == rgb.Red && c.Green == rgb.Green && c.Blue == rgb.Blue) == false)
					cd.CustomColors = new int[] { area.LineColor.R << 16 | area.LineColor.G << 8 | area.LineColor.B };
				cd.Color = area.LineColor;
				if (cd.ShowDialog() == DialogResult.OK)
					area.LineColor = cd.Color;
				panel1.Refresh();
			}
		}

		/// <summary>
		/// 背景色の設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button2_Click(object sender, EventArgs e)
		{
			using (ColorDialog cd = new ColorDialog())
			{
				RGB rgb = new RGB(area.FillColor.R, area.FillColor.G, area.FillColor.B);
				// 標準色でなければカスタムカラーの領域へ表示
				if (colorList.Any(c => c.Red == rgb.Red && c.Green == rgb.Green && c.Blue == rgb.Blue) == false)
					cd.CustomColors = new int[] { area.FillColor.R << 16 | area.FillColor.G << 8 | area.FillColor.B };
				cd.Color = area.FillColor;
				if (cd.ShowDialog() == DialogResult.OK)
					area.FillColor = cd.Color;
				panel1.Refresh();
			}
		}

		/// <summary>
		/// OKボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button3_Click(object sender, EventArgs e)
		{
			area.Title = textBox1.Text;
			Rectangle r = new Rectangle();
			r.X = int.Parse(textBox2.Text);
			r.Y = int.Parse(textBox3.Text);
			r.Width = int.Parse(textBox4.Text);
			r.Height = int.Parse(textBox5.Text);
			area.Rect = r;

			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		/// <summary>
		/// キャンセルボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button4_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		/// <summary>
		/// 数字とBSキー以外は受け付けないようにする
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void textBox2_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
				e.Handled = true;
		}

		/// <summary>
		/// 数字とBSキー以外は受け付けないようにする
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void textBox3_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
				e.Handled = true;
		}

		/// <summary>
		/// 数字とBSキー以外は受け付けないようにする
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void textBox4_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
				e.Handled = true;
		}

		/// <summary>
		/// 数字とBSキー以外は受け付けないようにする
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void textBox5_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
				e.Handled = true;
		}

		private void panel1_Paint(object sender, PaintEventArgs e)
		{
			// Graphicsオブジェクトの作成
			using (Graphics g = e.Graphics)
			using (SolidBrush fillbrush = new SolidBrush(area.FillColor))
			using (Pen linePen = new Pen(area.LineColor, 2))
			using (SolidBrush titlebrush = new SolidBrush(Color.Black))
			{
				g.FillRectangle(fillbrush, 20, 20, 145, 135);
				g.DrawRectangle(linePen, 20, 20, 145, 135);
				e.Graphics.DrawString(area.Title, this.Font, titlebrush, new Point(15, 10));
			}
		}

		private void textBox1_TextChanged(object sender, EventArgs e)
		{
			area.Title = textBox1.Text;
			panel1.Refresh();
		}
	}
}
