using System;
using System.Windows.Forms;

namespace 装置監視システム
{
	public partial class Form2 : Form
	{
		public string SetShortDate { get { return setShortDate; } }
		public string SetLongDate { get { return setLongDate; } }
		public DateTime SetDateTime { get { return dateTime; } }

		private string setShortDate;
		private string setLongDate;
		private DateTime dateTime;

		public Form2()
		{
			InitializeComponent();
			setShortDate = DateTime.Now.ToShortDateString();
			setLongDate = DateTime.Now.ToLongDateString();
			dateTime = DateTime.Now;
		}

		private void monthCalendar1_DateSelected(object sender, DateRangeEventArgs e)
		{
			setShortDate = monthCalendar1.SelectionStart.ToShortDateString();
			setLongDate = monthCalendar1.SelectionStart.ToLongDateString();
			dateTime = monthCalendar1.SelectionStart;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
		}
	}
}
