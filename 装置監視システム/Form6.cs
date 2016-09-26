using System;
using System.Windows.Forms;

namespace 装置監視システム
{
	public partial class Form6 : Form
	{
		public Form6()
		{
			InitializeComponent();
			this.AcceptButton = this.button1;

		}

		/// <summary>
		/// OKボタンが押されたら
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click(object sender, EventArgs e)
		{
			if (textBox1.Text == "qaz")
				this.DialogResult = DialogResult.OK;
			else
			{
				MessageBox.Show("パスワードが違います。", "保持点灯の消灯", MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.DialogResult = DialogResult.Cancel;
			}
		}

		/// <summary>
		/// キャンセルボタンが押されたら
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button2_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}
	}
}
