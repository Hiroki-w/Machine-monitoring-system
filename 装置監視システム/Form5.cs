﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 装置監視システム
{
	public partial class Form5 : Form
	{
		public string newName;

		public Form5(string title)
		{
			InitializeComponent();
			textBox1.Text = newName = title;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			newName = textBox1.Text;
			this.Close();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
