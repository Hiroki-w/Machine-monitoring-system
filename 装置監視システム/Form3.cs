using System;
using System.Drawing;
using System.Windows.Forms;

namespace 装置監視システム
{
	public partial class Form3 : Form
	{
		public Size ReturnSize;

		public Form3(Size sz)
		{
			InitializeComponent();
			ReturnSize = sz;
			textBox1.Text = sz.Height.ToString();
			textBox2.Text = sz.Width.ToString();
		}

		/// <summary>
		/// OKボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click(object sender, EventArgs e)
		{
			ReturnSize = new Size(int.Parse(textBox2.Text), int.Parse(textBox1.Text));
			this.Close();
		}

		/// <summary>
		/// キャンセルボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button2_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		/// <summary>
		/// 数字とBSキー以外は受け付けないようにする
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((e.KeyChar < '0' || '9' < e.KeyChar) && e.KeyChar != '\b')
				e.Handled = true;
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
	}
}
