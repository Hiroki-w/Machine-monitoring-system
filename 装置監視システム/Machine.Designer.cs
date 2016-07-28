namespace 装置監視システム
{
	partial class Machine
	{
		/// <summary> 
		/// 必要なデザイナー変数です。
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// 使用中のリソースをすべてクリーンアップします。
		/// </summary>
		/// <param name="disposing">マネージ リソースを破棄する場合は true を指定し、その他の場合は false を指定します。</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region コンポーネント デザイナーで生成されたコード

		/// <summary> 
		/// デザイナー サポートに必要なメソッドです。このメソッドの内容を 
		/// コード エディターで変更しないでください。
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.表示サイズ変更ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.縮小ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem9 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem11 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem12 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.データフォルダを開くToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.ネットワークToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.接続ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.切断ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// contextMenuStrip1
			// 
			this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.表示サイズ変更ToolStripMenuItem,
            this.縮小ToolStripMenuItem,
            this.ネットワークToolStripMenuItem,
            this.toolStripMenuItem1,
            this.データフォルダを開くToolStripMenuItem});
			this.contextMenuStrip1.Name = "contextMenuStrip1";
			this.contextMenuStrip1.Size = new System.Drawing.Size(197, 120);
			// 
			// 表示サイズ変更ToolStripMenuItem
			// 
			this.表示サイズ変更ToolStripMenuItem.Name = "表示サイズ変更ToolStripMenuItem";
			this.表示サイズ変更ToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.表示サイズ変更ToolStripMenuItem.Text = "表示サイズ変更";
			this.表示サイズ変更ToolStripMenuItem.Click += new System.EventHandler(this.表示サイズ変更ToolStripMenuItem_Click);
			// 
			// 縮小ToolStripMenuItem
			// 
			this.縮小ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7,
            this.toolStripMenuItem8,
            this.toolStripMenuItem9,
            this.toolStripMenuItem11,
            this.toolStripMenuItem12});
			this.縮小ToolStripMenuItem.Name = "縮小ToolStripMenuItem";
			this.縮小ToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.縮小ToolStripMenuItem.Text = "文字サイズ拡大、縮小";
			// 
			// toolStripMenuItem2
			// 
			this.toolStripMenuItem2.Name = "toolStripMenuItem2";
			this.toolStripMenuItem2.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem2.Text = "+20%";
			this.toolStripMenuItem2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem2.Click += new System.EventHandler(this.toolStripMenuItem2_Click);
			// 
			// toolStripMenuItem3
			// 
			this.toolStripMenuItem3.Name = "toolStripMenuItem3";
			this.toolStripMenuItem3.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem3.Text = "+10%";
			this.toolStripMenuItem3.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem3.Click += new System.EventHandler(this.toolStripMenuItem3_Click);
			// 
			// toolStripMenuItem4
			// 
			this.toolStripMenuItem4.Name = "toolStripMenuItem4";
			this.toolStripMenuItem4.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem4.Text = "0%";
			this.toolStripMenuItem4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem4.Click += new System.EventHandler(this.toolStripMenuItem4_Click);
			// 
			// toolStripMenuItem5
			// 
			this.toolStripMenuItem5.Name = "toolStripMenuItem5";
			this.toolStripMenuItem5.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem5.Text = "-10%";
			this.toolStripMenuItem5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem5.Click += new System.EventHandler(this.toolStripMenuItem5_Click);
			// 
			// toolStripMenuItem6
			// 
			this.toolStripMenuItem6.Name = "toolStripMenuItem6";
			this.toolStripMenuItem6.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem6.Text = "-20%";
			this.toolStripMenuItem6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem6.Click += new System.EventHandler(this.toolStripMenuItem6_Click);
			// 
			// toolStripMenuItem7
			// 
			this.toolStripMenuItem7.Name = "toolStripMenuItem7";
			this.toolStripMenuItem7.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem7.Text = "-30%";
			this.toolStripMenuItem7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem7.Click += new System.EventHandler(this.toolStripMenuItem7_Click);
			// 
			// toolStripMenuItem8
			// 
			this.toolStripMenuItem8.Name = "toolStripMenuItem8";
			this.toolStripMenuItem8.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem8.Text = "-40%";
			this.toolStripMenuItem8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem8.Click += new System.EventHandler(this.toolStripMenuItem8_Click);
			// 
			// toolStripMenuItem9
			// 
			this.toolStripMenuItem9.Name = "toolStripMenuItem9";
			this.toolStripMenuItem9.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem9.Text = "-50%";
			this.toolStripMenuItem9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.toolStripMenuItem9.Click += new System.EventHandler(this.toolStripMenuItem9_Click);
			// 
			// toolStripMenuItem11
			// 
			this.toolStripMenuItem11.Name = "toolStripMenuItem11";
			this.toolStripMenuItem11.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem11.Text = "-60%";
			this.toolStripMenuItem11.Click += new System.EventHandler(this.toolStripMenuItem11_Click);
			// 
			// toolStripMenuItem12
			// 
			this.toolStripMenuItem12.Name = "toolStripMenuItem12";
			this.toolStripMenuItem12.Size = new System.Drawing.Size(112, 22);
			this.toolStripMenuItem12.Text = "-70%";
			this.toolStripMenuItem12.Click += new System.EventHandler(this.toolStripMenuItem12_Click);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(193, 6);
			// 
			// データフォルダを開くToolStripMenuItem
			// 
			this.データフォルダを開くToolStripMenuItem.Name = "データフォルダを開くToolStripMenuItem";
			this.データフォルダを開くToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.データフォルダを開くToolStripMenuItem.Text = "データフォルダを開く";
			this.データフォルダを開くToolStripMenuItem.Click += new System.EventHandler(this.データフォルダを開くToolStripMenuItem_Click);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(0, 19);
			this.label1.TabIndex = 1;
			this.label1.Visible = false;
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(0, 36);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(0, 19);
			this.label2.TabIndex = 2;
			this.label2.Visible = false;
			// 
			// ネットワークToolStripMenuItem
			// 
			this.ネットワークToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.接続ToolStripMenuItem,
            this.切断ToolStripMenuItem});
			this.ネットワークToolStripMenuItem.Name = "ネットワークToolStripMenuItem";
			this.ネットワークToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
			this.ネットワークToolStripMenuItem.Text = "ネットワーク";
			this.ネットワークToolStripMenuItem.DropDownOpened += new System.EventHandler(this.ネットワークToolStripMenuItem_DropDownOpened);
			// 
			// 接続ToolStripMenuItem
			// 
			this.接続ToolStripMenuItem.Name = "接続ToolStripMenuItem";
			this.接続ToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.接続ToolStripMenuItem.Text = "接続";
			this.接続ToolStripMenuItem.Click += new System.EventHandler(this.接続ToolStripMenuItem_Click);
			// 
			// 切断ToolStripMenuItem
			// 
			this.切断ToolStripMenuItem.Name = "切断ToolStripMenuItem";
			this.切断ToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.切断ToolStripMenuItem.Text = "切断";
			this.切断ToolStripMenuItem.Click += new System.EventHandler(this.切断ToolStripMenuItem_Click);
			// 
			// Machine
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 19F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.ContextMenuStrip = this.contextMenuStrip1;
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.DoubleBuffered = true;
			this.Font = new System.Drawing.Font("ＭＳ ゴシック", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.Margin = new System.Windows.Forms.Padding(1);
			this.Name = "Machine";
			this.Size = new System.Drawing.Size(260, 95);
			this.Load += new System.EventHandler(this.Machine_Load);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.Machine_Paint);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Machine_MouseDown);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Machine_MouseMove);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Machine_MouseUp);
			this.contextMenuStrip1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
		private System.Windows.Forms.ToolStripMenuItem 縮小ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem9;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem データフォルダを開くToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem11;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem12;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ToolStripMenuItem 表示サイズ変更ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem ネットワークToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 接続ToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem 切断ToolStripMenuItem;
	}
}
