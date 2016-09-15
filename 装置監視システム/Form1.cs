using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Permissions;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Threading.Tasks;
using System.Threading;

namespace 装置監視システム
{

	/// <summary>
	///  アラーム情報の構造体
	/// </summary>
	internal struct alarmInformation
	{
		/// <summary>
		/// エラーの発生時間
		/// </summary>
		internal int startPos;
		/// <summary>
		/// エラーの終了時間
		/// </summary>
		internal int endPos;
		/// <summary>
		/// エラー情報
		/// </summary>
		internal string errMessage;
	}

	public partial class Form1 : Form
	{
		#region 変数の宣言++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		/// <summary>
		/// 起動時にコントロールのイベント動作を抑制するフラグ
		/// </summary>
		private bool bootFlag = true;

		private bool noCheck = false;

		/// <summary>
		/// 加工機情報で選択されている部屋 0=エリア1 1=エリア2
		/// </summary>
		private int selectRoom = 0;

		/// <summary>
		/// 装置一覧で選択されているインデックス 0～
		/// </summary>
		private int selectMachine = -1;

		/// <summary>
		/// 稼動情報で選択されているインデックス 0～
		/// </summary>
		private int selectDay = -1;

		/// <summary>
		/// アラーム保存時に使用可能なファイル形式の文字列
		/// </summary>
		private string saveType;

		// 装置個別情報表示のコントロールをループで回すための代替変数
		private PictureBox[] P2Pictur = new PictureBox[10];
		private Bitmap[] operationCanvas = new Bitmap[10];
		private Button[] P2Button = new Button[10];

		private TextBox[] OperationText = new TextBox[8];

		// 稼働マップ色設定などに使用する変数
		private AutoResetEvent[] clickEvent = new AutoResetEvent[2];
		private Point clickPos = new Point();
		private bool isWaitClick = false;
		private bool isClick = false;
		private bool isCancel = false;
		private Rectangle rect;
		private int eventPos1;
		private int eventPos2;
		internal int selectArraIndex = -1;
		internal List<Area> areaRoom1 = new List<Area>();
		internal List<Area> areaRoom2 = new List<Area>();

		/// <summary>
		/// *.almで読み込んだファイルリスト
		/// </summary>
		internal List<string> AlarmFile;

		internal List<string> OperationFile;

		/// <summary>
		/// 装置個別情報のエラー情報のバルーンの表示更新を抑制するフラグ
		/// </summary>
		private bool[] isViewToolChip = new bool[10] { false, false, false, false, false, false, false, false, false, false };

		/// <summary>
		/// パネルを間接的に処理するための配列
		/// </summary>
		private Panel[] panel = new Panel[4];
		/// <summary>
		/// ボタンを間接的に処理するための配列
		/// </summary>
		private Button[] button = new Button[4];
		/// <summary>
		/// 現在表示されているパネル
		/// </summary>
		internal int viewPanel;
		/// <summary>
		/// 装置情報が入るカスタムコントロールMachineクラスのインスタンス
		/// </summary>
		private List<List<Machine>> machineInformation = new List<List<Machine>>();

		/// <summary>
		/// 装置別稼動推移を表示させるコントロールの配列
		/// </summary>
		private PictureBox[] P3Picture;

		/// <summary>
		/// 装置別稼動推移を表示させるビットマップ
		/// </summary>
		private Bitmap[] P3Bitmap;

		/// <summary>
		/// 装置個別情報のエラー情報のバルーンの表示更新を抑制するフラグ
		/// </summary>
		private bool[] isP3ToolChip;

		/// <summary>
		/// 装置個別情報のエラー情報を入れる構造体の配列
		/// </summary>
		internal List<List<alarmInformation>> P2err = new List<List<alarmInformation>>();

		/// <summary>
		/// 装置別稼動推移のエラー情報を入れる構造体の配列
		/// </summary>
		internal List<List<alarmInformation>> P3err = new List<List<alarmInformation>>();

		/***** スレッドタイマ *****/
		/// <summary>
		/// 設定が変更された時、保存を促す点灯用タイマ
		/// </summary>
		System.Timers.Timer timer1;

		/// <summary>
		/// 定期的に稼動状況を更新するタイマ
		/// </summary>
		System.Timers.Timer timer2;

		/// <summary>
		/// フォームに時刻を表示するタイマ
		/// </summary>
		System.Timers.Timer timer3;

		/// <summary>
		/// 日付が変わるタイミングを監視するタイマ
		/// </summary>
		System.Timers.Timer timer4;

		/// <summary>
		/// 日付管理
		/// </summary>
		private int oldDay; 

		#endregion 変数の宣言 ----------------------------------------------------------------------------------------------------

		/// <summary>
		/// マウスでフォームが移動できないようにする
		/// </summary>
		/// <param name="m">Window Message</param>
		[SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.UnmanagedCode)]
		protected override void WndProc(ref Message m)
		{
			// 0x0112 = WM_SYSCOMMAND 0xF010 = SC_MOVE
			if (m.Msg == 0x0112 &&　(m.WParam.ToInt64() & 0xFFF0L) == 0xF010L)
			{
				m.Result = IntPtr.Zero;
				return;
			}
			base.WndProc(ref m);
		}

		/// <summary>
		/// フォームのコンストラクタ
		/// </summary>
		public Form1()
		{
			InitializeComponent();

			// これだけは表示前に処理しておく
			panel[0] = panel1;
			panel[1] = panel2;
			panel[2] = panel3;
			panel[3] = panel4;
			// パネルの初期設定
			foreach (var p in panel)
			{
				p.Location = new Point(4, 1048);
				p.Size = new Size(1912, 1000);
			}
			/***** エラー情報用Listの作成(10個なのは加工機情報で推移を表示してるのが10個だから) *****/
			for (int i = 0; i < 10; i++)
			{
				P2err.Add(new List<alarmInformation>());
			}
			// 起動時にExcelのインストール状況をチェックしてアラーム保存時に指定できるファイル形式を決める
			if (Type.GetTypeFromProgID("Excel.Application") != null)
				saveType = "CSV(カンマ区切り)(*.csv)|*.csv|Excelブック(*.xlsx)|*.xlsx";
			else
				saveType = "CSV(カンマ区切り)(*.csv)|*.csv";
		}

		/// <summary>
		/// フォームが初めて表示される直前のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void Form1_Load(object sender, EventArgs e)
		{
			try
			{
				// フォームを最大化する
				this.WindowState = FormWindowState.Maximized;
				// 設定を削除
				//Properties.Settings.Default.Reset();
				// 前バージョンからのUpgradeを実行していないときは、Upgradeを実施する
				if (Properties.Settings.Default.IsUpgrade == false)
				{
					// Upgradeを実行する
					Properties.Settings.Default.Upgrade();
					// 「Upgradeを実行した」という情報を設定する
					Properties.Settings.Default.IsUpgrade = true;
					// 現行バージョンの設定を保存する
					Properties.Settings.Default.Save();
				}

				/***** ループ処理を行うので配列に入れておく *****/
				
				button[0] = button1;
				button[1] = button2;
				button[2] = button3;
				button[3] = button4;

				P2Pictur[0] = pictureBox1;
				P2Pictur[1] = pictureBox2;
				P2Pictur[2] = pictureBox3;
				P2Pictur[3] = pictureBox4;
				P2Pictur[4] = pictureBox5;
				P2Pictur[5] = pictureBox6;
				P2Pictur[6] = pictureBox7;
				P2Pictur[7] = pictureBox8;
				P2Pictur[8] = pictureBox9;
				P2Pictur[9] = pictureBox10;

				P2Button[0] = button15;
				P2Button[1] = button16;
				P2Button[2] = button17;
				P2Button[3] = button18;
				P2Button[4] = button19;
				P2Button[5] = button20;
				P2Button[6] = button21;
				P2Button[7] = button22;
				P2Button[8] = button23;
				P2Button[9] = button24;

				OperationText[0] = textBox3;
				OperationText[1] = textBox4;
				OperationText[2] = textBox5;
				OperationText[3] = textBox6;
				OperationText[4] = textBox7;
				OperationText[5] = textBox8;
				OperationText[6] = textBox12;
				OperationText[7] = textBox13;

				// chart用パネルの位置とサイズ
				panel7.Location = new Point(8, 1000);
				panel7.Size = new Size(1896, 704);

				/***** スレッドタイマの初期化 *****/
				// 機械情報が変更された時、保存を促す表示を行うタイマ
				timer1 = new System.Timers.Timer(500);
				timer1.AutoReset = true;
				timer1.Elapsed += new System.Timers.ElapsedEventHandler(timer1_tick);

				// 稼働状況が当日表示の場合データを更新するタイマ
				timer2 = new System.Timers.Timer();
				timer2.AutoReset = true;
				timer2.Elapsed += new System.Timers.ElapsedEventHandler(timer2_tick);

				// 時刻を表示するタイマ
				timer3 = new System.Timers.Timer();
				timer3.AutoReset = true;
				timer3.Elapsed += new System.Timers.ElapsedEventHandler(timer3_tick);

				// 日付が変わるタイミングを監視するタイマ
				timer4 = new System.Timers.Timer();
				timer4.AutoReset = true;
				// 日にちが変わる時間を計算して次のタイマイベントのタイミングとする
				timer4.Interval = 86400000 - (DateTime.Now.Hour * 3600000 + DateTime.Now.Minute * 60000 + DateTime.Now.Second * 1000 + DateTime.Now.Millisecond);
				timer4.Elapsed += new System.Timers.ElapsedEventHandler(timer4_tick);
				timer4.Start();
				oldDay = DateTime.Now.Day;

				// 装置のインスタンスが生成される前に処理する
				checkBox2.Checked = Properties.Settings.Default.IsListData;
				areaName();

				/***** アラームリストを読み込みリストに追加する *****/
				Column28.Items.Add("");
				AlarmFile = new List<string>(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.alm").Select(c => c.Name));
				foreach (var file in AlarmFile)
				{
					comboBox1.Items.Add(file);
					Column28.Items.Add(file);
				}

				/***** 操作リストを読み込みリストに追加する *****/
				Column29.Items.Add("");
				OperationFile = new List<string>(new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).GetFiles("*.ope").Select(c => c.Name));
				foreach (var file in OperationFile)
				{
					comboBox2.Items.Add(file);
					Column29.Items.Add(file);
				}

				/*****　機械の情報読み込み *****/
				// とりあえず配列はクリアする
				machineInformation.Clear();
				List<Machine> room1 = new List<Machine>();
				List<Machine> room2 = new List<Machine>();
				string SettingPath = AppDomain.CurrentDomain.BaseDirectory + "MachineInformation.csv";
				// ファイルが存在していればデータの読み出し
				if (File.Exists(SettingPath))
				{
					List<string> machineData = new List<string>();
					using (FileStream fs = new FileStream(SettingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// ストリームの末尾まで繰り返す
						while (!sr.EndOfStream)
						{
							machineData.Add(sr.ReadLine());
						}
					}

					foreach (var str in machineData)
					{
						// 情報を入れる
						await Task.Run(() =>
						{
							Machine mc = new Machine(this, str);
							if (mc.RoomNumber == 0)
								room1.Add(mc);
							else
								room2.Add(mc);
						});
					}
					machineInformation.Add(room1);
					machineInformation.Add(room2);
					viewMachineInformation();

					// グループボックスの幅を設定する(Dockプロパティの「Fill」ではない方のコントロールのサイズを変える)
					panel8.Width = Properties.Settings.Default.Room2Width;
					// エリア1のグループボックスにコントロールを追加
					foreach (var machine in machineInformation[0])
						this.panel8.Controls.Add(machine);
					// エリア2のグループボックスにコントロールを追加
					foreach (var machine in machineInformation[1])
						this.panel9.Controls.Add(machine);

					// 稼働状況のパネルを表示する
					SetPanel(0);
				}
				// ファイルが存在してなければメッセージ表示
				else
				{
					machineInformation.Add(room1);
					machineInformation.Add(room2);
					MessageBox.Show("装置情報がありません。\r\n装置情報を設定してください。", "装置情報読み込み", MessageBoxButtons.OK, MessageBoxIcon.Error);
					SetPanel(1);
				}

				/***** 稼働マップエリア情報の読み込み *****/
				areaRoom1.Clear();
				areaRoom2.Clear();
				SettingPath = AppDomain.CurrentDomain.BaseDirectory + "MachineArea.csv";
				// ファイルが存在していればデータの読み出し
				if (File.Exists(SettingPath))
				{
					List<string[]> area = new List<string[]>();
					using (FileStream fs = new FileStream(SettingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// ストリームの末尾まで繰り返す
						while (!sr.EndOfStream)
						{
							area.Add(sr.ReadLine().Split(','));
						}
					}
					foreach (var a in area)
					{
						Area ar = new Area(a[1],
							new Rectangle(int.Parse(a[2]), int.Parse(a[3]), int.Parse(a[4]), int.Parse(a[5])),
							Color.FromArgb(int.Parse(a[6]), int.Parse(a[7]), int.Parse(a[8])),
							Color.FromArgb(int.Parse(a[9]), int.Parse(a[10]), int.Parse(a[11])));
						if (a[0] == "1")
							areaRoom1.Add(ar);
						else
							areaRoom2.Add(ar);
					}
					panel8.Refresh();
					panel9.Refresh();
				}

				/***** コントロールの初期化 *****/
				radioButton1.Checked = true;
				radioButton3.Checked = true;
				button13.Text = "選択日\r\n↓\r\n前日";
				button14.Text = "選択日\r\n↓\r\n後日";
				button25.Text = "表示月\r\nアラーム集計";
				button26.Text = "表示日\r\nアラーム集計";

				clickEvent[0] = new AutoResetEvent(false);
				clickEvent[1] = new AutoResetEvent(false);

				label156.Text = DateTime.Now.ToString("yyyy年MM月dd日 (ddd) H時mm分");
				timer3.Interval = 60100 - DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
				timer3.Start();

				/***** リスト送信の表示設定 *****/
				groupBox1.Enabled = checkBox2.Checked;
				groupBox2.Enabled = checkBox2.Checked;

				// 初期化が終了した事を示すフラグをクリア
				bootFlag = false;

			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
				// ここでのエラーは致命的なのでメッセージを出力して終了
				MessageBox.Show("エラーが発生しました。\r\nアプリケーションを終了します。", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.Close();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyData"></param>
		/// <returns></returns>
		[UIPermission(SecurityAction.Demand, Window = UIPermissionWindow.AllWindows)]
		protected override bool ProcessDialogKey(Keys keyData)
		{
			if (isWaitClick == true && keyData == Keys.Escape)
			{
				isCancel = true;
				clickEvent[0].Set();
				return true;
			}
			return base.ProcessDialogKey(keyData);
		}

		/// <summary>
		/// フォームが閉じる時のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Form1_FormClosed(object sender, FormClosedEventArgs e)
		{
			try
			{
				// カスタムコントロールの終了処理を行う
				foreach (var mc in machineInformation)
				{
					foreach (var m in mc)
					{
						m.Exit();
					}
				}

				// タイマオブジェクトのリソース開放
				timer1.Stop();
				timer2.Stop();
				timer3.Stop();
				timer4.Stop();
				timer1.Dispose();
				timer2.Dispose();
				timer3.Dispose();
				timer4.Dispose();
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		#region 全体を統括するメソッド ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// ボタンに応じたパネルを表示させる
		/// </summary>
		/// <param name="targetNumber">ターゲットとなるパネルの要素番号</param>
		private void SetPanel(int targetNumber)
		{
			try
			{
				/***** 最初に必ず行う処理 *****/
				foreach (var bt in button)
				{
					bt.Enabled = false;
				}
				button11.Enabled = false;
				timer2.Stop();
				// パネル切り替え動作 false=しない true=する
				bool isCancel = false;
				// 表示するパネル
				int panelIndex = 0;
				/***** パネルによって表示初期に処理すべき内容 *****/
				switch (targetNumber)
				{
					case 0:
						panel6.Visible = false;
						label141.ForeColor = Properties.Settings.Default.Panel1GreenForeColor;
						label141.BackColor = Properties.Settings.Default.Panel1GreenBackColor;
						label141.Text = Properties.Settings.Default.Panel1GreenName;
						label142.ForeColor = Properties.Settings.Default.Panel1YellowForeColor;
						label142.BackColor = Properties.Settings.Default.Panel1YellowBackColor;
						label142.Text = Properties.Settings.Default.Panel1YellowName;
						label143.ForeColor = Properties.Settings.Default.Panel1RedForeColor;
						label143.BackColor = Properties.Settings.Default.Panel1RedBackColor;
						label143.Text = Properties.Settings.Default.Panel1RedName;
						label144.ForeColor = Properties.Settings.Default.Panel1EraseForeColor;
						label144.BackColor = Properties.Settings.Default.Panel1EraseBackColor;
						label144.Text = Properties.Settings.Default.Panel1EraseName;
						label145.ForeColor = Properties.Settings.Default.Panel1CutForeColor;
						label145.BackColor = Properties.Settings.Default.Panel1CutBackColor;
						label145.Text = Properties.Settings.Default.Panel1CutName;
						label146.ForeColor = Properties.Settings.Default.Panel1DontForeColor;
						label146.BackColor = Properties.Settings.Default.Panel1DontBackColor;
						label146.Text = Properties.Settings.Default.Panel1DontName;
						label147.ForeColor = Properties.Settings.Default.Panel1StopForeColor;
						label147.BackColor = Properties.Settings.Default.Panel1StopBackColor;
						label147.Text = Properties.Settings.Default.Panel1StopName;
						panel5.Visible = true;
						panelIndex = 0;
						panel5.BringToFront();
						break;
					case 1:
					case 4:
						// 前回が1か4で今回が1か4ならメインパネル表示動作はなし
						if (viewPanel == 1 || viewPanel == 4)
							isCancel = true;
						if (targetNumber == 1)
						{
							dataGridView2.Rows.Clear();
							dataGridView5.Rows.Clear();
							if (isCancel == true)
							{
								for (int i = 0; i < 10; i++)
								{
									panel7.Location = new Point(8, panel7.Location.Y + 70);
									Application.DoEvents();
									System.Threading.Thread.Sleep(10);
								}
							}
							panel7.Location = new Point(8, 1000);
							panel7.Visible = false;
						}
						else
						{
							button13.Enabled = false;
							button14.Enabled = false;
							panel7.BringToFront();
							// 稼働データが選択されている状態ならその月(日)のchartを表示
							if (selectDay != -1)
							{
								// 選択されている行に表示されている文字から月(日)を算出
								int select = int.Parse(dataGridView1.Rows[selectDay].Cells[0].Value.ToString());
								if (dataGridView1.Columns[0].HeaderText == "月")
								{
									monthChart(select, this.monthTime(select));
								}
								else
								{
									dayChart(select);
								}
							}
							panel7.Visible = true;
							if (isCancel == true)
							{
								for (int i = 0; i < 10; i++)
								{
									panel7.Location = new Point(8, panel7.Location.Y - 70);
									Application.DoEvents();
									Thread.Sleep(10);
								}
							}
							panel7.Location = new Point(8, 296);
						}
						panel5.Visible = false;
						label148.ForeColor = Properties.Settings.Default.Panel2GreenForeColor;
						label148.BackColor = Properties.Settings.Default.Panel2GreenBackColor;
						label148.Text = Properties.Settings.Default.Panel2GreenName;
						label149.ForeColor = Properties.Settings.Default.Panel2YellowForeColor;
						label149.BackColor = Properties.Settings.Default.Panel2YellowBackColor;
						label149.Text = Properties.Settings.Default.Panel2YellowName;
						label150.ForeColor = Properties.Settings.Default.Panel2RedForeColor;
						label150.BackColor = Properties.Settings.Default.Panel2RedBackColor;
						label150.Text = Properties.Settings.Default.Panel2RedName;
						label151.ForeColor = Properties.Settings.Default.Panel2EraseForeColor;
						label151.BackColor = Properties.Settings.Default.Panel2EraseBackColor;
						label151.Text = Properties.Settings.Default.Panel2EraseName;
						label152.ForeColor = Properties.Settings.Default.Panel2CutForeColor;
						label152.BackColor = Properties.Settings.Default.Panel2CutBackColor;
						label152.Text = Properties.Settings.Default.Panel2CutName;
						label153.ForeColor = Properties.Settings.Default.Panel2DontForeColor;
						label153.BackColor = Properties.Settings.Default.Panel2DontBackColor;
						label153.Text = Properties.Settings.Default.Panel2DontName;
						panel6.Visible = true;
						panelIndex = 1;
						panel6.BringToFront();
						break;
					case 2:
						panel5.Visible = false;
						label148.ForeColor = Properties.Settings.Default.Panel2GreenForeColor;
						label148.BackColor = Properties.Settings.Default.Panel2GreenBackColor;
						label148.Text = Properties.Settings.Default.Panel2GreenName;
						label149.ForeColor = Properties.Settings.Default.Panel2YellowForeColor;
						label149.BackColor = Properties.Settings.Default.Panel2YellowBackColor;
						label149.Text = Properties.Settings.Default.Panel2YellowName;
						label150.ForeColor = Properties.Settings.Default.Panel2RedForeColor;
						label150.BackColor = Properties.Settings.Default.Panel2RedBackColor;
						label150.Text = Properties.Settings.Default.Panel2RedName;
						label151.ForeColor = Properties.Settings.Default.Panel2EraseForeColor;
						label151.BackColor = Properties.Settings.Default.Panel2EraseBackColor;
						label151.Text = Properties.Settings.Default.Panel2EraseName;
						label152.ForeColor = Properties.Settings.Default.Panel2CutForeColor;
						label152.BackColor = Properties.Settings.Default.Panel2CutBackColor;
						label152.Text = Properties.Settings.Default.Panel2CutName;
						label153.ForeColor = Properties.Settings.Default.Panel2DontForeColor;
						label153.BackColor = Properties.Settings.Default.Panel2DontBackColor;
						label153.Text = Properties.Settings.Default.Panel2DontName;
						panel6.Visible = true;
						panelIndex = 2;
						panel6.BringToFront();
						p3View();
						break;
					case 3:
						panel5.Visible = false;
						panel6.Visible = false;
						label1.ForeColor = Properties.Settings.Default.Panel1GreenForeColor;
						label1.BackColor = Properties.Settings.Default.Panel1GreenBackColor;
						label1.Text = Properties.Settings.Default.Panel1GreenName;
						label2.ForeColor = Properties.Settings.Default.Panel1YellowForeColor;
						label2.BackColor = Properties.Settings.Default.Panel1YellowBackColor;
						label2.Text = Properties.Settings.Default.Panel1YellowName;
						label3.ForeColor = Properties.Settings.Default.Panel1RedForeColor;
						label3.BackColor = Properties.Settings.Default.Panel1RedBackColor;
						label3.Text = Properties.Settings.Default.Panel1RedName;
						label4.ForeColor = Properties.Settings.Default.Panel1EraseForeColor;
						label4.BackColor = Properties.Settings.Default.Panel1EraseBackColor;
						label4.Text = Properties.Settings.Default.Panel1EraseName;
						label5.ForeColor = Properties.Settings.Default.Panel1CutForeColor;
						label5.BackColor = Properties.Settings.Default.Panel1CutBackColor;
						label5.Text = Properties.Settings.Default.Panel1CutName;
						label6.ForeColor = Properties.Settings.Default.Panel1DontForeColor;
						label6.BackColor = Properties.Settings.Default.Panel1DontBackColor;
						label6.Text = Properties.Settings.Default.Panel1DontName;
						label133.ForeColor = Properties.Settings.Default.Panel1StopForeColor;
						label133.BackColor = Properties.Settings.Default.Panel1StopBackColor;
						label133.Text = Properties.Settings.Default.Panel1StopName;

						label134.ForeColor = Properties.Settings.Default.Panel2GreenForeColor;
						label134.BackColor = Properties.Settings.Default.Panel2GreenBackColor;
						label134.Text = Properties.Settings.Default.Panel2GreenName;
						label135.ForeColor = Properties.Settings.Default.Panel2YellowForeColor;
						label135.BackColor = Properties.Settings.Default.Panel2YellowBackColor;
						label135.Text = Properties.Settings.Default.Panel2YellowName;
						label136.ForeColor = Properties.Settings.Default.Panel2RedForeColor;
						label136.BackColor = Properties.Settings.Default.Panel2RedBackColor;
						label136.Text = Properties.Settings.Default.Panel2RedName;
						label137.ForeColor = Properties.Settings.Default.Panel2EraseForeColor;
						label137.BackColor = Properties.Settings.Default.Panel2EraseBackColor;
						label137.Text = Properties.Settings.Default.Panel2EraseName;
						label138.ForeColor = Properties.Settings.Default.Panel2CutForeColor;
						label138.BackColor = Properties.Settings.Default.Panel2CutBackColor;
						label138.Text = Properties.Settings.Default.Panel2CutName;
						label139.ForeColor = Properties.Settings.Default.Panel2DontForeColor;
						label139.BackColor = Properties.Settings.Default.Panel2DontBackColor;
						label139.Text = Properties.Settings.Default.Panel2DontName;
						textBox9.Text = ((double)Properties.Settings.Default.RegularlyTime / 1000).ToString();

						checkBox1.Checked = Properties.Settings.Default.IsGridSnap;
						checkBox4.Checked = Properties.Settings.Default.IsViewErrir;
						checkBox5.Checked = Properties.Settings.Default.IsViewOccupancy;
						checkBox6.Checked = Properties.Settings.Default.IsViewMaintenance;
						checkBox7.Checked = Properties.Settings.Default.IsViewMemo;

						if (comboBox1.Items.Count > 0 && comboBox1.SelectedIndex == -1)
							comboBox1.SelectedIndex = 0;
						if (comboBox2.Items.Count > 0 && comboBox2.SelectedIndex == -1)
							comboBox2.SelectedIndex = 0;
						
						panelIndex = 3;
						break;
				}
				// 画面切り替えが必要なら
				if (isCancel == false)
				{
					// 対象となるパネルの初期位置をセット
					panel[panelIndex].Location = new Point(4, 1048);
					// 対象となるパネルを前面にする
					panel[panelIndex].BringToFront();
					for (int i = 0; i < 10; i++)
					{
						panel[panelIndex].Location = new Point(panel[panelIndex].Location.X, panel[panelIndex].Location.Y - 100);
						Application.DoEvents();
						Thread.Sleep(10);
					}
					// 最後の微調整
					panel[panelIndex].Location = new Point(4, 48);
				}

				// パネルによって表示最後に処理すべき内容
				switch (targetNumber)
				{
					case 0:
						button11.Enabled = true;
						break;
					case 1:
						// 以前装置が選択されていれば同じ装置を選択する
						if (selectMachine != -1 && dataGridView3.RowCount > (selectMachine + 1))
							dataGridView3.Rows[selectMachine].Selected = true;
						// 「日」の情報が表示されていて稼働情報が表示されていれば、その日のエラー情報を表示させる
						if (dataGridView1.Columns[0].HeaderText == "日" && selectDay != -1)
						{
							viewAlarm();
						}
						button2.Enabled = false;
						button11.Enabled = true;
						break;
					case 2:
						button11.Enabled = true;
						panel3.Focus();
						break;
					case 3:
						button11.Enabled = true;
						viewArea();
						break;
					case 4:
						button2.Enabled = true;
						button11.Enabled = false;
						break;
				}
				// 表示されているパネルに該当するボタン以外をEnable
				for (int i = 0; i < panel.Length; i++)
				{
					if (i != panelIndex)
						button[i].Enabled = true;
				}
				viewPanel = targetNumber;
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 稼働状況ボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button1_Click(object sender, EventArgs e)
		{
			SetPanel(0);
		}

		/// <summary>
		/// 装置個別情報ボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button2_Click(object sender, EventArgs e)
		{
			SetPanel(1);
		}

		/// <summary>
		/// 装置稼動推移ボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button3_Click(object sender, EventArgs e)
		{
			SetPanel(2);
		}

		/// <summary>
		/// 設定ボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button4_Click(object sender, EventArgs e)
		{
			SetPanel(3);
		}

		/// <summary>
		/// 装置設定値の保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button7_Click(object sender, EventArgs e)
		{
			try
			{
				button7.Visible = false;
				timer1.Stop();
				string SettingPath = AppDomain.CurrentDomain.BaseDirectory + "MachineInformation.csv";
				using (FileStream fs = new FileStream(SettingPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					foreach (var mc in machineInformation)
					{
						foreach (var m in mc)
						{
							sw.WriteLine(m.UpdateSetting());
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// エリア名称セット
		/// </summary>
		private void areaName()
		{
			label9.Text = Properties.Settings.Default.Area1;
			radioButton1.Text = Properties.Settings.Default.Area1;
			radioButton3.Text = Properties.Settings.Default.Area1;
			radioButton5.Text = Properties.Settings.Default.Area1;
			textBox10.Text = Properties.Settings.Default.Area1;

			label10.Text = Properties.Settings.Default.Area2;
			radioButton2.Text = Properties.Settings.Default.Area2;
			radioButton4.Text = Properties.Settings.Default.Area2;
			radioButton6.Text = Properties.Settings.Default.Area2;
			textBox11.Text = Properties.Settings.Default.Area2;
		}

		#endregion 全体を統括するメソッド ----------------------------------------------------------------------------------------------------

		#region 稼動マップパネル ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// グループボックス1の再描画
		/// ここで基準線を描画する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel9_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				// Graphicsオブジェクトの作成
				using (Graphics g = e.Graphics)
				{
					// 背景の塗りつぶし
					foreach (var area in areaRoom2)
					{
						using (SolidBrush fillbrush = new SolidBrush(area.FillColor))
						using (Pen linePen = new Pen(area.LineColor, 2))
						using (SolidBrush titlebrush = new SolidBrush(Color.Black))
						{
							g.FillRectangle(fillbrush, area.Rect);
							g.DrawRectangle(linePen, area.Rect);
							e.Graphics.DrawString(area.Title, this.Font, titlebrush, new Point(area.Rect.X + 5, area.Rect.Y - 10));
						}
					}

					// パネル1のグループ内に基準線を引く
					for (int i = 50; i < panel9.Width; i += 50)
					{
						for (int j = 50; j < panel9.Height; j += 50)
						{
							g.FillRectangle(Brushes.Gray, i - 2, j, 5, 1);
							g.FillRectangle(Brushes.Gray, i, j - 2, 1, 5);
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// グループボックス2の再描画
		/// ここで基準線を描画する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel8_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				// Graphicsオブジェクトの作成
				using (Graphics g = e.Graphics)
				{
					// 背景の塗りつぶし
					foreach (var area in areaRoom1)
					{
						using (SolidBrush fillbrush = new SolidBrush(area.FillColor))
						using (Pen linePen = new Pen(area.LineColor, 2))
						using (SolidBrush titlebrush = new SolidBrush(Color.Black))
						{
							g.FillRectangle(fillbrush, area.Rect);
							g.DrawRectangle(linePen, area.Rect);
							e.Graphics.DrawString(area.Title, this.Font, titlebrush, new Point(area.Rect.X + 5, area.Rect.Y - 10));
						}
					}

					// パネル1のグループ内に基準線を引く
					for (int i = 50; i < panel8.Width; i += 50)
					{
						for (int j = 50; j < panel8.Height; j += 50)
						{
							g.FillRectangle(Brushes.Gray, i - 2, j, 5, 1);
							g.FillRectangle(Brushes.Gray, i, j - 2, 1, 5);
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// スプリッタの移動が完了した時
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void splitter1_SplitterMoved(object sender, SplitterEventArgs e)
		{
			Properties.Settings.Default.Room2Width = panel8.Width;
			// 表示しているコントロールの配置をチェック
			foreach (var mc in machineInformation)
			{
				foreach (var m in mc)
				{
					m.CheckLocation();
				}
			}
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// 背景の設定が選択された時
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private async void 背景の設定ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			背景の設定ToolStripMenuItem.Visible = false;
			Control source = contextMenuStrip3.SourceControl;
			try
			{
				await Task.Run(() =>
				{
					// 最初なのでRectangle構造体を初期化しておく
					rect.X = 0;
					rect.Y = 0;
					rect.Width = 0;
					rect.Height = 0;
					MessageBox.Show("始点と終点でクリックしてください。\r\nEscキーでキャンセル。", "背景設定");
					// マウスは手
					Invoke((Action)(() =>
					{
						if (source.Name == "panel8")
							panel8.Cursor = Cursors.Hand;
						else
							panel9.Cursor = Cursors.Hand;
					}));
					isWaitClick = true;
					// 最初のクリックがあるまで待機
					eventPos1 = WaitHandle.WaitAny(clickEvent);
					// キャンセル(Escキーが押された)チェック
					if (isCancel == true)
					{
						isCancel = false;
						isWaitClick = false;
					}
					else
					{
						isClick = true;
						// 2回目のクリックがあるまで待機
						eventPos2 = WaitHandle.WaitAny(clickEvent);
						// キャンセル(Escキーが押された)チェック
						if (isCancel == true)
						{
							isCancel = false;
							isWaitClick = false;
							isClick = false;
							if (source.Name == "panel8")
							{
								using (Graphics g = panel8.CreateGraphics())
								using (Pen oldp = new Pen(panel8.BackColor, 2))
								{
									g.DrawRectangle(oldp, rect);
									Invoke((Action)(() => panel8.Refresh()));
								}
							}
							else
							{
								using (Graphics g = panel9.CreateGraphics())
								using (Pen oldp = new Pen(panel9.BackColor, 2))
								{
									g.DrawRectangle(oldp, rect);
									Invoke((Action)(() => panel9.Refresh()));
								}
							}
						}
						else
						{
							// ここまで来たら登録
							isWaitClick = false;
							isClick = false;
							// 設定画面表示
							Area ar = new Area(new Rectangle(rect.X, rect.Y, rect.Width, rect.Height));
							
							Invoke((Action)(() =>
							{
								using (Form4 f = new Form4(ar))
								{
									f.Left = System.Windows.Forms.Cursor.Position.X - 50;
									f.Top = System.Windows.Forms.Cursor.Position.Y - 280;
									f.Size = new Size(443, 253);
									// OKならリストに追加して再描画
									if (f.ShowDialog(this) == DialogResult.OK)
									{
										if (source.Name == "panel8")
										{
											this.areaRoom1.Add(f.area);
										}
										else
										{
											this.areaRoom2.Add(f.area);
										}
										this.saveArea();
									}
									// キャンセルでもリフレッシュは行う
									if (source.Name == "panel8")
										panel8.Refresh();
									else
										panel9.Refresh();
									this.saveArea();
								}
							}));
						}
					}
				});
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
			// マウスカーソルを戻す
			if (source.Name == "panel8")
				panel8.Cursor = Cursors.Default;
			else
				panel9.Cursor = Cursors.Default;
			背景の設定ToolStripMenuItem.Visible = true;
		}

		/// <summary>
		/// エリア1表示エリアでマウスボタンが離された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel8_MouseUp(object sender, MouseEventArgs e)
		{
			// 背景の設定が開始されていれば位置を取得
			if (isWaitClick == true)
			{
				clickPos.X = e.X;
				clickPos.Y = e.Y;
				clickEvent[0].Set();
			}
		}

		/// <summary>
		/// エリア2表示エリアでマウスボタンが離された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel9_MouseUp(object sender, MouseEventArgs e)
		{
			// 背景の設定が開始されていれば位置を取得
			if (isWaitClick == true)
			{
				clickPos.X = e.X;
				clickPos.Y = e.Y;
				clickEvent[1].Set();
			}
		}

		/// <summary>
		/// 描画可能な状態なら四角を描画する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel8_MouseMove(object sender, MouseEventArgs e)
		{
			if (isWaitClick == true && isClick == true && eventPos1 == 0)
			{
				using (Graphics g = panel8.CreateGraphics())
				using (Pen oldp = new Pen(panel8.BackColor, 2))
				using (Pen p = new Pen(Color.Black, 2))
				{
					// 以前に描画されていれば消す
					if(rect.IsEmpty != true)
						g.DrawRectangle(oldp, rect);
					// 差がプラスかマイナスかによってセットする値を変える
					if (e.X - clickPos.X > 0)
					{
						rect.X = clickPos.X;
						rect.Width = e.X - clickPos.X;
					}
					else
					{
						rect.X = e.X;
						rect.Width = clickPos.X - e.X;
					}
					if (e.Y - clickPos.Y > 0)
					{
						rect.Y = clickPos.Y;
						rect.Height = e.Y - clickPos.Y;
					}
					else
					{
						rect.Y = e.Y;
						rect.Height = clickPos.Y - e.Y;
					}					
					g.DrawRectangle(p, rect);
				}
				// グリッドが消えるので再描画
				using (Graphics g = this.panel8.CreateGraphics())
				{
					// パネル1のグループ内に基準線を引く
					for (int i = 50; i < panel8.Width; i += 50)
					{
						for (int j = 50; j < panel8.Height; j += 50)
						{
							g.FillRectangle(Brushes.Gray, i - 2, j, 5, 1);
							g.FillRectangle(Brushes.Gray, i, j - 2, 1, 5);
						}
					}
				}
			}
		}

		/// <summary>
		/// 描画可能な状態なら四角を描画する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel9_MouseMove(object sender, MouseEventArgs e)
		{
			if (isWaitClick == true && isClick == true && eventPos1 == 1)
			{
				using (Graphics g = panel9.CreateGraphics())
				using (Pen oldp = new Pen(panel9.BackColor, 2))
				using (Pen p = new Pen(Color.Black, 2))
				{
					// 以前に描画されていれば消す
					if (rect.IsEmpty != true)
						g.DrawRectangle(oldp, rect);
					// 差がプラスかマイナスかによってセットする値を変える
					if (e.X - clickPos.X > 0)
					{
						rect.X = clickPos.X;
						rect.Width = e.X - clickPos.X;
					}
					else
					{
						rect.X = e.X;
						rect.Width = clickPos.X - e.X;
					}
					if (e.Y - clickPos.Y > 0)
					{
						rect.Y = clickPos.Y;
						rect.Height = e.Y - clickPos.Y;
					}
					else
					{
						rect.Y = e.Y;
						rect.Height = clickPos.Y - e.Y;
					}
					g.DrawRectangle(p, rect);
				}
				// グリッドが消えるので再描画
				using (Graphics g = this.panel9.CreateGraphics())
				{
					// パネル1のグループ内に基準線を引く
					for (int i = 50; i < panel9.Width; i += 50)
					{
						for (int j = 50; j < panel9.Height; j += 50)
						{
							g.FillRectangle(Brushes.Gray, i - 2, j, 5, 1);
							g.FillRectangle(Brushes.Gray, i, j - 2, 1, 5);
						}
					}
				}
			}
		}

		/// <summary>
		/// 稼働マップのエリア情報を保存
		/// </summary>
		private void saveArea()
		{
			string SettingPath = AppDomain.CurrentDomain.BaseDirectory + "MachineArea.csv";
			using (FileStream fs = new FileStream(SettingPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
			using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
			{
				foreach (var area in areaRoom1)
				{
					StringBuilder dat = new StringBuilder("1,");
					dat.Append(area.Title).Append(",");
					dat.Append(area.Rect.X.ToString()).Append(",");
					dat.Append(area.Rect.Y.ToString()).Append(",");
					dat.Append(area.Rect.Width.ToString()).Append(",");
					dat.Append(area.Rect.Height.ToString()).Append(",");
					dat.Append(area.LineColor.R.ToString()).Append(",");
					dat.Append(area.LineColor.G.ToString()).Append(",");
					dat.Append(area.LineColor.B.ToString()).Append(",");
					dat.Append(area.FillColor.R.ToString()).Append(",");
					dat.Append(area.FillColor.G.ToString()).Append(",");
					dat.Append(area.FillColor.B.ToString());
					sw.WriteLine(dat.ToString());
				}
				foreach (var area in areaRoom2)
				{
					StringBuilder dat = new StringBuilder("2,");
					dat.Append(area.Title).Append(",");
					dat.Append(area.Rect.X.ToString()).Append(",");
					dat.Append(area.Rect.Y.ToString()).Append(",");
					dat.Append(area.Rect.Width.ToString()).Append(",");
					dat.Append(area.Rect.Height.ToString()).Append(",");
					dat.Append(area.LineColor.R.ToString()).Append(",");
					dat.Append(area.LineColor.G.ToString()).Append(",");
					dat.Append(area.LineColor.B.ToString()).Append(",");
					dat.Append(area.FillColor.R.ToString()).Append(",");
					dat.Append(area.FillColor.G.ToString()).Append(",");
					dat.Append(area.FillColor.B.ToString());
					sw.WriteLine(dat.ToString());
				}
			}
		}

		#endregion 稼動マップパネル -------------------------------------------------------------------------------------------------


		#region 加工機情報パネル ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// データグリッドに装置情報を表示させる
		/// </summary>
		private void viewMachineInformation()
		{
			try
			{
				if (machineInformation.Count == 0)
					return;
				noCheck = true;
				dataGridView3.Rows.Clear();
				for (int i = 0; i < machineInformation[selectRoom].Count; i++)
				{
					dataGridView3.Rows.Add();
					dataGridView3.Rows[i].Cells[0].Value = machineInformation[selectRoom][i].Maker;
					dataGridView3.Rows[i].Cells[1].Value = machineInformation[selectRoom][i].ModelNumber;
					dataGridView3.Rows[i].Cells[2].Value = machineInformation[selectRoom][i].MachineNumber;
					dataGridView3.Rows[i].Cells[3].Value = machineInformation[selectRoom][i].MachineAddress.ToString();
					dataGridView3.Rows[i].Cells[4].Value = machineInformation[selectRoom][i].MachineCheck == false ? "しない" : "する";
					dataGridView3.Rows[i].Cells[5].Value = machineInformation[selectRoom][i].GreenKeep == false ? "しない" : "する";
					dataGridView3.Rows[i].Cells[6].Value = machineInformation[selectRoom][i].YellowKeep == false ? "しない" : "する";
					dataGridView3.Rows[i].Cells[7].Value = machineInformation[selectRoom][i].RedKeep == false ? "しない" : "する";
					dataGridView3.Rows[i].Cells[8].Value = machineInformation[selectRoom][i].MaintenanceDate;
					dataGridView3.Rows[i].Cells[9].Value = machineInformation[selectRoom][i].AlarmFile;
					dataGridView3.Rows[i].Cells[10].Value = machineInformation[selectRoom][i].OperationFile;
					dataGridView3.Rows[i].Cells[11].Value = machineInformation[selectRoom][i].Memo;
				}
				if (machineInformation[selectRoom].Count > 0)
					dataGridView3.CurrentCell = dataGridView3.Rows[0].Cells[0];
				selectMachine = 0;
				// 装置台数によってはスクロールバーが表示されるのでコントロールのサイズを変える
				dataGridView3.Size = dataGridView3.RowCount < 13 ? new Size(1162, 277) : new Size(1180, 277);
				noCheck = false;
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 装置個別情報の稼働状況表示をクリアする
		/// </summary>
		private void p2ClearOperation()
		{
			try
			{
				if (panel7.Visible == true)
				{
					chart1.Series.Clear();
					chart1.Legends.Clear();
					chart1.Titles.Clear();
					chart1.ChartAreas[0].Area3DStyle.Enable3D = false;
					chart2.Series.Clear();
					chart2.Legends.Clear();
					chart2.Titles.Clear();
					chart2.ChartAreas.Clear();
				}
				// 今のビットマップをクリアする
				for (int i = 0; i < 10; i++)
				{
					// エラー情報をクリア
					P2err[i].Clear();
					P2Button[i].Text = "表示年月日";
					if (operationCanvas[i] != null)
					{
						P2Pictur[i].Image = null;
						operationCanvas[i].Dispose();
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 装置個別の稼働状況を単体で取得する
		/// </summary>
		/// <param name="index"></param>
		/// <param name="bt"></param>
		private void viewOperation(int index, Button bt)
		{
			try
			{
				using (Form2 frm2 = new Form2())
				{
					frm2.Left = System.Windows.Forms.Cursor.Position.X - 50;
					frm2.Top = System.Windows.Forms.Cursor.Position.Y - 280;
					frm2.Size = new Size(273, 317);
					frm2.Text = "稼動状況表示日";
					if (frm2.ShowDialog(this) == DialogResult.OK)
					{
						bt.Text = frm2.SetLongDate;
						List<alarmInformation> err = new List<alarmInformation>();
						if (machineInformation[selectRoom][selectMachine].SetOperation(ref P2Pictur[index], ref operationCanvas[index], frm2.SetDateTime, index, ref err) == true)
							P2err[index] = new List<alarmInformation>(err);
						else
							MessageBox.Show("指定された日の情報はありません。", "稼動情報表示", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// アラームと操作内容の表示
		/// </summary>
		private void viewAlarm()
		{
			try
			{
				// アラーム
				dataGridView2.Rows.Clear();
				dataGridView2.Size = new Size(318, 529);

				List<alarmInformation> alarm = machineInformation[selectRoom][selectMachine].GetErrorDay(new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, selectDay + 1));

				for (int i = 0; i < alarm.Count; i++)
				{
					dataGridView2.Rows.Add();
					dataGridView2.Rows[i].Cells[0].Value = new TimeSpan(0, 0, alarm[i].startPos);
					dataGridView2.Rows[i].Cells[1].Value = alarm[i].errMessage;
				}
				if (alarm.Count > 24)
					dataGridView2.Size = new Size(336, 529);

				// 操作リスト
				dataGridView5.Rows.Clear();
				dataGridView5.Size = new Size(318, 151);
				Dictionary<string,string> operation = machineInformation[selectRoom][selectMachine].OperationList(new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, selectDay + 1));
				// 操作名の配列を作ってコピー
				string[] keys = new string[operation.Keys.Count];
				operation.Keys.CopyTo(keys, 0);
				// 時間の配列を作ってコピー
				string[] value = new string[operation.Values.Count];
				operation.Values.CopyTo(value, 0);
				for (int i = 0; i < operation.Count; i++)
				{
					dataGridView5.Rows.Add();
					dataGridView5.Rows[i].Cells[0].Value = keys[i];
					dataGridView5.Rows[i].Cells[1].Value = value[i];
				}
				if(operation.Count > 6)
					dataGridView5.Size = new Size(336, 151);
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 設定値が変更されたかチェックする
		/// </summary>
		internal void Difference()
		{
			// 変更チェックはコンストラクタ通過後
			if (bootFlag == false)
			{
				try
				{
					foreach (var mc in machineInformation)
					{
						foreach (var m in mc)
						{
							if (m.Difference == false)
							{
								button7.Visible = true;
								timer1.Start();
								return;
							}
						}
					}

					// ここまで来たら差異はないのでボタンは押せないようにする
					button7.Visible = false;
					timer1.Stop();
				}
				catch (Exception exc)
				{
					SysrtmError(exc.StackTrace);
				}
			}
		}

		/// <summary>
		/// エリア1が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void radioButton1_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButton1.Checked == true)
			{
				selectRoom = 0;
				viewMachineInformation();
				p2ClearOperation();
				dataGridView1.Rows.Clear();
				selectDay = -1;
				button13.Enabled = false;
				button14.Enabled = false;
			}
		}

		/// <summary>
		/// エリア2が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void radioButton2_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButton2.Checked == true)
			{
				selectRoom = 1;
				viewMachineInformation();
				p2ClearOperation();
				dataGridView1.Rows.Clear();
				selectDay = -1;
				button13.Enabled = false;
				button14.Enabled = false;
			}
		}

		/// <summary>
		/// 月別情報が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button5_Click(object sender, EventArgs e)
		{
			try
			{
				// 不要な表示を消す
				dataGridView2.Rows.Clear();
				dataGridView5.Rows.Clear();
				// 稼働状況ボタンの有効、無効を記憶しておく
				bool bten = button13.Enabled;
				System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
				button6.Enabled = false;
				button7.Enabled = false;
				button13.Enabled = false;
				button14.Enabled = false;
				// 指定された年のファイルパスを生成
				StringBuilder path = new StringBuilder(AppDomain.CurrentDomain.BaseDirectory);
				path.Append(Path.Combine("加工機情報", dataGridView3.CurrentRow.Cells[2].Value.ToString(), dateTimePicker2.Value.ToString("yyyy年"))).Append(@"\");
				// フォルダの存在チェック
				if (Directory.Exists(path.ToString()))
				{
					dataGridView1.Columns[0].HeaderText = "月";
					dataGridView1.Rows.Clear();
					selectDay = -1;
					dataGridView1.Size = new Size(406, 277);
					for (int i = 1; i < 13; i++)
					{
						// まず行を追加して月をセット
						dataGridView1.Rows.Add();
						dataGridView1.Rows[i - 1].Cells[0].Value = i.ToString("00");
						// 月のパスを生成
						string monthPath = path.ToString() + i.ToString("00") + @"月\";
						int[] totalTime = null;
						// フォルダの存在チェック
						if (Directory.Exists(monthPath))
						{
							// 一ヶ月のデータ取得
							totalTime = this.monthTime(i);

							// データ表示
							for (int j = 0; j < 6; j++)
							{
								if (totalTime[j] != 0)
								{
									dataGridView1.Rows[i - 1].Cells[j + 1].Value = (totalTime[j] / 3600).ToString("00:")
										+ ((totalTime[j] % 3600) / 60).ToString("00:") + (totalTime[j] % 60).ToString("00");
								}
								else
								{
									dataGridView1.Rows[i - 1].Cells[j + 1].Value = "-";
								}
							}
							dataGridView1.Rows[i - 1].Cells[7].Value = ((double)totalTime[0] / totalTime.Sum()).ToString("P1");
						}
						else
						{
							for (int j = 1; j < 8; j++)
							{
								dataGridView1.Rows[i - 1].Cells[j].Value = "-";
							}
						}
						// 現在抽出している月と選択された月が同じならデータグリッドビューを選択状態にする
						if (dateTimePicker2.Value.Month == i)
						{
							dataGridView1.Rows[i - 1].Selected = true;
							selectDay = i - 1;
							// 統計が表示されていればグラフを表示
							if (panel7.Visible == true && totalTime != null)
							{
								monthChart(i, totalTime);
							}
						}
					}
				}
				else
				{
					// 表示するデータがない時だけ稼働状況ボタンを以前の状態に戻す
					if (bten == true)
					{
						button13.Enabled = true;
						button14.Enabled = true;
					}
					MessageBox.Show("指定された月を含む年には情報がありません。", "月別情報", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				button6.Enabled = true;
				button7.Enabled = true;
				button25.Enabled = true;
				button26.Enabled = false;
				System.Windows.Forms.Cursor.Current = Cursors.Default;
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 指定された月のトータル時間を返す
		/// </summary>
		/// <returns>トータル時間の配列</returns>
		private int[] monthTime(int month)
		{
			int[] totalTime = new int[6] { 0, 0, 0, 0, 0, 0 };
			try
			{
				// 該当する月のトータル日数を算出
				int day = DateTime.DaysInMonth(dateTimePicker2.Value.Year, month) + 1;
				// 1ヶ月のデータを集計
				for (int i = 1; i < day; i++)
				{
					int[] getData = machineInformation[selectRoom][selectMachine].GetTotalTime(new DateTime(dateTimePicker2.Value.Year, month, i));
					if (getData != null)
					{
						for (int j = 0; j < 6; j++)
						{
							totalTime[j] += getData[j];
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
			return totalTime;
		}

		/// <summary>
		/// 月単位のチャート表示
		/// </summary>
		/// <param name="month">該当する月</param>
		/// <param name="totaltime">該当する月の時間データ</param>
		private void monthChart(int month, int[] totaltime)
		{
			// 色に該当する数値を返すラムダ式
			Func<int, Color> colorNumber = (color) =>
			{
				return color == 0 ? Properties.Settings.Default.Panel2GreenBackColor :
					color == 1 ? Properties.Settings.Default.Panel2YellowBackColor :
					color == 2 ? Properties.Settings.Default.Panel2RedBackColor :
					color == 3 ? Properties.Settings.Default.Panel2EraseBackColor :
					color == 4 ? Properties.Settings.Default.Panel2CutBackColor :
					Properties.Settings.Default.Panel2DontBackColor;
			};
			// 数字に該当する色の文字を返すラムダ式
			Func<int, string> colorName = (name) =>
			{
				return name == 0 ? "緑" :
					name == 1 ? "黄" :
					name == 2 ? "赤" :
					name == 3 ? "消" :
					name == 4 ? "断" :
					"無";
			};

			try
			{
				// 以前のデータはクリア
				chart1.Series.Clear();
				chart1.Legends.Clear();
				// グラフのタイトル
				chart1.Titles.Clear();
				if(totaltime.Sum() != 0)
					chart1.Titles.Add(dateTimePicker2.Value.ToString("yyyy年") + month.ToString() + "月の稼働情報");

				Series series = new Series();
				series.ChartType = SeriesChartType.Doughnut;

				int j = 0;
				// 表示するデータをセット
				for (int i = 0; i < 6; i++)
				{
					if (totaltime[i] != 0)
					{
						// まずは値をセット
						series.Points.Add(totaltime[i]);
						// 値に該当する色をセット
						series.Points[j].Color = colorNumber(i);
						// 表示する文字列をセット
						series.Points[j].Label = ((double)totaltime[i] / totaltime.Sum()).ToString("P1");
						// 表示するデータが10%以下なら引出線
						if ((double)totaltime[i] / totaltime.Sum() < 0.02)
						{
							series.Points[j]["PieLabelStyle"] = "Outside";
							series.Points[j]["3DLabelLineSize"] = "10";
							series.Points[j]["PieLineColor"] = "Black";
						}
						j++;
					}
				}
				chart1.Series.Add(series);
				
				// 開始位置は上にする
				chart1.Series[0]["PieStartAngle"] = "270";
				chart1.ChartAreas[0].Area3DStyle.Enable3D = true;

				/***** エラー情報表示 *****/

				List<alarmInformation> alarm = new List<alarmInformation>();
				// 該当する月のトータル日数を算出
				int day = DateTime.DaysInMonth(dateTimePicker2.Value.Year, month) + 1;
				// 1ヶ月のデータを集計
				for (int i = 1; i < day; i++)
				{
					alarm.AddRange(new List<alarmInformation>(machineInformation[selectRoom][selectMachine].GetErrorDay(new DateTime(dateTimePicker2.Value.Year, month, i))));
				}

				// 以前のデータはクリア
				chart2.Series.Clear();
				chart2.Legends.Clear();
				chart2.Titles.Clear();
				chart2.ChartAreas.Clear();
				
				if (alarm.Count != 0)
				{
					List<int> count = new List<int>();
					List<int> time = new List<int>();
					List<string> err = new List<string>();
					// Listが0になるまで同一アラームを集計する
					while (alarm.Count != 0)
					{
						// 先頭と同じアラーム名のデータを抽出
						List<alarmInformation> selectAlarm = new List<alarmInformation>(alarm.Where(a => a.errMessage == alarm[0].errMessage));
						int totalTime = 0;
						int totalCoint = 0;
						// 時間を集計する
						foreach (var alm in selectAlarm)
						{
							totalCoint++;
							totalTime += (alm.endPos - alm.startPos);
						}
						// 集計したデータをリストに追加する
						time.Add(totalTime);
						count.Add(totalCoint);
						err.Add(alarm[0].errMessage);
						// 追加したデータは元のListから削除する
						alarm.RemoveAll(a => a.errMessage == err[err.Count - 1]);
					}
					chart2.ChartAreas.Add("ChartArea1");
					// アラームグラフのタイトル
					chart2.Titles.Add(dateTimePicker2.Value.ToString("yyyy年") + month.ToString() + "月のアラーム情報");

					Series series1 = new Series();
					series1.ChartType = SeriesChartType.Column;
					series1.Color = Color.Blue;
					series1.BorderDashStyle = ChartDashStyle.Dash;

					Series series2 = new Series();
					series2.ChartType = SeriesChartType.Column;
					series2.Color = Color.Red;
					series2.YAxisType = AxisType.Secondary;

					for (int i = 0; i < err.Count; i++)
					{
						series1.Points.AddXY(err[i], count[i]);
						series2.Points.AddXY(err[i], (double)time[i] / 3600);
						series1.Points[i].Label = count[i].ToString() + "回";
						series2.Points[i].Label = (time[i] / 3600).ToString("0時") + ((time[i] % 3600) / 60).ToString("0分") + (time[i] % 60).ToString("0秒");
					}
					chart2.Series.Add(series1);
					chart2.Series.Add(series2);

					// データポイントの描画スタイルを変える
					chart2.Series[0]["DrawingStyle"] = "Cylinder";
					chart2.Series[1]["DrawingStyle"] = "Cylinder";

					// データ部のグラデーション
					chart2.ChartAreas[0].BackColor = Color.White;
					chart2.ChartAreas[0].BackGradientStyle = GradientStyle.TopBottom;
					chart2.ChartAreas[0].BackSecondaryColor = Color.LimeGreen;

					// X軸のアラーム名は45度配置
					chart2.ChartAreas[0].AxisX.LabelStyle.Angle = 45;

					// 軸タイトル設定
					chart2.ChartAreas[0].AxisX.Title = "アラーム名";
					chart2.ChartAreas[0].AxisY.Title = "発生回数";
					chart2.ChartAreas[0].AxisY.TitleForeColor = Color.Blue;
					chart2.ChartAreas[0].AxisY2.Title = "アラーム合計時間";
					chart2.ChartAreas[0].AxisY2.TitleForeColor = Color.Red;

					// Y軸基準線の色
					chart2.ChartAreas[0].AxisY.LineColor = Color.Blue;
					chart2.ChartAreas[0].AxisY.LineWidth = 2;
					chart2.ChartAreas[0].AxisY.LineDashStyle = ChartDashStyle.Dash;
					chart2.ChartAreas[0].AxisY2.LineColor = Color.Red;

					// X軸の目盛線は消す
					chart2.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
					chart2.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;

					// Y軸基準ラベルの文字色
					chart2.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.Blue;
					chart2.ChartAreas[0].AxisY2.LabelStyle.ForeColor = Color.Red;

					// アラーム回数の基準線の設定
					chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Blue;
					chart2.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
					chart2.ChartAreas[0].AxisY.MajorGrid.LineWidth = 2;
					// アラーム回数の目盛の設定
					chart2.ChartAreas[0].AxisY.MajorTickMark.LineColor = Color.Blue;
					chart2.ChartAreas[0].AxisY.MajorTickMark.LineWidth = 2;
					chart2.ChartAreas[0].AxisY.MajorTickMark.LineDashStyle = ChartDashStyle.Dash;

					// アラーム時間の基準線の設定
					chart2.ChartAreas[0].AxisY2.MajorGrid.LineColor = Color.Red;
					chart2.ChartAreas[0].AxisY2.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
					// アラーム時間の目盛の設定
					chart2.ChartAreas[0].AxisY2.MajorTickMark.LineColor = Color.Red;

					// Y軸の最大値設定
					chart2.ChartAreas[0].AxisY.Maximum = count.Max() + 1;
					chart2.ChartAreas[0].AxisY2.Maximum = time.Max() / 3600 + 1;
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 日単位のチャート表示
		/// </summary>
		/// <param name="day">該当する日</param>
		private void dayChart(int day)
		{
			// 色に該当する数値を返すラムダ式
			Func<int, Color> colorNumber = (color) =>
			{
				return color == 0 ? Properties.Settings.Default.Panel2GreenBackColor :
					color == 1 ? Properties.Settings.Default.Panel2YellowBackColor :
					color == 2 ? Properties.Settings.Default.Panel2RedBackColor :
					color == 3 ? Properties.Settings.Default.Panel2EraseBackColor :
					color == 4 ? Properties.Settings.Default.Panel2CutBackColor :
					Properties.Settings.Default.Panel2DontBackColor;
			};

			try
			{
				// 以前のデータはクリア
				chart1.Series.Clear();
				chart1.Legends.Clear();
				chart1.Titles.Clear();

				// 以前のデータはクリア
				chart2.Series.Clear();
				chart2.Legends.Clear();
				chart2.Titles.Clear();
				chart2.ChartAreas.Clear();

				Series series = new Series();
				series.ChartType = SeriesChartType.Doughnut;

				int[] getData = machineInformation[selectRoom][selectMachine].GetTotalTime(new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, day));

				if (getData == null)
				{
					chart1.Series.Add(series);
					return;
				}

				chart1.Titles.Add(dateTimePicker2.Value.ToString("yyyy年M月") + day.ToString() + "日の稼働情報");
				int j = 0;
				// 表示するデータをセット
				for (int i = 0; i < 6; i++)
				{
					if (getData[i] != 0)
					{
						// まずは値をセット
						series.Points.Add(getData[i]);
						// 値に該当する色をセット
						series.Points[j].Color = colorNumber(i);
						// 表示する文字列をセット
						series.Points[j].Label = ((double)getData[i] / getData.Sum()).ToString("P1");
						// 表示するデータが10%以下なら引出線
						if ((double)getData[i] / getData.Sum() < 0.02)
						{
							series.Points[j]["PieLabelStyle"] = "Outside";
							series.Points[j]["3DLabelLineSize"] = "10";
							series.Points[j]["PieLineColor"] = "Black";
						}
						j++;
					}
				}
				chart1.Series.Add(series);
				// 開始位置は上にする
				chart1.Series[0]["PieStartAngle"] = "270";
				chart1.ChartAreas[0].Area3DStyle.Enable3D = true;
				
				/***** エラー情報表示 *****/
				List<alarmInformation> alarm = machineInformation[selectRoom][selectMachine].GetErrorDay(new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, day));

				if (alarm.Count != 0)
				{
					chart2.ChartAreas.Add("ChartArea1");
					List<int> count = new List<int>();
					List<int> time = new List<int>();
					List<string> err = new List<string>();
					// Listが0になるまで同一アラームを集計する
					while (alarm.Count != 0)
					{
						// 先頭と同じアラーム名のデータを抽出
						List<alarmInformation> selectAlarm = new List<alarmInformation>(alarm.Where(a => a.errMessage == alarm[0].errMessage));
						int totalTime = 0;
						int totalCoint = 0;
						// 時間を集計する
						foreach (var alm in selectAlarm)
						{
							totalCoint++;
							totalTime += (alm.endPos - alm.startPos);
						}
						// 集計したデータをリストに追加する
						time.Add(totalTime);
						count.Add(totalCoint);
						err.Add(alarm[0].errMessage);
						// 追加したデータは元のListから削除する
						alarm.RemoveAll(a => a.errMessage == err[err.Count - 1]);
					}

					// アラームグラフのタイトル
					chart2.Titles.Add(dateTimePicker2.Value.ToString("yyyy年M月") + day.ToString() + "日のアラーム情報");

					Series series1 = new Series();
					series1.ChartType = SeriesChartType.Column;
					series1.Color = Color.Blue;
					series1.BorderDashStyle = ChartDashStyle.Dash;

					Series series2 = new Series();
					series2.ChartType = SeriesChartType.Column;
					series2.Color = Color.Red;
					series2.YAxisType = AxisType.Secondary;

					for (int i = 0; i < err.Count; i++)
					{
						series1.Points.AddXY(err[i], count[i]);
						series2.Points.AddXY(err[i], (double)time[i] / 3600);
						series1.Points[i].Label = count[i].ToString() + "回";
						series1.Points[i].LabelToolTip = count[i].ToString() + "回";

						series2.Points[i].Label = new TimeSpan(0, 0, time[i]).ToString(@"h\時m\分s\秒");
						series2.Points[i].LabelToolTip = series2.Points[i].Label;
					}
					chart2.Series.Add(series1);
					chart2.Series.Add(series2);

					// データポイントの描画スタイルを変える
					chart2.Series[0]["DrawingStyle"] = "Cylinder";
					chart2.Series[1]["DrawingStyle"] = "Cylinder";

					// データ部のグラデーション
					chart2.ChartAreas[0].BackColor = Color.White;
					chart2.ChartAreas[0].BackGradientStyle = GradientStyle.TopBottom;
					chart2.ChartAreas[0].BackSecondaryColor = Color.LimeGreen;

					// X軸のアラーム名は45度配置
					chart2.ChartAreas[0].AxisX.LabelStyle.Angle = 45;

					// 軸タイトル設定
					chart2.ChartAreas[0].AxisX.Title = "アラーム名";
					chart2.ChartAreas[0].AxisY.Title = "発生回数";
					chart2.ChartAreas[0].AxisY.TitleForeColor = Color.Blue;
					chart2.ChartAreas[0].AxisY2.Title = "アラーム合計時間";
					chart2.ChartAreas[0].AxisY2.TitleForeColor = Color.Red;

					// アラーム回数の目盛間隔
					chart2.ChartAreas[0].AxisY.Interval = count.Max() / 10 + 1;

					// Y軸基準線の色
					chart2.ChartAreas[0].AxisY.LineColor = Color.Blue;
					chart2.ChartAreas[0].AxisY.LineWidth = 2;
					chart2.ChartAreas[0].AxisY.LineDashStyle = ChartDashStyle.Dash;
					chart2.ChartAreas[0].AxisY2.LineColor = Color.Red;

					// X軸の目盛線は消す
					chart2.ChartAreas[0].AxisX.MajorGrid.Enabled = false;
					chart2.ChartAreas[0].AxisX.MajorTickMark.Enabled = false;

					// Y軸基準ラベルの文字色
					chart2.ChartAreas[0].AxisY.LabelStyle.ForeColor = Color.Blue;
					chart2.ChartAreas[0].AxisY2.LabelStyle.ForeColor = Color.Red;

					// アラーム回数の基準線の設定
					chart2.ChartAreas[0].AxisY.MajorGrid.LineColor = Color.Blue;
					chart2.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
					chart2.ChartAreas[0].AxisY.MajorGrid.LineWidth = 2;
					// アラーム回数の目盛の設定
					chart2.ChartAreas[0].AxisY.MajorTickMark.LineColor = Color.Blue;
					chart2.ChartAreas[0].AxisY.MajorTickMark.LineWidth = 2;
					chart2.ChartAreas[0].AxisY.MajorTickMark.LineDashStyle = ChartDashStyle.Dash;
					
					// アラーム時間の基準線の設定
					chart2.ChartAreas[0].AxisY2.MajorGrid.LineColor = Color.Red;
					chart2.ChartAreas[0].AxisY2.MajorGrid.LineDashStyle = ChartDashStyle.Solid;
					// アラーム時間の目盛の設定
					chart2.ChartAreas[0].AxisY2.MajorTickMark.LineColor = Color.Red;

					// Y軸の最大値設定
					chart2.ChartAreas[0].AxisY.Maximum = count.Max() + 1;
					chart2.ChartAreas[0].AxisY2.Maximum = time.Max() / 3600 + 1;
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 日別情報が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button6_Click(object sender, EventArgs e)
		{
			try
			{
				System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
				button6.Enabled = false;
				button7.Enabled = false;
				button13.Enabled = false;
				button14.Enabled = false;
				// 指定された月のファイルパスを生成
				StringBuilder path = new StringBuilder(AppDomain.CurrentDomain.BaseDirectory);
				path.Append(Path.Combine("加工機情報", dataGridView3.CurrentRow.Cells[2].Value.ToString(), dateTimePicker2.Value.ToString("yyyy年"), dateTimePicker2.Value.ToString("MM月"))).Append(@"\");
				// フォルダの存在チェック
				if (Directory.Exists(path.ToString()))
				{
					dataGridView1.Columns[0].HeaderText = "日";
					dataGridView1.Rows.Clear();
					selectDay = -1;
					// 指定された月の総日数(+1)
					int totalDay = DateTime.DaysInMonth(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month) + 1;
					for (int i = 1; i < totalDay; i++)
					{
						// 表示するデータの行を追加
						dataGridView1.Rows.Add();
						// まずは日付を入れる
						dataGridView1.Rows[i - 1].Cells[0].Value = i.ToString("00");
						int[] getDate = null;
						// ファイルが存在していればデータ取得
						if (File.Exists(path.ToString() + i.ToString("00") + "日.csv"))
						{
							// 存在しているファイル名からDateTime型のデータを作る
							DateTime setDate = new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, i);
							// Machineクラスのインスタンスから指定した日の情報を取得
							getDate = machineInformation[selectRoom][selectMachine].GetTotalTime(setDate);
							//配列がNullでなければ秒を時、分、秒に変換して表示する
							if (getDate != null)
							{
								for (int j = 0; j < 6; j++)
								{
									dataGridView1.Rows[i - 1].Cells[j + 1].Value = new TimeSpan(0, 0, getDate[j]).ToString();
								}
								dataGridView1.Rows[i - 1].Cells[7].Value = ((double)getDate[0] / getDate.Sum()).ToString("P1");
							}
							else
							{
								// データが存在してなければ「-」を表示
								for (int j = 1; j < 8; j++)
								{
									dataGridView1.Rows[i - 1].Cells[j].Value = "-";
								}
							}
						}
						else
						{
							// データが存在してなければ「-」を表示
							for (int j = 1; j < 8; j++)
							{
								dataGridView1.Rows[i - 1].Cells[j].Value = "-";
							}
						}
						// 現在抽出している日と選択された日が同じならデータグリッドビューを選択状態にする
						if (dateTimePicker2.Value.Day == i)
						{
							dataGridView1.Rows[i - 1].Selected = true;
							selectDay = i - 1;
							// 統計が表示されていればグラフを表示
							if (panel7.Visible == true && getDate != null)
							{
								dayChart(i);
							}
						}
					}
					if (panel7.Visible != true)
					{
						button13.Enabled = true;
						button14.Enabled = true;
					}
					dataGridView1.Size = new Size(424, 277);
					// 選択された行までスクロールする
					dataGridView1.FirstDisplayedScrollingRowIndex = selectDay;
					// 選択されている行のアラーム表示
					viewAlarm();
				}
				else
				{
					MessageBox.Show("指定された日を含む月には情報がありません。", "日別情報", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				button6.Enabled = true;
				button7.Enabled = true;
				button25.Enabled = false;
				button26.Enabled = true;
				System.Windows.Forms.Cursor.Current = Cursors.Default;
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 装置追加
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button8_Click(object sender, EventArgs e)
		{
			try
			{
				MessageBox.Show("追加した装置情報は保存しないと監視対象となりません。\r\n装置情報を全て入力した後に「保存」してください。", "装置追加", MessageBoxButtons.OK, MessageBoxIcon.Information);
				// データグリッドビューの編集内容変更イベント処理は行わないようにする
				int addIndex = dataGridView3.Rows.Count;
				noCheck = true;
				dataGridView3.Rows.Add();
				//dataGridView3.Rows[addIndex].Cells[0].Value = "メーカ";
				dataGridView3.Rows[addIndex].Cells[1].Value = "メーカ型番";
				dataGridView3.Rows[addIndex].Cells[2].Value = "機械番号";
				dataGridView3.Rows[addIndex].Cells[3].Value = "0.0.0.0";
				dataGridView3.Rows[addIndex].Cells[4].Value = "しない";
				dataGridView3.Rows[addIndex].Cells[5].Value = "しない";
				dataGridView3.Rows[addIndex].Cells[6].Value = "しない";
				dataGridView3.Rows[addIndex].Cells[7].Value = "しない";
				dataGridView3.Rows[addIndex].Cells[8].Value = "記録なし";
				//dataGridView3.Rows[addIndex].Cells[9].Value = "アラームリスト";
				dataGridView3.Rows[addIndex].Cells[11].Value = "メモ";
				// 追加した行を選択状態にする
				dataGridView3.Rows[addIndex].Selected = true;
				selectMachine = addIndex;
				// 装置台数によってはスクロールバーが表示されるのでコントロールのサイズを変える
				dataGridView3.Size = dataGridView3.RowCount < 13 ? new Size(1162, 277) : new Size(1180, 277);
				// スクロールさせ見えるようにする
				dataGridView3.FirstDisplayedScrollingRowIndex = addIndex;
				noCheck = false;
				// 保存するまで削除できないようにする
				button9.Enabled = false;
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 装置削除
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button9_Click(object sender, EventArgs e)
		{
			try
			{
				if (dataGridView3.Rows.Count == 0)
					return; 
				if (MessageBox.Show("選択されている装置を削除してもよろしいですか？", "登録装置削除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
				{
					// DataGridViewを削除すると選択されているインデックスが変わるで待避
					int deleteIndex = selectMachine;
					// 選択されている行の機械番号を取得
					string deleteMachine = dataGridView3.Rows[selectMachine].Cells[2].Value.ToString();
					// 選択されている行は削除
					dataGridView3.Rows.RemoveAt(selectMachine);
					// 装置台数によってはスクロールバーが表示されるのでコントロールのサイズを変える
					dataGridView3.Size = dataGridView3.RowCount < 13 ? new Size(1162, 277) : new Size(1180, 277);
					// 
					// 装置のインスタンスと表示されているコントロールを削除
					if (selectRoom == 0)
					{
						if (machineInformation[0].Count >= (deleteIndex + 1))
						{
							this.panel8.Controls.Remove(machineInformation[0][deleteIndex]);
							machineInformation[0].Remove(machineInformation[0][deleteIndex]);
						}
						// 装置が残っていれば先頭を選択状態にする
						if (machineInformation[0].Count > 0)
						{
							dataGridView3.Rows[0].Selected = true;
							selectMachine = 0;
						}
					}
					else
					{
						if (machineInformation[1].Count >= (deleteIndex + 1))
						{
							this.panel9.Controls.Remove(machineInformation[1][deleteIndex]);
							machineInformation[1].Remove(machineInformation[1][deleteIndex]);
						}
						// 装置が残っていれば先頭を選択状態にする
						if (machineInformation[1].Count > 0)
						{
							dataGridView3.Rows[0].Selected = true;
							selectMachine = 0;
						}
					}
					// 全部を読み込み、該当する機械番号の行だけを除く
					string SettingPath = AppDomain.CurrentDomain.BaseDirectory + "MachineInformation.csv";
					List<string> oldData = new List<string>();
					// csvファイルを開く
					using (FileStream fs = new FileStream(SettingPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// 文字列の読み込み
						while (!sr.EndOfStream)
						{
							string readData = sr.ReadLine();
							string[] checkstr = readData.Split(',');
							// カンマ区切りで要素番号4が機械番号に該当
							if (deleteMachine != checkstr[4])
							{
								oldData.Add(readData);
							}
						}
					}
					// 必要な部分だけを書き込み
					using (FileStream fs = new FileStream(SettingPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
					using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						foreach (var dat in oldData)
						{
							sw.WriteLine(dat);
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 装置情報保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button10_Click(object sender, EventArgs e)
		{
			try
			{
				// 装置情報を読み出して保存するラムダ式
				Action<int, int> addMachine = (index, room) =>
				{
					StringBuilder addStr = new StringBuilder();
					addStr.Append(dataGridView3.Rows[room].Cells[0].Value.ToString());
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[1].Value.ToString());
					addStr.Append(",");
					addStr.Append(index.ToString());
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[3].Value.ToString());
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[2].Value.ToString());
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[4].Value.ToString() == "しない" ? "0" : "1");
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[5].Value.ToString() == "しない" ? "0" : "1");
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[6].Value.ToString() == "しない" ? "0" : "1");
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[7].Value.ToString() == "しない" ? "0" : "1");
					addStr.Append(",");
					addStr.Append(dataGridView3.Rows[room].Cells[8].Value.ToString());
					addStr.Append(",");
					if(dataGridView3.Rows[room].Cells[9].Value != null)
						addStr.Append(dataGridView3.Rows[room].Cells[9].Value.ToString());
					addStr.Append(",");
					if(dataGridView3.Rows[room].Cells[10].Value != null)
						addStr.Append(dataGridView3.Rows[room].Cells[10].Value.ToString());
					addStr.Append(",");
					if (dataGridView3.Rows[room].Cells[11].Value != null)
						addStr.Append(dataGridView3.Rows[room].Cells[11].Value.ToString());
					addStr.Append(",0,0,100,50,0");

					Machine mc = new Machine(this, addStr.ToString());
					machineInformation[index].Add(mc);

					if (index == 0)
					{
						this.panel8.Controls.Add(machineInformation[0][machineInformation[0].Count - 1]);
					}
					else
					{
						this.panel9.Controls.Add(machineInformation[1][machineInformation[1].Count - 1]);
					}

					// 追加された装置情報をファイルに保存
					string SettingPath = AppDomain.CurrentDomain.BaseDirectory + "MachineInformation.csv";
					using (FileStream fs = new FileStream(SettingPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
					using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						sw.WriteLine(addStr.ToString());
					}
				};

				// 追加になった行のみを取り出して未入力項目がないか確認する
				for (int i = machineInformation[selectRoom].Count; i < dataGridView3.Rows.Count; i++)
				{
					// メーカ名が選択されているか確認
					if (dataGridView3.Rows[i].Cells[0].Value == null)
					{
						MessageBox.Show("設定されてない項目があります。\r\n全ての項目を入力してから追加してください。", "装置追加", MessageBoxButtons.OK, MessageBoxIcon.Error);
						return;
					}
				}

				// 装置情報が全くない場合
				if (machineInformation.Count == 0)
				{
					List<Machine> room1 = new List<Machine>();
					List<Machine> room2 = new List<Machine>();
					machineInformation.Add(room1);
					machineInformation.Add(room2);
					for (int i = 0; i < dataGridView3.Rows.Count; i++)
					{
						addMachine(selectRoom, i);
					}
				}
				// 現在の装置情報に追加する場合
				else
				{
					// 追加になった行のみを取り出して機械番号が重複してないかチェックし、重複してなければインスタンスを生成し追加する
					for (int i = machineInformation[selectRoom].Count; i < dataGridView3.Rows.Count; i++)
					{
						// LINQのAnyで同一名を検索
						if (machineInformation[selectRoom].Any(m => m.MachineNumber == dataGridView3.Rows[i].Cells[2].Value.ToString()) == true)
						{
							MessageBox.Show("機械番号が重複しています。\r\n重複しない機械番号を設定してください。", "装置情報の保存", MessageBoxButtons.OK, MessageBoxIcon.Error);
							return;
						}
						// ここまで来たら重複はないので追加する
						addMachine(selectRoom, i);
					}
				}
				// 削除できるようにする
				button9.Enabled = true;
				MessageBox.Show("機械情報を保存しました。", "装置情報の保存", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 選択日→前日の稼動状況表示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button13_Click(object sender, EventArgs e)
		{
			try
			{
				// まずは今の画面をクリア
				p2ClearOperation();
				// 表示可能な最大日数を計算
				int maxLoop = selectDay < 10 ? selectDay + 1 : 10;
				int selectIndex = selectDay;

				for (int i = 0; i < maxLoop; i++, selectIndex--)
				{
					// DateTimePickerに設定されている年と月、稼働時間に表示されている日からDateTime型を生成
					DateTime dT = new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, int.Parse(dataGridView1.Rows[selectIndex].Cells[0].Value.ToString()));
					// 生成したDateTime型からボタンに年月日を表示
					P2Button[i].Text = dT.ToLongDateString();
					// エラー情報を取得する構造体のリスト
					List<alarmInformation> errInfo = new List<alarmInformation>();
					// 稼働状況を描画
					if (machineInformation[selectRoom][selectMachine].SetOperation(ref P2Pictur[i], ref operationCanvas[i], dT, i, ref errInfo) == true)
						P2err[i] = new List<alarmInformation>(errInfo);
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 選択日→後日の稼働状況表示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button14_Click(object sender, EventArgs e)
		{
			try
			{
				// まずは今の画面をクリア
				p2ClearOperation();
				// 表示可能な最大日数を計算
				int maxLoop = (DateTime.DaysInMonth(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month) - selectDay) < 10 ? DateTime.DaysInMonth(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month) - selectDay : 10;
				int selectIndex = selectDay;
				for (int i = 0; i < maxLoop; i++, selectIndex++)
				{
					// DateTimePickerに設定されている年と月、稼働時間に表示されている日からDateTime型を生成
					DateTime dT = new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, int.Parse(dataGridView1.Rows[selectIndex].Cells[0].Value.ToString()));
					// 生成したDateTime型からボタンに年月日を表示
					P2Button[i].Text = dT.ToLongDateString();
					// エラー情報を取得する構造体のリスト
					List<alarmInformation> errInfo = new List<alarmInformation>();
					// 稼働状況を描画
					if (machineInformation[selectRoom][selectMachine].SetOperation(ref P2Pictur[i], ref operationCanvas[i], dT, i, ref errInfo) == true)
						P2err[i] = new List<alarmInformation>(errInfo);
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// アラーム情報を保存する
		/// </summary>
		/// <param name="alarm">アラーム情報のリスト</param>
		/// <param name="fileName">保存するファイル名</param>
		private void seveAlarm(List<alarmInformation> alarm, string fileName)
		{
			try
			{
				List<int> count = new List<int>();
				List<int> time = new List<int>();
				List<string> err = new List<string>();
				// Listが0になるまで同一アラームを集計する
				while (alarm.Count != 0)
				{
					// 先頭と同じアラーム名のデータを抽出
					List<alarmInformation> selectAlarm = new List<alarmInformation>(alarm.Where(a => a.errMessage == alarm[0].errMessage));
					int totalTime = 0;
					int totalCoint = 0;
					// 時間を集計する
					foreach (var alm in selectAlarm)
					{
						totalCoint++;
						totalTime += (alm.endPos - alm.startPos);
					}
					// 集計したデータをリストに追加する
					time.Add(totalTime);
					count.Add(totalCoint);
					err.Add(alarm[0].errMessage);
					// 追加したデータは元のListから削除する
					alarm.RemoveAll(a => a.errMessage == err[err.Count - 1]);
				}

				using (SaveFileDialog sfd = new SaveFileDialog())
				{
					//はじめのファイル名を指定する
					sfd.FileName = fileName + ".csv";
					//はじめに表示されるフォルダを指定する
					sfd.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory).ToString();
					//[ファイルの種類]に表示される選択肢を指定する
					sfd.Filter = saveType;
					//ダイアログを表示する
					if (sfd.ShowDialog() == DialogResult.OK)
					{
						System.Windows.Forms.Cursor.Current = Cursors.WaitCursor;
						if (sfd.FilterIndex == 1)
						{
							// CSVファイルの場合
							using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
							using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
							{
								sw.WriteLine("アラーム情報,回数,時間");
								for (int i = 0; i < count.Count; i++)
								{
									sw.WriteLine(err[i] + "," + count[i].ToString() + "," + (time[i] / 3600).ToString("00:") + ((time[i] % 3600) / 60).ToString("00:") + (time[i] % 60).ToString("00"));
								}
							}
						}
						else
						{
							// 保存するデータの配列を生成
							string[,] value = new string[count.Count + 1, 3];
							// まずは題目を入れる
							value[0, 0] = "アラーム情報";
							value[0, 1] = "回数";
							value[0, 2] = "時間";
							for (int i = 1, j = 0; j < count.Count; i++, j++)
							{
								value[i, 0] = err[j];
								value[i, 1] = count[j].ToString();
								value[i, 2] = (time[j] / 3600).ToString("00:") + ((time[j] % 3600) / 60).ToString("00:") + (time[j] % 60).ToString("00");
							}

							/***** Excelのファイル形式 *****/
							// GetTypeFromProgID関数を使用して、レジストリからExcel.Applicationオブジェクトの型を取得します。
							Type t = Type.GetTypeFromProgID("Excel.Application");
							// Excelオブジェクトのインスタンスを生成します。
							dynamic excel = Activator.CreateInstance(t);
							// Workbooksオブジェクトを取得して、Workbookオブジェクトを生成します。
							dynamic workbooks = excel.Workbooks;
							dynamic workbook = workbooks.Add();
							// Worksheetsオブジェクトを取得して、1番目のワークシートのWorksheetオブジェクトを取得します。
							dynamic worksheets = workbook.Sheets;
							dynamic worksheet = worksheets[1];
							// WorksheetオブジェクトからRangeオブジェクトを取得
							dynamic range = worksheet.Range[worksheet.Cells[1, 1], worksheet.Cells[count.Count + 1, 3]];
							// ファイルに編集中の内容を保存
							range.Value = value;
							workbook.SaveAs(sfd.FileName);
							// 使用したExcelオブジェクトは全てMarshal.ReleaseComObjectを使用して解放する必要がある
							System.Runtime.InteropServices.Marshal.ReleaseComObject(range);
							System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheet);
							System.Runtime.InteropServices.Marshal.ReleaseComObject(worksheets);
							System.Runtime.InteropServices.Marshal.ReleaseComObject(workbook);
							System.Runtime.InteropServices.Marshal.ReleaseComObject(workbooks);
							// Excel.ApplicationオブジェクトについてはQuit();も実行
							excel.Quit();
							System.Runtime.InteropServices.Marshal.ReleaseComObject(excel);
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
			System.Windows.Forms.Cursor.Current = Cursors.Default;
		}

		/// <summary>
		/// 表示月のアラーム集計
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button25_Click(object sender, EventArgs e)
		{
			try
			{
				button25.Enabled = false;
				List<alarmInformation> alarm = new List<alarmInformation>();
				// 該当する月のトータル日数を算出
				int day = DateTime.DaysInMonth(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month) + 1;
				// 1ヶ月のデータを集計
				for (int i = 1; i < day; i++)
				{
					alarm.AddRange(machineInformation[selectRoom][selectMachine].GetErrorDay(new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, i)));
				}
				if (alarm.Count != 0)
				{
					seveAlarm(alarm, dateTimePicker2.Value.ToString("yyyy") + dataGridView1.Rows[selectDay].Cells[0].Value.ToString());
				}
				else
				{
					MessageBox.Show("指定された月はアラーム情報がありません", "アラーム集計");
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
			button25.Enabled = true;
		}

		/// <summary>
		/// 表示日のアラーム集計
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button26_Click(object sender, EventArgs e)
		{
			try
			{
				button26.Enabled = false;
				List<alarmInformation> alarm = machineInformation[selectRoom][selectMachine].GetErrorDay(new DateTime(dateTimePicker2.Value.Year, dateTimePicker2.Value.Month, Convert.ToInt32(dataGridView1.Rows[selectDay].Cells[0].Value)));
				if (alarm.Count != 0)
				{
					seveAlarm(alarm, dateTimePicker2.Value.ToString("yyyyMM") + dataGridView1.Rows[selectDay].Cells[0].Value.ToString());
				}
				else
				{
					MessageBox.Show("指定された日はアラーム情報がありません", "アラーム集計");
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
			button26.Enabled = true;
		}

		/// <summary>
		/// 装置個別稼働状況 その1
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button15_Click(object sender, EventArgs e)
		{
			viewOperation(0, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その2
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button16_Click(object sender, EventArgs e)
		{
			viewOperation(1, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その3
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button17_Click(object sender, EventArgs e)
		{
			viewOperation(2, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その4
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button18_Click(object sender, EventArgs e)
		{
			viewOperation(3, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その5
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button19_Click(object sender, EventArgs e)
		{
			viewOperation(4, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その6
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button20_Click(object sender, EventArgs e)
		{
			viewOperation(5, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その7
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button21_Click(object sender, EventArgs e)
		{
			viewOperation(6, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その8
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button22_Click(object sender, EventArgs e)
		{
			viewOperation(7, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その9
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button23_Click(object sender, EventArgs e)
		{
			viewOperation(8, (Button)sender);
		}

		/// <summary>
		/// 装置個別稼働状況 その10
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button24_Click(object sender, EventArgs e)
		{
			viewOperation(9, (Button)sender);
		}

		/// <summary>
		/// 統計ボタンが押された時
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button11_Click(object sender, EventArgs e)
		{
			// 統計表示
			SetPanel(4);
		}

		/// <summary>
		/// セルの選択行が変わった
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dataGridView1_CurrentCellChanged(object sender, EventArgs e)
		{
			try
			{
				if (dataGridView1.CurrentCell != null)
				{
					if (dataGridView1.CurrentCell.RowIndex > -1)
					{
						selectDay = dataGridView1.CurrentCell.RowIndex;
						//統計が表示されていればグラフを表示
						if (panel7.Visible == true)
						{
							// 選択されている行に表示されている文字から月(日)を算出
							if (dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value != null)
							{
								int select = int.Parse(dataGridView1.Rows[dataGridView1.CurrentCell.RowIndex].Cells[0].Value.ToString());
								if (dataGridView1.Columns[0].HeaderText == "月")
								{
									monthChart(select, this.monthTime(select));
								}
								else
								{
									dayChart(select);
								}
							}
						}
						else
						{
							if (dataGridView1.Columns[0].HeaderText == "日")
								viewAlarm();
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}
		
		/// <summary>
		/// 選択されているセルが変わった
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dataGridView3_CurrentCellChanged(object sender, EventArgs e)
		{
			try
			{
				if (dataGridView3.CurrentCell != null)
				{
					if (selectMachine != dataGridView3.CurrentCell.RowIndex)
					{
						selectMachine = dataGridView3.CurrentCell.RowIndex;
						// 違う装置が選ばれたのでデータはクリアする
						p2ClearOperation();
						dataGridView1.Rows.Clear();
						selectDay = -1;
						button13.Enabled = false;
						button14.Enabled = false;
						button25.Enabled = false;
						button26.Enabled = false;
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// ボタンが押された時の処理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
		{
			// クリックされた列がボタンなら
			if (e.ColumnIndex == 8)
			{
				try
				{
					using (Form2 frm2 = new Form2())
					{
						frm2.Left = System.Windows.Forms.Cursor.Position.X - 136;
						frm2.Top = System.Windows.Forms.Cursor.Position.Y - 100;
						frm2.Size = new Size(273, 317);
						frm2.Text = "メンテナンス日";
						if (frm2.ShowDialog(this) == DialogResult.OK)
						{
							dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = frm2.SetShortDate;
						}
					}
				}
				catch (Exception exc)
				{
					SysrtmError(exc.StackTrace);
				}
			}
		}

		/// <summary>
		/// 修正後にすぐCellValueChangedイベントが発生するようにコミットする
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dataGridView3_CurrentCellDirtyStateChanged(object sender, EventArgs e)
		{
			try
			{
				dataGridView3.CommitEdit(DataGridViewDataErrorContexts.Commit);
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 設定内容が編集された時
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dataGridView3_CellValueChanged(object sender, DataGridViewCellEventArgs e)
		{
			// 起動時と表示を切り替える時は処理を行わない
			if (bootFlag == false && noCheck == false)
			{
				try
				{
					// 装置情報のインスタンス配列以上の要素番号でない事をチェックする(データグリッドビューには追加されたが変数には追加されてない時の処理)
					if (machineInformation.Count == 0 || machineInformation[selectRoom].Count <= e.RowIndex)
						return;

					// 編集された列によって処理を変える
					switch (e.ColumnIndex)
					{
						// メーカ名
						case 0:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].Maker = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].Maker = "";
							}
							break;
						// 機種名
						case 1:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].ModelNumber = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].ModelNumber = "";
							}
							break;
						// 機械番号
						case 2:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].MachineNumber = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].MachineNumber = "";
							}
							break;
						// IPアドレス
						case 3:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								IPAddress ipaddress;
								if (IPAddress.TryParse(dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString(), out ipaddress))
								{
									machineInformation[selectRoom][e.RowIndex].MachineAddress = IPAddress.Parse(dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
								}
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].MachineAddress = IPAddress.Parse("0.0.0.0");
							}
							break;
						// 監視
						case 4:
							machineInformation[selectRoom][e.RowIndex].MachineCheck = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "しない" ? false : true;
							break;
						// 緑保持
						case 5:
							machineInformation[selectRoom][e.RowIndex].GreenKeep = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "しない" ? false : true;
							break;
						// 黄保持
						case 6:
							machineInformation[selectRoom][e.RowIndex].YellowKeep = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "しない" ? false : true;
							break;
						// 赤保持
						case 7:
							machineInformation[selectRoom][e.RowIndex].RedKeep = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "しない" ? false : true;
							break;
						// メンテナンス日
						case 8:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].MaintenanceDate = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].MaintenanceDate = "";
							}
							break;
						// アラームリスト
						case 9:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].AlarmFile = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].AlarmFile = "";
							}
							break;
						// 操作リスト
						case 10:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].OperationFile = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].OperationFile = "";
							}
							break;
						// メモ
						case 11:
							if (dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value != null)
							{
								machineInformation[selectRoom][e.RowIndex].Memo = dataGridView3.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString();
							}
							else
							{
								machineInformation[selectRoom][e.RowIndex].Memo = "";
							}
							break;
					}
					Difference();
				}
				catch (Exception exc)
				{
					SysrtmError(exc.StackTrace);
				}
			}
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="index"></param>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void errorView(int index, object sender, MouseEventArgs e)
		{
			try
			{
				if (P2err[index].Count != 0)
				{
					// マウスカーソル座標がエラーリストの範囲にあるかチェック
					bool mousePos = P2err[index].Any(err => err.startPos <= e.X && err.endPos >= e.X);
					if (mousePos == true && isViewToolChip[index] == false)
					{
						isViewToolChip[index] = true;
						toolTip2.SetToolTip((PictureBox)sender, P2err[index].First(err => err.startPos <= e.X && err.endPos >= e.X).errMessage);
					}
					else
					{
						if (mousePos == false)
						{
							toolTip2.Active = false;
							toolTip2.Active = true;
						}
						isViewToolChip[index] = false;
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(0, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(1, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(2, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox4_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(3, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox5_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(4, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox6_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(5, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox7_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(6, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox8_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(7, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox9_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(8, sender, e);
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void pictureBox10_MouseMove(object sender, MouseEventArgs e)
		{
			errorView(9, sender, e);
		}

		/// <summary>
		/// 基準となる時間の数字を描画
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel2_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				Point timePos = new Point(116, 352);
				using (SolidBrush blue = new SolidBrush(Color.Blue))
				using (SolidBrush red = new SolidBrush(Color.Red))
				{
					for (int i = 0; i < 5; i++)
					{
						for (int j = 0; j < 25; j++)
						{
							e.Graphics.DrawString(j.ToString(), this.Font, j > 17 ? blue : j > 7 ? red : blue, timePos);
							timePos.X += j == 9 ? 56 : 60;
						}
						timePos.X = 116;
						timePos.Y += 144;
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		#endregion 加工機情報パネル -------------------------------------------------------------------------------------------------


		#region 加工機稼動推移パネル ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 装置別稼動推移を表示する
		/// </summary>
		private void p3View()
		{
			this.SuspendLayout();
			try
			{
				// まずは今のビットマップをクリアする
				if (P3Picture != null)
				{
					for (int i = 0; i < P3Picture.Length; i++)
					{
						P3Picture[i].Image = null;
						if (P3Bitmap[i] != null)
							P3Bitmap[i].Dispose();
						// 一度コントロールを削除する
						panel3.Controls.Remove(P3Picture[i]);
					}
				}
				// エラー情報もクリア
				P3err.Clear();
				int loopCount = machineInformation[radioButton3.Checked == true ? 0 : 1].Count;
				isP3ToolChip = new bool[loopCount];
				P3Picture = new PictureBox[loopCount];
				P3Bitmap = new Bitmap[loopCount];
				// 各コントロールの初期座標
				Point pixturePos = new Point(88, 72);
				// 機械の台数分だけコントロールを生成する
				for (int i = 0; i < loopCount; i++, pixturePos.Y += 72)
				{
					isP3ToolChip[i] = false;
					// 稼働状況PixtureBoxの設定
					P3Picture[i] = new PictureBox();
					P3Picture[i].Name = i.ToString();
					P3Picture[i].BorderStyle = BorderStyle.FixedSingle;
					P3Picture[i].Location = pixturePos;
					P3Picture[i].Size = new Size(1442, 50);
					P3Picture[i].MouseMove += new MouseEventHandler(picture_MouseMove);
					panel3.Controls.Add(P3Picture[i]);
					// チェックボックスの状態によって表示する日を変える
					DateTime dt = checkBox3.Checked == false ? dateTimePicker1.Value : DateTime.Now;
					// エラー情報用リストの生成
					P3err.Add(new List<alarmInformation>());
					List<alarmInformation> err = new List<alarmInformation>();
					if (machineInformation[radioButton3.Checked == true ? 0 : 1][i].SetOperation(ref P3Picture[i], ref P3Bitmap[i], dt, i, ref err) == true)
						P3err[i] = err;
				}
				// リアルタイム表示がオンならタイマを動作して1分ごとに更新
				if (checkBox3.Checked == true)
				{
					timer2.Interval = 60000 - DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
					timer2.Start();
				}
				else
				{
					timer2.Stop();
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
			this.ResumeLayout();
		}

		/// <summary>
		/// 基準となる時間の数字と機械番号を描画
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void panel3_Paint(object sender, PaintEventArgs e)
		{
			try
			{
				if (bootFlag == false)
				{
					int index = radioButton3.Checked == true ? 0 : 1;
					if (machineInformation[index].Count != 0)
					{
						// 時間の数字描画
						int loopCount = machineInformation[index].Count / 2 + machineInformation[index].Count % 2;
						Point timePos = new Point(84, 128 + panel3.AutoScrollPosition.Y);
						using (SolidBrush blue = new SolidBrush(Color.Blue))
						using (SolidBrush red = new SolidBrush(Color.Red))
						{
							for (int i = 0; i < loopCount; i++)
							{
								for (int j = 0; j < 25; j++)
								{
									e.Graphics.DrawString(j.ToString(), this.Font, j > 17 ? blue : j > 7 ? red : blue, timePos);
									timePos.X += j == 9 ? 56 : 60;
								}
								timePos.X = 84;
								timePos.Y += 144;
							}
						}
						// 機械番号描画
						loopCount = machineInformation[index].Count;
						timePos = new Point(24, 91 + panel3.AutoScrollPosition.Y);
						using (SolidBrush sb = new SolidBrush(Color.Black))
						{
							for (int i = 0; i < loopCount; i++, timePos.Y += 72)
							{
								e.Graphics.DrawString(machineInformation[index][i].MachineNumber, this.Font, sb, timePos);
							}
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// エリア1が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void radioButton3_CheckedChanged(object sender, EventArgs e)
		{
			if (bootFlag == false)
			{
				if (radioButton3.Checked == true)
				{
					p3View();
					panel3.Refresh();
				}
			}
		}

		/// <summary>
		/// エリア2が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void radioButton4_CheckedChanged(object sender, EventArgs e)
		{
			if (bootFlag == false)
			{
				if (radioButton4.Checked == true)
				{
					p3View();
					panel3.Refresh();
				}
			}
		}

		/// <summary>
		/// リアルタイム表示か否かの設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox3_CheckedChanged(object sender, EventArgs e)
		{
			dateTimePicker1.Enabled = !checkBox3.Checked;
			p3View();
		}

		/// <summary>
		/// 日にちが設定された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dateTimePicker1_ValueChanged(object sender, EventArgs e)
		{
			if (bootFlag == false)
			{
				p3View();
			}
		}

		/// <summary>
		/// ピクチャーボックス上のエラー部にマウスカーソルがある場合はバルーンでエラー内容を表示する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void picture_MouseMove(object sender, MouseEventArgs e)
		{
			try
			{
				// パネルの名前は0～となっているのでそれを要素番号にする
				int index = int.Parse(((PictureBox)sender).Name.ToString());

				// マウスカーソル座標がエラーリストの範囲にあるかチェック
				bool mousePos = P3err[index].Any(err => err.startPos <= e.X && err.endPos >= e.X);
				if (mousePos == true && isP3ToolChip[index] == false)
				{
					isP3ToolChip[index] = true;
					toolTip2.SetToolTip((PictureBox)sender, P3err[index].First(err => err.startPos <= e.X && err.endPos >= e.X).errMessage);
				}
				else
				{
					if (mousePos == false)
					{
						toolTip2.Active = false;
						toolTip2.Active = true;
					}
					isP3ToolChip[index] = false;
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}
		

		#endregion 加工機稼動推移パネル -------------------------------------------------------------------------------------------------


		#region 設定パネル ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 設定をデフォルト値に戻す
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button12_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.Reset();
			// 設定画面の凡例を戻す
			label1.ForeColor = Properties.Settings.Default.Panel1GreenForeColor;
			label1.BackColor = Properties.Settings.Default.Panel1GreenBackColor;
			label1.Text = Properties.Settings.Default.Panel1GreenName;
			label2.ForeColor = Properties.Settings.Default.Panel1YellowForeColor;
			label2.BackColor = Properties.Settings.Default.Panel1YellowBackColor;
			label2.Text = Properties.Settings.Default.Panel1YellowName;
			label3.ForeColor = Properties.Settings.Default.Panel1RedForeColor;
			label3.BackColor = Properties.Settings.Default.Panel1RedBackColor;
			label3.Text = Properties.Settings.Default.Panel1RedName;
			label4.ForeColor = Properties.Settings.Default.Panel1EraseForeColor;
			label4.BackColor = Properties.Settings.Default.Panel1EraseBackColor;
			label4.Text = Properties.Settings.Default.Panel1EraseName;
			label5.ForeColor = Properties.Settings.Default.Panel1CutForeColor;
			label5.BackColor = Properties.Settings.Default.Panel1CutBackColor;
			label5.Text = Properties.Settings.Default.Panel1CutName;
			label6.ForeColor = Properties.Settings.Default.Panel1DontForeColor;
			label6.BackColor = Properties.Settings.Default.Panel1DontBackColor;
			label6.Text = Properties.Settings.Default.Panel1DontName;
			label133.ForeColor = Properties.Settings.Default.Panel1StopForeColor;
			label133.BackColor = Properties.Settings.Default.Panel1StopBackColor;
			label133.Text = Properties.Settings.Default.Panel1StopName;

			label134.ForeColor = Properties.Settings.Default.Panel2GreenForeColor;
			label134.BackColor = Properties.Settings.Default.Panel2GreenBackColor;
			label134.Text = Properties.Settings.Default.Panel2GreenName;
			label135.ForeColor = Properties.Settings.Default.Panel2YellowForeColor;
			label135.BackColor = Properties.Settings.Default.Panel2YellowBackColor;
			label135.Text = Properties.Settings.Default.Panel2YellowName;
			label136.ForeColor = Properties.Settings.Default.Panel2RedForeColor;
			label136.BackColor = Properties.Settings.Default.Panel2RedBackColor;
			label136.Text = Properties.Settings.Default.Panel2RedName;
			label137.ForeColor = Properties.Settings.Default.Panel2EraseForeColor;
			label137.BackColor = Properties.Settings.Default.Panel2EraseBackColor;
			label137.Text = Properties.Settings.Default.Panel2EraseName;
			label138.ForeColor = Properties.Settings.Default.Panel2CutForeColor;
			label138.BackColor = Properties.Settings.Default.Panel2CutBackColor;
			label138.Text = Properties.Settings.Default.Panel2CutName;
			label139.ForeColor = Properties.Settings.Default.Panel2DontForeColor;
			label139.BackColor = Properties.Settings.Default.Panel2DontBackColor;
			label139.Text = Properties.Settings.Default.Panel2DontName;

			checkBox1.Checked = Properties.Settings.Default.IsGridSnap;
			checkBox2.Checked = Properties.Settings.Default.IsListData;
			checkBox4.Checked = Properties.Settings.Default.IsViewErrir;
			checkBox5.Checked = Properties.Settings.Default.IsViewOccupancy;
			checkBox6.Checked = Properties.Settings.Default.IsViewMaintenance;
			checkBox7.Checked = Properties.Settings.Default.IsViewMemo;

			textBox10.Text = Properties.Settings.Default.Area1;
			textBox11.Text = Properties.Settings.Default.Area2;

			textBox9.Text = ((double)Properties.Settings.Default.RegularlyTime / 1000).ToString();
		}

		/// <summary>
		/// 例外エラークリア
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button33_Click(object sender, EventArgs e)
		{
			label8.Visible = false;
		}

		/// <summary>
		/// 保存ボタン
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button34_Click(object sender, EventArgs e)
		{
			Properties.Settings.Default.RegularlyTime = (int)(double.Parse(textBox9.Text) * 1000);
			Properties.Settings.Default.Area1 = textBox10.Text;
			Properties.Settings.Default.Area2 = textBox11.Text;
			Properties.Settings.Default.Save();
			areaName();
		}

		/// <summary>
		/// レイアウトのグリッドスナップの設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox1_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsGridSnap = checkBox1.Checked;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// リスト送信の設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox2_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsListData = checkBox2.Checked;
			Properties.Settings.Default.Save();
			groupBox1.Enabled = checkBox2.Checked;
			groupBox2.Enabled = checkBox2.Checked;
			// リスト送信が有効になったら一度全部送信する
			if (checkBox2.Checked == true)
			{
				Task.Run(() =>
				{
					foreach (var mc in machineInformation)
					{
						foreach (var m in mc)
						{
							m.setData(true);
						}
					}
				});
			}
		}

		/// <summary>
		/// エラー回数表示の設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox4_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsViewErrir = checkBox4.Checked;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// 稼働率表示の設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox5_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsViewOccupancy = checkBox5.Checked;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// メンテナンス日表示の設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox6_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsViewMaintenance = checkBox6.Checked;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// メモ表示の設定
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkBox7_CheckedChanged(object sender, EventArgs e)
		{
			Properties.Settings.Default.IsViewMemo = checkBox7.Checked;
			Properties.Settings.Default.Save();
		}

		/// <summary>
		/// カラーダイアログを開いて色を設定する
		/// </summary>
		/// <param name="defaltColor"></param>
		/// <returns></returns>
		private Color setColor(Color defaltColor)
		{
			List<RGB> colorList = new List<RGB>();
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

			RGB rgb = new RGB(defaltColor.R, defaltColor.G, defaltColor.B);
			using (ColorDialog cd = new ColorDialog())
			{
				// 標準色でなければカスタムカラーの領域へ表示
				if (colorList.Any(c => c.Red == rgb.Red && c.Green == rgb.Green && c.Blue == rgb.Blue) == false)
					cd.CustomColors = new int[] { defaltColor.R << 16 | defaltColor.G << 8 | defaltColor.B };
				cd.Color = defaltColor;
				return cd.ShowDialog() == DialogResult.OK ? cd.Color : defaltColor;
			}
		}

		/// <summary>
		///	稼働状況の文字を変えるコンテキストメニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 文字の変更ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				Control source = contextMenuStrip1.SourceControl;

				if (source != null)
				{
					string newName;

					using (Form5 f5 = new Form5(source.Text))
					{
						f5.Left = System.Windows.Forms.Cursor.Position.X - 81;
						f5.Top = System.Windows.Forms.Cursor.Position.Y - 60;
						f5.ShowDialog(this);
						newName = f5.newName;
					}

					// 選択されたコントロールの文字を設定された文字にする
					switch (source.Name)
					{
						case "label1":
							label1.Text = Properties.Settings.Default.Panel1GreenName = newName;
							break;
						case "label2":
							label2.Text = Properties.Settings.Default.Panel1YellowName = newName;
							break;
						case "label3":
							label3.Text = Properties.Settings.Default.Panel1RedName = newName;
							break;
						case "label4":
							label4.Text = Properties.Settings.Default.Panel1EraseName = newName;
							break;
						case "label5":
							label5.Text = Properties.Settings.Default.Panel1CutName = newName;
							break;
						case "label6":
							label6.Text = Properties.Settings.Default.Panel1DontName = newName;
							break;
						case "label133":
							label133.Text = Properties.Settings.Default.Panel1StopName = newName;
							break;
					}
					Properties.Settings.Default.Save();
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 稼働状況の文字色を変えるコンテキストメニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 文字色の変更ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				Control source = contextMenuStrip1.SourceControl;
				if (source != null)
				{
					// 選択されたコントロールの文字色を選択されている色にする
					switch (source.Name)
					{
						case "label1":
							label1.ForeColor = Properties.Settings.Default.Panel1GreenForeColor = setColor(Properties.Settings.Default.Panel1GreenForeColor);
							break;
						case "label2":
							label2.ForeColor = Properties.Settings.Default.Panel1YellowForeColor = setColor(Properties.Settings.Default.Panel1YellowForeColor);
							break;
						case "label3":
							label3.ForeColor = Properties.Settings.Default.Panel1RedForeColor = setColor(Properties.Settings.Default.Panel1RedForeColor);
							break;
						case "label4":
							label4.ForeColor = Properties.Settings.Default.Panel1EraseForeColor = setColor(Properties.Settings.Default.Panel1EraseForeColor);
							break;
						case "label5":
							label5.ForeColor = Properties.Settings.Default.Panel1CutForeColor = setColor(Properties.Settings.Default.Panel1CutForeColor);
							break;
						case "label6":
							label6.ForeColor = Properties.Settings.Default.Panel1DontForeColor = setColor(Properties.Settings.Default.Panel1DontForeColor);
							break;
						case "label133":
							label133.ForeColor = Properties.Settings.Default.Panel1StopForeColor = setColor(Properties.Settings.Default.Panel1StopForeColor);
							break;
					}
						
					Properties.Settings.Default.Save();
					// 装置オブジェクトの色設定を更新する
					foreach (var mc in machineInformation)
					{
						foreach (var m in mc)
						{
							m.SetColor();
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 稼働状況の背景色を変えるコンテキストメニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 背景色の変更ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			try
			{
				Control source = contextMenuStrip1.SourceControl;
				if (source != null)
				{
					// 選択されたコントロールの背景色を選択されている色にする
					switch (source.Name)
					{
						case "label1":
							label1.BackColor = Properties.Settings.Default.Panel1GreenBackColor = setColor(Properties.Settings.Default.Panel1GreenBackColor);
							break;
						case "label2":
							label2.BackColor = Properties.Settings.Default.Panel1YellowBackColor = setColor(Properties.Settings.Default.Panel1YellowBackColor);
							break;
						case "label3":
							label3.BackColor = Properties.Settings.Default.Panel1RedBackColor = setColor(Properties.Settings.Default.Panel1RedBackColor);
							break;
						case "label4":
							label4.BackColor = Properties.Settings.Default.Panel1EraseBackColor = setColor(Properties.Settings.Default.Panel1EraseBackColor);
							break;
						case "label5":
							label5.BackColor = Properties.Settings.Default.Panel1CutBackColor = setColor(Properties.Settings.Default.Panel1CutBackColor);
							break;
						case "label6":
							label6.BackColor = Properties.Settings.Default.Panel1DontBackColor = setColor(Properties.Settings.Default.Panel1DontBackColor);
							break;
						case "label133":
							label133.BackColor = Properties.Settings.Default.Panel1StopBackColor = setColor(Properties.Settings.Default.Panel1StopBackColor);
							break;
					}
					Properties.Settings.Default.Save();
					// 装置オブジェクトの色設定を更新する
					foreach (var mc in machineInformation)
					{
						foreach (var m in mc)
						{
							m.SetColor();
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 加工機情報、稼動推移の文字を変えるコンテキストメニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 文字色の変更ToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			try
			{
				Control source = contextMenuStrip2.SourceControl;
				if (source != null)
				{
					// 選択されたコントロールの背景色を選択されている色にする
					switch (source.Name)
					{
						case "label134":
							label134.ForeColor = Properties.Settings.Default.Panel2GreenForeColor = setColor(Properties.Settings.Default.Panel2GreenForeColor);
							break;
						case "label135":
							label135.ForeColor = Properties.Settings.Default.Panel2YellowForeColor = setColor(Properties.Settings.Default.Panel2YellowForeColor);
							break;
						case "label136":
							label136.ForeColor = Properties.Settings.Default.Panel2RedForeColor = setColor(Properties.Settings.Default.Panel2RedForeColor);
							break;
						case "label137":
							label137.ForeColor = Properties.Settings.Default.Panel2EraseForeColor = setColor(Properties.Settings.Default.Panel2EraseForeColor);
							break;
						case "label138":
							label138.ForeColor = Properties.Settings.Default.Panel2CutForeColor = setColor(Properties.Settings.Default.Panel2CutForeColor);
							break;
						case "label139":
							label139.ForeColor = Properties.Settings.Default.Panel2DontForeColor = setColor(Properties.Settings.Default.Panel2DontForeColor);
							break;
					}
					Properties.Settings.Default.Save();
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 加工機情報、稼動推移の設定を変えるコンテキストメニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 背景色の変更ToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			try
			{
				Control source = contextMenuStrip2.SourceControl;
				if (source != null)
				{
					// 選択されたコントロールの背景色を選択されている色にする
					switch (source.Name)
					{
						case "label134":
							label134.BackColor = Properties.Settings.Default.Panel2GreenBackColor = setColor(Properties.Settings.Default.Panel2GreenBackColor);
							break;
						case "label135":
							label135.BackColor = Properties.Settings.Default.Panel2YellowBackColor = setColor(Properties.Settings.Default.Panel2YellowBackColor);
							break;
						case "label136":
							label136.BackColor = Properties.Settings.Default.Panel2RedBackColor = setColor(Properties.Settings.Default.Panel2RedBackColor);
							break;
						case "label137":
							label137.BackColor = Properties.Settings.Default.Panel2EraseBackColor = setColor(Properties.Settings.Default.Panel2EraseBackColor);
							break;
						case "label138":
							label138.BackColor = Properties.Settings.Default.Panel2CutBackColor = setColor(Properties.Settings.Default.Panel2CutBackColor);
							break;
						case "label139":
							label139.BackColor = Properties.Settings.Default.Panel2DontBackColor = setColor(Properties.Settings.Default.Panel2DontBackColor);
							break;
					}
					Properties.Settings.Default.Save();
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 加工機情報、稼動推移の文字を変えるコンテキストメニュー
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 文字の変更ToolStripMenuItem_Click_1(object sender, EventArgs e)
		{
			try
			{
				Control source = contextMenuStrip2.SourceControl;

				if (source != null)
				{
					string newName;

					using (Form5 f5 = new Form5(source.Text))
					{
						f5.Left = System.Windows.Forms.Cursor.Position.X - 81;
						f5.Top = System.Windows.Forms.Cursor.Position.Y - 60;
						f5.ShowDialog(this);
						newName = f5.newName;
					}

					// 選択されたコントロールの背景色を選択されている色にする
					switch (source.Name)
					{
						case "label134":
							label134.Text = Properties.Settings.Default.Panel2GreenName = newName;
							break;
						case "label135":
							label135.Text = Properties.Settings.Default.Panel2YellowName = newName;
							break;
						case "label136":
							label136.Text = Properties.Settings.Default.Panel2RedName = newName;
							break;
						case "label137":
							label137.Text = Properties.Settings.Default.Panel2EraseName = newName;
							break;
						case "label138":
							label138.Text = Properties.Settings.Default.Panel2CutName = newName;
							break;
						case "label139":
							label139.Text = Properties.Settings.Default.Panel2DontName = newName;
							break;
					}

					Properties.Settings.Default.Save();
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 設定画面に領域設定のリストを表示
		/// </summary>
		/// <param name="index">選択状態にするインデックス</param>
		private void viewArea(int index = 0)
		{
			try
			{
				dataGridView4.Rows.Clear();
				List<Area> areaRoom = new List<Area>(radioButton5.Checked == true ? areaRoom1 : areaRoom2);
				if (areaRoom.Count == 0)
				{
					button27.Enabled = false;
					button28.Enabled = false;
					selectArraIndex = -1;
					return;
				}
				button27.Enabled = true;
				button28.Enabled = true;
				for (int i = 0; i < areaRoom.Count; i++)
				{
					dataGridView4.Rows.Add();
					dataGridView4.Rows[i].Cells[0].Value = areaRoom[i].Title;
					dataGridView4.Rows[i].Cells[1].Value = areaRoom[i].Rect.X.ToString();
					dataGridView4.Rows[i].Cells[2].Value = areaRoom[i].Rect.Y.ToString();
					dataGridView4.Rows[i].Cells[3].Value = areaRoom[i].Rect.Width.ToString();
					dataGridView4.Rows[i].Cells[4].Value = areaRoom[i].Rect.Height.ToString();
					dataGridView4.Rows[i].Cells[5].Style.BackColor = areaRoom[i].LineColor;
					dataGridView4.Rows[i].Cells[5].Style.SelectionBackColor = areaRoom[i].LineColor;
					dataGridView4.Rows[i].Cells[6].Style.BackColor = areaRoom[i].FillColor;
					dataGridView4.Rows[i].Cells[6].Style.SelectionBackColor = areaRoom[i].FillColor;
				}
				dataGridView4.Rows[index].Selected = true;
				selectArraIndex = index;
				if (areaRoom.Count < 8)
					dataGridView4.Size = new Size(462, 172);
				else
					dataGridView4.Size = new Size(480, 172);
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 領域設定のセルがクリックされた
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void dataGridView4_CellClick(object sender, DataGridViewCellEventArgs e)
		{
			selectArraIndex = e.RowIndex;
		}

		/// <summary>
		/// 領域設定にエリア1が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void radioButton5_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButton5.Checked == true)
				this.viewArea();
		}

		/// <summary>
		/// 領域設定にエリア2が選択された
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void radioButton6_CheckedChanged(object sender, EventArgs e)
		{
			if (radioButton6.Checked == true)
				this.viewArea();
		}

		/// <summary>
		/// 領域設定編集
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button27_Click(object sender, EventArgs e)
		{
			try
			{
				Area area;
				if (radioButton5.Checked == true)
					area = new Area(areaRoom1[selectArraIndex]);
				else
					area = new Area(areaRoom2[selectArraIndex]);
				using (Form4 f = new Form4(area))
				{
					f.Left = System.Windows.Forms.Cursor.Position.X - 300;
					f.Top = System.Windows.Forms.Cursor.Position.Y - 80;
					f.Size = new Size(443, 253);
					SetPanel(0);
					// OKならリストを編集して再描画
					if (f.ShowDialog(this) == DialogResult.OK)
					{
						if (radioButton5.Checked == true)
							areaRoom1[selectArraIndex] = f.area;
						else
							areaRoom2[selectArraIndex] = f.area;
						this.saveArea();
						this.viewArea();
					}
					SetPanel(3);
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 領域設定削除
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button28_Click(object sender, EventArgs e)
		{
			try
			{
				if (MessageBox.Show("削除した領域設定は復元できません。\r\n削除してもよろしいですか？", "領域設定の削除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
				{
					if (radioButton5.Checked == true)
					{
						areaRoom1.RemoveAt(selectArraIndex);
						if (areaRoom1.Count > 0)
						{
							if (selectArraIndex > 0)
							{
								selectArraIndex--;
								dataGridView4.Rows[selectArraIndex].Selected = true;
							}
						}
					}
					else
					{
						areaRoom2.RemoveAt(selectArraIndex);
						if (areaRoom2.Count > 0)
						{
							selectArraIndex--;
							if (selectArraIndex < 0)
								selectArraIndex = 0;
							dataGridView4.Rows[selectArraIndex].Selected = true;
						}
					}
					this.viewArea(selectArraIndex);
					this.saveArea();
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// アラーム情報上書き保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button29_Click(object sender, EventArgs e)
		{
			try
			{
				string filenama = AppDomain.CurrentDomain.BaseDirectory + comboBox1.Text;
				using (FileStream fs = new FileStream(filenama, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					sw.WriteLine(textBox1.Text);
					// 余分な改行コードを削除して保存
					sw.WriteLine(textBox2.Text.TrimEnd('\r', '\n'));
				}
				// 今回保存したファイルを使用している装置のアラーム更新フラグをセットする
				foreach (var mc in machineInformation)
				{
					foreach (var m in mc)
					{
						if (m.AlarmFile == comboBox1.Text)
						{
							m.UpdateSetting(true);
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// アラーム情報を名前を付けて保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button30_Click(object sender, EventArgs e)
		{
			try
			{
				using (SaveFileDialog sfd = new SaveFileDialog())
				{
					//はじめに「ファイル名」で表示される文字列を指定する
					sfd.FileName = "新しいファイル.alm";
					//はじめに表示されるフォルダを指定する
					sfd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
					//タイトルを設定する
					//sfd.Title = "保存先のファイルを選択してください";
					sfd.Filter = "アラーム情報ファイル(*.alm)|*.alm";
					//ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
					sfd.RestoreDirectory = true;
					//ダイアログを表示する
					if (sfd.ShowDialog() == DialogResult.OK)
					{
						using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
						using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
						{
							sw.WriteLine(textBox1.Text);
							// 余分な改行コードを削除して保存
							sw.WriteLine(textBox2.Text.TrimEnd('\r', '\n'));
						}
						// 存在しているファイル名と一致してなければ追加
						string filename = Path.GetFileName(sfd.FileName);
						int findindex = comboBox1.FindString(filename);
						if (findindex == -1)
						{
							comboBox1.Items.Add(filename);
							Column28.Items.Add(filename);
							comboBox1.SelectedIndex = comboBox1.Items.Count - 1;
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 操作リストの上書き保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button31_Click(object sender, EventArgs e)
		{
			try
			{
				string filenama = AppDomain.CurrentDomain.BaseDirectory + comboBox2.Text;
				using (FileStream fs = new FileStream(filenama, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					for (int i = 0; i < OperationText.Length; i++)
					{
						if(OperationText[i].Text != "")
							sw.WriteLine(OperationText[i].Text);
					}	
				}
				// 今回保存したファイルを使用している装置の操作更新フラグをセットする
				foreach (var mc in machineInformation)
				{
					foreach (var m in mc)
					{
						if (m.OperationFile == comboBox2.Text)
						{
							m.UpdateSetting(true);
						}
					}
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 操作リストを名前をつけて保存
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void button32_Click(object sender, EventArgs e)
		{
			try
			{
				using (SaveFileDialog sfd = new SaveFileDialog())
				{
					//はじめに「ファイル名」で表示される文字列を指定する
					sfd.FileName = "新しいファイル.ope";
					//はじめに表示されるフォルダを指定する
					sfd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;
					//タイトルを設定する
					//sfd.Title = "保存先のファイルを選択してください";
					sfd.Filter = "操作情報ファイル(*.ope)|*.ope";
					//ダイアログボックスを閉じる前に現在のディレクトリを復元するようにする
					sfd.RestoreDirectory = true;
					//ダイアログを表示する
					if (sfd.ShowDialog() == DialogResult.OK)
					{
						using (FileStream fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
						using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
						{
							for (int i = 0; i < OperationText.Length; i++)
							{
								if (OperationText[i].Text != "")
									sw.WriteLine(OperationText[i].Text);
							}
						}
						// 存在しているファイル名と一致してなければ追加
						string filename = Path.GetFileName(sfd.FileName);
						int findindex = comboBox1.FindString(filename);
						if (findindex == -1)
						{
							comboBox2.Items.Add(filename);
							Column29.Items.Add(filename);
							comboBox2.SelectedIndex = comboBox2.Items.Count - 1;
						}
					}	
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 選択されているファイルが変わった
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				string[] almstr;
				string path = AppDomain.CurrentDomain.BaseDirectory + comboBox1.Text;
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					// 文字列を改行で区切り配列に入れる(空の文字列を含む配列要素は除く)
					almstr = sr.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				}
				if (almstr.Length != 0)
				{
					textBox1.Text = almstr[0];
					textBox2.Clear();
					for (int i = 1; i < almstr.Length; i++)
						textBox2.AppendText(almstr[i] + "\r\n");
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 操作リストで選択されているファイルが変わった
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
		{
			try
			{
				string[] opestr;
				string path = AppDomain.CurrentDomain.BaseDirectory + comboBox2.Text;
				using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					// 文字列を改行で区切り配列に入れる(空の文字列を含む配列要素は除く)
					opestr = sr.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
				}
				if (opestr.Length != 0)
				{
					for (int i = 0; i < OperationText.Length; i++)
						OperationText[i].Clear();

					for (int i = 0; i < opestr.Length; i++)
						OperationText[i].Text = opestr[i];
				}
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		#endregion 設定パネル -------------------------------------------------------------------------------------------------


		#region タイマー記述 ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 保存を促す点灯
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer1_tick(object sender, System.Timers.ElapsedEventArgs e)
		{
			BeginInvoke((Action)(() =>
			{
				button7.BackColor = button7.BackColor == Color.Red ? System.Drawing.SystemColors.Control : Color.Red;
			}));
		}

		/// <summary>
		/// 装置別稼動状況を定期的に更新するタイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer2_tick(object sender, System.Timers.ElapsedEventArgs e)
		{
			timer2.Stop();
			BeginInvoke((Action)(() =>
			{
				try
				{
					// エラー情報をクリア
					P3err.Clear();
					int loopCount = machineInformation[radioButton3.Checked == true ? 0 : 1].Count;
					for (int i = 0; i < loopCount; i++)
					{
						// エラー情報用リストの生成
						P3err.Add(new List<alarmInformation>());
						List<alarmInformation> err = new List<alarmInformation>();
						// 稼動状況表示
						if (machineInformation[radioButton3.Checked == true ? 0 : 1][i].SetOperation(ref P3Picture[i], ref P3Bitmap[i], DateTime.Now, i, ref err) == true)
						{
							if(err.Count != 0)
								P3err[i] = err;
						}
					}
				}
				catch (Exception exc)
				{
					SysrtmError(exc.StackTrace);
				}
			}));
			if (viewPanel == 2 && checkBox3.Checked == true)
			{
				timer2.Interval = 60000 - DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
				timer2.Start();
			}
		}

		/// <summary>
		/// 時刻を表示するタイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer3_tick(object sender, System.Timers.ElapsedEventArgs e)
		{
			timer3.Stop();
			BeginInvoke((Action)(() =>
			{
				label156.Text = DateTime.Now.ToString("yyyy年MM月dd日 (ddd) H時mm分");
			}));
			// 次に「分」が変化するタイミング(誤差で変わらないかもしれないので余裕を見て+100msec)がインターバル時間
			timer3.Interval = 60100 - DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
			timer3.Start();
		}

		/// <summary>
		/// 日付が変わるタイミングを監視するタイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void timer4_tick(object sender, System.Timers.ElapsedEventArgs e)
		{
			// 次のターゲットとなる時間をセット(誤差で日付が変わってなければ微小な値がセットされる)
			timer4.Interval = 86400000 - (e.SignalTime.Hour * 3600000 + e.SignalTime.Minute * 60000 + e.SignalTime.Second * 1000 + e.SignalTime.Millisecond);
			// 日にちが変わっていたらDateTimePickerに新しい日をセットする
			if (oldDay != e.SignalTime.Day)
			{
				oldDay = e.SignalTime.Day;
				BeginInvoke((Action)(() =>
				{
					dateTimePicker1.Value = DateTime.Now;
					dateTimePicker2.Value = DateTime.Now;
				}));
			}
		}

		#endregion タイマー記述 -------------------------------------------------------------------------------------------------

		#region internalなメソッド ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
		/// <summary>
		///  システムエラーログ
		/// </summary>
		/// <param name="message">メソッド名(GetCurrentMethodの戻り値)</param>
		/// <param name="addstring">エラー名称</param>
		/// <param name="view">例外を表示するかの指示 false=表示しない</param>
		internal void SysrtmError(string message, string addstring = null, bool view = true)
		{
			try
			{
				BeginInvoke((Action)(() =>
				{
					StringBuilder writeStr = new StringBuilder(DateTime.Now.ToString("yyyy/M/d H:m:s,")).Append(message.Replace("\r\n", " ")).Append(addstring);
					// ファイル名を生成
					string SettingPath = AppDomain.CurrentDomain.BaseDirectory + "SysrtemError.csv";
					// システムエラーログ書き込み
					using (FileStream fs = new FileStream(SettingPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
					using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						sw.WriteLine(writeStr.ToString());
					}
					if(view == true)
						label8.Visible = true;
				}));
			}
			catch (Exception exc)
			{
				SysrtmError(exc.StackTrace);
			}
		}

		#endregion internalなメソッド -------------------------------------------------------------------------------------------------

	}
}
