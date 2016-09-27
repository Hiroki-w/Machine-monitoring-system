using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 装置監視システム
{
	/// <summary>
	///  操作情報の構造体
	/// </summary>
	internal struct operationInformation
	{
		/// <summary>
		/// 操作内容
		/// </summary>
		internal string operationName;
		/// <summary>
		/// 発生時刻
		/// </summary>
		internal TimeSpan operationTime;
		/// <summary>
		/// 状態
		/// </summary>
		internal bool operationState;
	}

	public partial class Machine : UserControl
	{

		// マウスでコントロールをドラッグ＆ドロップする時に必要な変数
		private int x;
		private int y;
		private bool isDraggable = false;
		private bool isSize = false;
		private bool snap = false;
		private int mouseX;
		private int mouseY;
		private int endMouseX;
		private int endMouseY;
		// グリッドから離れてすぐスナップするのを抑制させるフラグ
		private bool mouseWait = false;

		// 装置情報
		private string maker;
		private string modelNumber;
		private string machineNumber;
		private string maintenanceDate;
		private string memo;
		private string alarmFile = "";
		private string operationFile = "";

		/***** ネットワーク関連 *****/
		private IPAddress ipaddress = new IPAddress(0);
		private Socket socket;
		private bool isConnect = false;
		/// <summary>
		/// ネットワーク切断が任意かを示す true=任意な切断
		/// </summary>
		private bool userClose = false;
		readonly private int port = 50622;
		private object Lock = new object();
		private ManualResetEvent connectEvent = new ManualResetEvent(false);

		// 変更が発生したかを判断するために保存しておく初期値
		private bool diffmachineCheck;
		private string diffMaker;
		private string diffModelNumber;
		private string diffMachineNumber;
		private string diffMaintenanceDate;
		private string diffMemo;
		private IPAddress diffMachineAddress;
		private bool diffGreenKeep;
		private bool diffYellowKeep;
		private bool diffRedKeep;
		private Point diffPos;
		private int diffZoom;
		private Size diffSize;
		private string diffAlarmFile;
		private string diffOperationFile;

		// 監視するか否かを記憶しておく変数
		private bool machineCheck = false;
		/// <summary>
		/// ライトの状態を記憶しておく変数
		/// 0=緑 1=黄 2=赤 3=消 4=通信エラー 5=監視しない
		/// </summary>
		private int lightState = -1;
		// タッチパネルへ保持している点灯を消灯に変える要求フラグ
		private bool isErase = false;
		// 緑を保持するか記憶しておく変数
		private bool greenKeep = false;
		// 黄を保持するか記憶しておく変数
		private bool yellowKeep = false;
		// 赤を保持するか記憶しておく変数
		private bool redKeep = false;
		// プロパティを初期値としてセットするかを決めるフラグ true=初期値とする
		private bool initialFlag = true;
		// コントロールの拡大、縮小状態
		private int zoom;
		// ネットワーク接続エラー点滅判定
		private bool isFlashing = false;
		// 以前に書き込んだアラーム情報
		private List<string> alarmstring = new List<string>();
		// 設定値が変更された事を示すフラグ
		private bool isSetting = false;
		// 今日エラーカウント数
		private int errorToday = 0;
		// 今月のエラーカウント数
		private int errorCount = 0;
		// 稼働率のリスト
		private List<int> Occupancy = new List<int>(31);

		// アラームリストの文字列
		private List<string> alarmList = new List<string>();
		// 操作リストの文字列
		private List<string> operationList = new List<string>();
		// 操作リストの状態
		private bool[] operationState = new bool[8] { false, false, false, false, false, false, false, false };

		/// <summary>
		/// 新しいログファイルを生成する時に判断する日付
		/// </summary>
		private int oldDay;
		// 月が変わった事を判断する数値
		private int oldMonth;

		// 親ウィンドウ
		private Form1 Frm;
		// ファイルのパス
		private string basePath = AppDomain.CurrentDomain.BaseDirectory;

		// 日付が変わるタイミングを監視するタイマ
		private System.Timers.Timer newDayTimer;
		// 稼働率を更新するタイマ
		private System.Timers.Timer occupancyRateTimer;
		// 稼動情報を収集するタイマ
		private System.Timers.Timer operationTimer;
		// コントロールを点滅させるタイマ
		private System.Timers.Timer errTimer;
		// 定期的に接続を試みるタイマ
		private System.Timers.Timer connectTimer;

		#region コントロールのプロパティ +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 監視を行うか否か
		/// </summary>
		public bool MachineCheck
		{
			set
			{
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
				{
					diffmachineCheck = value;
					machineCheck = value;
				}
				// 初期値でなければ比較処理
				else
				{
					machineDifference();
					checkOperation(value);
				}
			}
			get { return machineCheck; }
		}

		/// <summary>
		/// メーカ名のプロパティ
		/// </summary>
		public string Maker
		{
			set
			{
				maker = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffMaker = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
			}
			get { return maker; }
		}

		/// <summary>
		/// メーカ型番のプロパティ
		/// </summary>
		public string ModelNumber
		{
			set
			{
				modelNumber = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffModelNumber = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
			}
			get { return modelNumber; }
		}

		/// <summary>
		/// 部屋のプロパティ 0=エリア1 1=エリア2
		/// </summary>
		public int RoomNumber
		{
			get;
		}

		/// <summary>
		/// 装置IPアドレスのプロパティ
		/// </summary>
		public IPAddress MachineAddress
		{
			set
			{
				ipaddress = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffMachineAddress = value;
				else
					machineDifference();
			}
			get
			{
				return ipaddress;
			}
		}

		/// <summary>
		/// 機械番号のプロパティ
		/// </summary>
		public string MachineNumber
		{
			set
			{
				machineNumber = value;
				label3.Text = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffMachineNumber = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
			}
			get { return machineNumber; }
		}

		/// <summary>
		/// メンテナンス日のプロパティ
		/// </summary>
		public string MaintenanceDate
		{
			set
			{
				maintenanceDate = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffMaintenanceDate = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
			}
			get { return maintenanceDate; }
		}

		/// <summary>
		/// メモのプロパティ
		/// </summary>
		public string Memo
		{
			set
			{
				memo = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffMemo = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
			}
			get { return memo; }
		}

		/// <summary>
		/// 緑シグナルタワー保持設定
		/// </summary>
		public bool GreenKeep
		{
			set
			{
				greenKeep = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffGreenKeep = value;
				// 初期値でなければ比較処理
				else
				{
					// データが不一致なら更新されたフラグをセットする
					if (machineDifference() == false)
						isSetting = true;
				}
			}
			get { return greenKeep; }
		}

		/// <summary>
		/// 黄シグナルタワー保持設定
		/// </summary>
		public bool YellowKeep
		{
			set
			{
				yellowKeep = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffYellowKeep = value;
				// 初期値でなければ比較処理
				else
				{
					// データが不一致なら更新されたフラグをセットする
					if (machineDifference() == false)
						isSetting = true;
				}
			}
			get { return yellowKeep; }
		}

		/// <summary>
		/// 赤シグナルタワー保持設定
		/// </summary>
		public bool RedKeep
		{
			set
			{
				redKeep = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffRedKeep = value;
				// 初期値でなければ比較処理
				else
				{
					// データが不一致なら更新されたフラグをセットする
					if (machineDifference() == false)
						isSetting = true;
				}
			}
			get { return redKeep; }
		}

		/// <summary>
		/// 使用するアラームファイル名
		/// </summary>
		public string AlarmFile
		{
			set
			{
				alarmFile = value;
				getAlarm();
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffAlarmFile = value;
				else
				{
					// データが不一致なら更新されたフラグをセットする
					if (machineDifference() == false)
						isSetting = true;
				}
			}
			get { return alarmFile; }
		}

		/// <summary>
		/// 使用する操作ファイル名
		/// </summary>
		public string OperationFile
		{
			set
			{
				operationFile = value;
				getOperation();
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffOperationFile = value;
				else
				{
					// データが不一致なら更新されたフラグをセットする
					if (machineDifference() == false)
						isSetting = true;
				}
			}
			get { return operationFile; }
		}

		/// <summary>
		/// コントロールの背景色
		/// </summary>
		public Color BColor
		{
			set { this.BackColor = value; }
			get { return this.BackColor; }
		}

		/// <summary>
		/// コントロールの文字色
		/// </summary>
		public Color FColor
		{
			set { this.ForeColor = value; }
			get { return this.ForeColor; }
		}

		/// <summary>
		/// コントロールの座標
		/// </summary>
		public Point Pos
		{
			set
			{
				this.Location = value;
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffPos = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
			}
			get { return this.Location; }
		}

		/// <summary>
		/// コントロールのサイズ
		/// </summary>
		public Size Sz
		{
			set
			{
				this.Size = new Size(value.Width, value.Height);
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffSize = value;
				else
				{
					machineDifference();
					// 全体の差異チェック
					Frm.Difference();
				}
			}
			get { return this.Size; }
		}

		/// <summary>
		/// コントロールの拡大、縮小
		/// </summary>
		public int Zoom
		{
			set
			{
				// 初期値としてセットなら比較用データに入れる
				if (InitialFlag == true)
					diffZoom = value;
				// 初期値でなければ比較処理
				else
					machineDifference();
				setMachineSize(value);
			}
			get { return zoom; }
		}

		/// <summary>
		/// プロパティを初期値としてセットするか否かを決める
		/// trueにセットしてから各プロパティを書き込むと、それを初期値とする
		/// </summary>
		public bool InitialFlag
		{
			set { initialFlag = value; }
			get { return initialFlag; }
		}

		/// <summary>
		/// 初期値と設定値との差異
		/// false=差異あり　true=差異なし
		/// </summary>
		public bool Difference
		{
			get
			{
				return machineDifference();
			}
		}

		#endregion コントロールのプロパティ ----------------------------------------------------------------------------------------------------


		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="frm">親ウィンドウ</param>
		/// <param name="settingData">設定値の文字列</param>
		public Machine(Form1 frm, string settingData)
		{
			InitializeComponent();
			Frm = frm;
			// 引数をカンマ区切りで分割しセットする
			string[] setStr = settingData.Split(',');
			Maker = setStr[0];
			ModelNumber = setStr[1];
			RoomNumber = int.Parse(setStr[2]);
			MachineAddress = IPAddress.Parse(setStr[3]);
			MachineNumber = setStr[4];
			MachineCheck = setStr[5] == "0" ? false : true;
			GreenKeep = setStr[6] == "0" ? false : true;
			YellowKeep = setStr[7] == "0" ? false : true;
			RedKeep = setStr[8] == "0" ? false : true;
			MaintenanceDate = setStr[9];
			if (setStr[10] != "")
			{
				if (frm.AlarmFile.Any(a => a == setStr[10]))
					AlarmFile = setStr[10];
				else
					MessageBox.Show(setStr[10] + "が存在しません。\r\n設定を確認してください。", "機械番号：" + MachineNumber + " アラームファイル読み込み", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
				AlarmFile = "";
			if (setStr[11] != "")
			{
				if (frm.OperationFile.Any(a => a == setStr[11]))
					OperationFile = setStr[11];
				else
					MessageBox.Show(setStr[11] + "が存在しません。\r\n設定を確認してください。", "機械番号：" + MachineNumber + "操作ファイル読み込み", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
				OperationFile = "";
			Memo = setStr[12];
			Pos = new Point(int.Parse(setStr[13]), int.Parse(setStr[14]));
			Sz = new Size(int.Parse(setStr[15]), int.Parse(setStr[16]));
			Zoom = int.Parse(setStr[17]);

			basePath += Path.Combine("加工機情報", MachineNumber) + @"\";
		}

		/// <summary>
		/// フォームが初めて表示される直前のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Machine_Load(object sender, EventArgs e)
		{
			try
			{
				/***** スレッドタイマの初期化 *****/
				newDayTimer = new System.Timers.Timer();
				newDayTimer.AutoReset = true;
				// 日にちが変わる時間を計算して次のタイマイベントのタイミングとする
				newDayTimer.Interval = 86400000 - (DateTime.Now.Hour * 3600000 + DateTime.Now.Minute * 60000 + DateTime.Now.Second * 1000 + DateTime.Now.Millisecond);
				newDayTimer.Elapsed += new System.Timers.ElapsedEventHandler(checkLogFile);
				newDayTimer.Start();
				// アプリ起動時はタイマによる書き込みを行わないので現在の日付をセットしておく
				oldDay = DateTime.Now.Day;

				// 1分周期で稼働時間や稼動率を算出するタイマ
				occupancyRateTimer = new System.Timers.Timer();
				occupancyRateTimer.AutoReset = true;
				occupancyRateTimer.Elapsed += new System.Timers.ElapsedEventHandler(OccupancyTime);
				occupancyRateTimer.Interval = 60010 - DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
				occupancyRateTimer.Start();

				// 稼動情報を収集するタイマ
				operationTimer = new System.Timers.Timer(Properties.Settings.Default.RegularlyTime);
				operationTimer.AutoReset = true;
				operationTimer.Elapsed += new System.Timers.ElapsedEventHandler(OperationTime);
				operationTimer.Start();

				// コントロールを点滅させるタイマ
				errTimer = new System.Timers.Timer(500);
				errTimer.AutoReset = true;
				errTimer.Elapsed += new System.Timers.ElapsedEventHandler(ErrTime);

				// 切断時、定期的に接続を試みるタイマ
				connectTimer = new System.Timers.Timer(1000);
				connectTimer.AutoReset = true;
				connectTimer.Elapsed += new System.Timers.ElapsedEventHandler(ConnectTime);

				// 初期化フラグ解除
				InitialFlag = false;

				// 機械番号のラベルを適切な位置へ移動
				label3.Location = new Point(this.Width / 2 - label3.Width / 2, 1);

				lightOperation(5);
				// 「監視する」設定なら接続
				if (MachineCheck == true)
				{
					Task.Run(() =>
					{
						while (true)
						{
							if (this.connect() == true)
								break;
						}
					});
				}

				// 今月のエラー回数を数える
				oldMonth = DateTime.Now.Month;
				for (int i = 1; i < DateTime.Now.Day + 1; i++)
				{
					StringBuilder filepath = new StringBuilder(basePath);
					filepath.Append(Path.Combine(DateTime.Now.ToString("yyyy年"), DateTime.Now.ToString("MM月"), i.ToString("00日") + @".csv"));
					// ファイルが存在していたら開く
					if (File.Exists(filepath.ToString()))
					{
						using (FileStream fs = new FileStream(filepath.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
						{
							// ストリームの末尾まで繰り返す
							while (!sr.EndOfStream)
							{
								// 1行読み込み、文字が存在していたらエラーとみなす
								if (sr.ReadLine().Contains("赤") == true)
								{
									errorCount++;
									// 今日のファイルなら今日のエラーにも加算
									if (i == DateTime.Now.Day)
										errorToday++;
								}
							}
						}
						// 指定日のデータ集計
						int[] getTime = this.GetTotalTime(new DateTime(DateTime.Now.Year, DateTime.Now.Month, i));
						// 稼働率を計算して配列にセット
						Occupancy.Add(getTime.Sum() == 0 ? 0 : (int)((double)getTime[0] / getTime.Sum() * 100));
					}
					// ファイルが存在してなければ
					else
					{
						Occupancy.Add(-1);
					}
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 再描画処理
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Machine_Paint(object sender, PaintEventArgs e)
		{
			using (SolidBrush sb = new SolidBrush(this.ForeColor))
			{
				float posY = label3.Size.Height + 10;

				// 文字高さを取得しておく(後でオフセット)
				float addPos = e.Graphics.MeasureString(" \r\n ", this.Font, this.Width).Height + 5;

				// 今月エラー回数の表示
				if (Properties.Settings.Default.IsViewErrir == true)
				{
					e.Graphics.DrawString("今月のエラー回数\r\n " + errorCount.ToString() + "回", this.Font, sb, 5, posY);
					posY += addPos;

					e.Graphics.DrawString("今日のエラー回数\r\n " + errorToday.ToString() + "回", this.Font, sb, 5, posY);
					posY += addPos;
				}

				// 稼働率の表示
				if (Properties.Settings.Default.IsViewOccupancy == true)
				{
					// 稼働率の計算と文字列化
					if (Occupancy.Count(o => o != -1) != 0)
						e.Graphics.DrawString("今月の稼働率\r\n " + Occupancy.Where(o => o != -1).Average().ToString("0.0") + "%", this.Font, sb, 5, posY);
					else
						e.Graphics.DrawString("今月の稼働率\r\n 0%", this.Font, sb, 5, posY);
					posY += addPos;

					// 監視しない場合は「－」を表示する
					if (machineCheck == true)
						e.Graphics.DrawString("今日の稼働率\r\n " + Occupancy[DateTime.Now.Day - 1].ToString() + "%", this.Font, sb, 5, posY);
					else
						e.Graphics.DrawString("今日の稼働率\r\n －", this.Font, sb, 5, posY);
					posY += addPos;
				}

				// メンテナンス日の表示
				if (Properties.Settings.Default.IsViewMaintenance == true)
				{
					e.Graphics.DrawString("メンテナンス日\r\n " + maintenanceDate, this.Font, sb, 5, posY);
					posY += addPos;
				}

				// メモの表示
				if (Properties.Settings.Default.IsViewMemo == true)
				{
					e.Graphics.DrawString("メモ\r\n " + memo, this.Font, sb, 5, posY);
				}
			}
		}

		/// <summary>
		/// 終了処理
		/// </summary>
		public void Exit()
		{
			try
			{
				this.lightOperation(5);

				// ネットワークが接続されていれば切断
				if (isConnect == true)
				{
					userClose = true;
					this.close();
				}

				newDayTimer.Stop();
				occupancyRateTimer.Stop();
				operationTimer.Stop();
				errTimer.Stop();
				connectTimer.Stop();
				newDayTimer.Dispose();
				occupancyRateTimer.Dispose();
				operationTimer.Dispose();
				errTimer.Dispose();
				connectTimer.Dispose();

			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		#region internalメソッド +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 設定値を更新し保存する文字列を返す
		/// </summary>
		/// <param name="isSend">false：設定値は送信しない true：設定値を送信</param>
		/// <returns>設定値として保存可能な文字列</returns>
		internal string UpdateSetting(bool isSend = false)
		{
			StringBuilder updateStr = new StringBuilder();
			try
			{
				// メーカ名
				diffMaker = Maker;
				updateStr.Append(Maker).Append(",");
				// メーカ型番
				diffModelNumber = ModelNumber;
				updateStr.Append(ModelNumber).Append(",");
				// 部屋識別
				updateStr.Append(RoomNumber == 0 ? "0," : "1,");
				// IPアドレス
				diffMachineAddress = MachineAddress;
				updateStr.Append(MachineAddress.ToString()).Append(",");
				// 機械番号
				diffMachineNumber = MachineNumber;
				updateStr.Append(MachineNumber).Append(",");
				// 監視設定
				diffmachineCheck = machineCheck;
				updateStr.Append(machineCheck == false ? "0," : "1,");
				// 緑保持設定
				diffGreenKeep = greenKeep;
				updateStr.Append(greenKeep == false ? "0," : "1,");
				// 黄保持設定
				diffYellowKeep = yellowKeep;
				updateStr.Append(yellowKeep == false ? "0," : "1,");
				// 赤保持設定
				diffRedKeep = redKeep;
				updateStr.Append(redKeep == false ? "0," : "1,");
				// メンテナンス日
				diffMaintenanceDate = MaintenanceDate;
				updateStr.Append(MaintenanceDate).Append(",");
				// アラームファイル名
				diffAlarmFile = alarmFile;
				updateStr.Append(alarmFile).Append(",");
				// 操作リストファイル名
				diffOperationFile = operationFile;
				updateStr.Append(operationFile).Append(",");
				// メモ
				diffMemo = Memo;
				updateStr.Append(Memo).Append(",");
				// コントロール位置X
				diffPos.X = this.Location.X;
				updateStr.Append(this.Location.X.ToString()).Append(",");
				// コントロール位置Y
				diffPos.Y = this.Location.Y;
				updateStr.Append(this.Location.Y.ToString()).Append(",");
				// コントロールサイズ幅
				diffSize.Width = this.Size.Width;
				updateStr.Append(this.Size.Width.ToString()).Append(",");
				// コントロールサイズ高さ
				diffSize.Height = this.Size.Height;
				updateStr.Append(this.Size.Height.ToString()).Append(",");
				// 文字サイズ
				diffZoom = zoom;
				updateStr.Append(zoom.ToString());
				if (isSend == true || isSetting == true)
				{
					this.setData(true);
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
				return null;
			}
			return updateStr.ToString();
		}

		/// <summary>
		/// 指定された日のデータ集計を行う
		/// </summary>
		/// <param name="dT">指定日</param>
		/// <returns>集計した秒データの配列 0=緑 1=黄 2=赤 3=消 4=断 5=無 nullなら該当する日のデータはない</returns>
		internal int[] GetTotalTime(DateTime dT)
		{
			int[] getdata = null;
			try
			{
				// 色に該当する数値を返すラムダ式
				Func<string, int> colorNumber = (str) =>
				{
					return str == "緑" ? 0 :
						str == "黄" ? 1 :
						str == "赤" ? 2 :
						str == "消" ? 3 :
						str == "断" ? 4 :
						5;
				};
				// パスを生成
				StringBuilder settingPath = new StringBuilder(basePath);
				settingPath.Append(Path.Combine(dT.ToString("yyyy年"), dT.ToString("MM月"))).Append(@"\");
				// フォルダチェック
				if (Directory.Exists(settingPath.ToString()))
				{
					// フォルダが存在していればファイルの有無チェック
					settingPath.Append(Path.Combine(dT.ToString("dd日"))).Append(".csv");
					if (File.Exists(settingPath.ToString()))
					{
						getdata = new int[6] { 0, 0, 0, 0, 0, 0 };
						List<string[]> csvData = new List<string[]>();
						using (FileStream fs = new FileStream(settingPath.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
						{
							// ストリームの末尾まで繰り返す
							while (!sr.EndOfStream)
							{
								// 1行読み、追加する
								csvData.Add(sr.ReadLine().Split(','));
							}
						}
						int beforeState = 0;
						TimeSpan beforeTS = new TimeSpan(0, 0, 0);
						// 最初のデータ処理
						//　0=無通信　1=緑 2=黄 3=赤 4=消灯　5=データなし
						beforeState = colorNumber(csvData[0][1]);
						beforeTS = TimeSpan.Parse(csvData[0][0]);
						// 最初の時間が00:00:00以外なら停止してたのでデータなしに時間を入れる
						if (csvData[0][0] != "00:00:00")
							getdata[5] = (int)beforeTS.TotalSeconds;
						// csvData.Countは何度も参照するので変数に入れておく
						int count = csvData.Count;
						// 残りのデータ処理
						for (int i = 1; i < count; i++)
						{
							//　最後は当日でなければ23:59:59までを計算し、当日なら今の時間で計算
							if (i == count - 1)
							{
								// まずは前回の計算
								getdata[beforeState] += (int)TimeSpan.Parse(csvData[i][0]).TotalSeconds - (int)beforeTS.TotalSeconds;
								//　0=無通信　1=緑 2=黄 3=赤 4=消灯　5=データなし
								beforeState = colorNumber(csvData[i][1]);
								// 最後のデータ取得
								beforeTS = TimeSpan.Parse(csvData[i][0]);
								// 今日なら今の時間から前回の時間を引き時間を算出
								if (dT.Date == DateTime.Now.Date)
									getdata[beforeState] += (int)DateTime.Now.TimeOfDay.TotalSeconds - (int)beforeTS.TotalSeconds;
								// 今日以外なら86400(日付が変わるタイミングの秒数)から前回の時間を引き、最後の時間を計算
								else
									getdata[beforeState] += 86400 - (int)beforeTS.TotalSeconds;
							}
							else
							{
								// 今の時間から前回の時間を引き時間を算出し、シグナルタワーの色に該当する要素番号の配列に入れる
								getdata[beforeState] += (int)TimeSpan.Parse(csvData[i][0]).TotalSeconds - (int)beforeTS.TotalSeconds;
								// 今回の色と時間を次回の計算用として変数に入れておく
								beforeState = colorNumber(csvData[i][1]);
								beforeTS = TimeSpan.Parse(csvData[i][0]);
							}
						}
					}
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
				return null;
			}
			return getdata;
		}

		/// <summary>
		/// 稼働状況を表示する
		/// </summary>
		/// <param name="picture">表示するピクチャーボックス</param>
		/// /// <param name="operationCanvas">稼働状況をグラフィカル表示させるビットマップ</param>
		/// <param name="datetime">表示する日</param>
		/// <param name="index">表示するピクチャーボックスのインデックス</param>
		/// <param name="errorInfo">稼働情報を入れる構造体のリスト</param>
		/// <returns></returns>
		internal bool SetOperation(ref PictureBox picture, ref Bitmap operationCanvas, DateTime datetime, int index, ref List<alarmInformation> errorInfo)
		{
			try
			{
				// 引数から色を決定するラムダ式
				Func<string, Brush> SelectColor = (selectStr) =>
				{
					return selectStr == "緑" ? new SolidBrush(Properties.Settings.Default.Panel2GreenBackColor) :
						selectStr == "黄" ? new SolidBrush(Properties.Settings.Default.Panel2YellowBackColor) :
						selectStr == "赤" ? new SolidBrush(Properties.Settings.Default.Panel2RedBackColor) :
						selectStr == "消" ? new SolidBrush(Properties.Settings.Default.Panel2EraseBackColor) :
						selectStr == "断" ? new SolidBrush(Properties.Settings.Default.Panel2CutBackColor) :
						new SolidBrush(Properties.Settings.Default.Panel2DontBackColor);
				};

				// エラー情報を生成するラムダ式
				// 引数1：エラーリスト 引数2：開始時刻 引数3：終了時刻
				Func< List <alarmInformation>, int, int, alarmInformation> alarmSet = (errList, startPos, endPos) =>
				{
					// 開始時間から終了時間までの間に入っているデータを全て抽出
					List<alarmInformation> selectAlarm = new List<alarmInformation>(errList.Where(a => a.startPos >= startPos && a.startPos <= endPos));
					alarmInformation alarm = new alarmInformation();
					alarm.startPos = startPos;
					alarm.endPos = endPos;
					if (selectAlarm.Count == 0)
						alarm.errMessage = "アラーム情報なし";
					else
					{
						StringBuilder alarmStr = new StringBuilder();
						foreach (var alm in selectAlarm)
							alarmStr.Append(alm.errMessage).Append("\r\n");
						// 末尾のスペースは消す
						alarmStr.ToString().TrimEnd('\r','\n');
						alarm.errMessage = alarmStr.ToString();
					}
					return alarm;
				};

				StringBuilder filepath = new StringBuilder(basePath);
				filepath.Append(Path.Combine(datetime.ToString("yyyy年"), datetime.ToString("MM月"), datetime.ToString("dd日"))).Append(".csv");
				// ファイルが存在していたら開く
				if (File.Exists(filepath.ToString()))
				{
					List<string[]> csvData = new List<string[]>();
					/***** 稼働状況csvファイルを開く *****/
					using (FileStream fs = new FileStream(filepath.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// ストリームの末尾まで繰り返す
						while (!sr.EndOfStream)
						{
							// 1行読み、追加する
							csvData.Add(sr.ReadLine().Split(','));
						}
					}
					/***** アラーム情報ファイルを開く *****/
					List<alarmInformation> errList = new List<alarmInformation>();
					filepath = new StringBuilder(basePath);
					filepath.Append(Path.Combine(datetime.ToString("yyyy年"), datetime.ToString("MM月"), datetime.ToString("dd日"))).Append("アラーム.csv");
					// ファイルが存在していたら開く
					if (File.Exists(filepath.ToString()))
					{
						using (FileStream fs = new FileStream(filepath.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
						{
							// ストリームの末尾まで繰り返す
							while (!sr.EndOfStream)
							{
								// 1行読み、追加する
								string[] str = sr.ReadLine().Split(',');
								// とりあえずアラームデータを仮入れしておく
								alarmInformation err = new alarmInformation();
								err.startPos = int.Parse(str[0].Substring(0, 2)) * 60 + int.Parse(str[0].Substring(3, 2));
								err.endPos = -1;
								err.errMessage = str[1].Replace(" ","\r\n");
								errList.Add(err);
							}
						}
					}
					// オブジェクトが作成されてたら一度クリアする
					if (operationCanvas != null) operationCanvas.Dispose();
					//描画先とするImageオブジェクトを作成する
					operationCanvas = new Bitmap(picture.Width, picture.Height);
					//ImageオブジェクトのGraphicsオブジェクトを作成する
					using (Graphics g = Graphics.FromImage(operationCanvas))
					{
						int startPoint = 0;
						string set_color = "無";
						int count = csvData.Count;
						// 最初のデータ処理
						// 取得した時間から開始位置を算出
						startPoint = int.Parse(csvData[0][0].Substring(0, 2)) * 60 + int.Parse(csvData[0][0].Substring(3, 2));
						set_color = csvData[0][1];
						// データが1つしかなければこれで描画
						if (count == 1)
						{
							int length;
							// 今日のデータ以外なら最後まで描画
							if (datetime.ToShortDateString() != DateTime.Now.ToShortDateString())
								length = 1440 - startPoint;
							else
								// 現在時刻から長さを算出
								length = DateTime.Now.Hour * 60 + DateTime.Now.Minute - startPoint;
							// startPointが0以外なら0からstartPointの間は「データなし」の色にする
							if (startPoint != 0)
							{
								g.FillRectangle(SelectColor("無"), new Rectangle(0, 0, startPoint, 50));
							}
							// 塗りつぶし
							g.FillRectangle(SelectColor(set_color), new Rectangle(startPoint, 0, length, 50));
							// 「赤」ならアラームデータの座標取得
							if (set_color == "赤")
							{
								errorInfo.Add(alarmSet(errList, startPoint, length));
							}
						}
						else
						{
							// 最初の位置が0以外なら0から最初の時間までは「データなし」の色にする
							if (startPoint != 0)
							{
								g.FillRectangle(SelectColor("無"), new Rectangle(0, 0, startPoint, 50));
							}
							// データ数だけループさせ描画する
							for (int i = 1; i < count; i++)
							{
								// 時間から長さを算出(秒は表示できないから分まで)
								int length = int.Parse(csvData[i][0].Substring(0, 2)) * 60 + int.Parse(csvData[i][0].Substring(3, 2));
								// 変化が1分以上の場合色の設定
								if (startPoint != length)
								{
									// 塗りつぶし
									g.FillRectangle(SelectColor(set_color), new Rectangle(startPoint, 0, length - startPoint, 50));
									// 「赤」ならアラームデータの座標取得
									if (set_color == "赤")
									{
										errorInfo.Add(alarmSet(errList, startPoint, length));
									}
									// 長さの数値が次の開始位置となる
									startPoint = length;
								}
								// 次の色をセットしておく
								set_color = csvData[i][1];
								// 最後のデータ処理
								if (i == count - 1)
								{
									// 今日のデータ以外なら最後まで描画
									if (datetime.ToShortDateString() != DateTime.Now.ToShortDateString())
										length = 1440 - startPoint;
									// 今日なら今の時間まで描画
									else
										length = (DateTime.Now.Hour * 60 + DateTime.Now.Minute) - startPoint;
									// 塗りつぶし
									g.FillRectangle(SelectColor(set_color), new Rectangle(startPoint, 0, length, 50));
									// 「赤」ならアラームデータの座標取得
									if (set_color == "赤")
									{
										errorInfo.Add(alarmSet(errList, startPoint, startPoint + length));
									}
								}
							}
						}
						// インデックスが奇数か偶数かでラインの開始位置を変える
						if (index % 2 == 0)
						{
							// 30分ごとの短いライン
							for (int i = 30; i < 1440; i += 60)
							{
								g.DrawLine(Pens.Black, i, 35, i, 50);
							}
							// 1時間ごとのラインを引く
							for (int i = 60; i < 1440; i += 60)
							{
								g.DrawLine(Pens.Black, i, 25, i, 50);
							}
						}
						else
						{
							// 30分ごとの短いライン
							for (int i = 30; i < 1440; i += 60)
							{
								g.DrawLine(Pens.Black, i, 0, i, 15);
							}
							// 1時間ごとのラインを引く
							for (int i = 60; i < 1440; i += 60)
							{
								g.DrawLine(Pens.Black, i, 0, i, 25);
							}
						}
						//PictureBoxに表示する
						picture.Image = operationCanvas;
					}
					// アラームリストでendPosの値が-1の要素を削除(表示されない短時間のアラームの可能性があるため)
					errorInfo.RemoveAll(c => c.endPos == -1);
				}
				else
				{
					return false;
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
				return false;
			}
			return true;
		}

		/// <summary>
		/// 指定された日のエラー情報を得る
		/// 稼働状況データとアラーム情報を照らし合わせ開始、終了時刻を得る
		/// </summary>
		/// <param name="dt">取得する日</param>
		/// <returns>エラーリスト</returns>
		internal List<alarmInformation> GetErrorDay(DateTime dt)
		{
			List<alarmInformation> alarm = new List<alarmInformation>();
			try
			{
				// データファイルのパス
				StringBuilder dataFile = new StringBuilder(basePath);
				dataFile.Append(Path.Combine(dt.ToString("yyyy年"), dt.ToString("MM月"), dt.ToString("dd日"))).Append(".csv");
				// データファイルが存在していたら開く
				if (File.Exists(dataFile.ToString()))
				{
					List<string[]> dataList = new List<string[]>();
					// データファイルを開く
					using (FileStream fs = new FileStream(dataFile.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// ストリームの末尾まで繰り返す
						while (!sr.EndOfStream)
						{
							// 1行読み、追加する
							dataList.Add(sr.ReadLine().Split(','));
						}
					}
					List<alarmInformation> alaemList = new List<alarmInformation>();
					// アラームファイルのパス
					StringBuilder alarmFile = new StringBuilder(basePath);
					alarmFile.Append(Path.Combine(dt.ToString("yyyy年"), dt.ToString("MM月"), dt.ToString("dd日"))).Append("アラーム.csv");
					// アラームファイルが存在していたら開く
					if (File.Exists(alarmFile.ToString()))
					{
						// アラームファイルを開く
						using (FileStream fs = new FileStream(alarmFile.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
						{
							// ストリームの末尾まで繰り返す
							while (!sr.EndOfStream)
							{
								// 1行読み、追加する
								string[] str = sr.ReadLine().Split(',');
								// とりあえずアラームデータを仮入れしておく
								alarmInformation al = new alarmInformation();
								al.startPos = int.Parse(str[0].Substring(0, 2)) * 3600 + int.Parse(str[0].Substring(3, 2)) * 60 + int.Parse(str[0].Substring(6, 2));
								al.errMessage = str[1].Replace(" ", "\r\n");
								alaemList.Add(al);
							}
						}
					}

					// データリストの分だけループし「赤」の時のアラーム時間を算出する
					for (int i = 0; i < dataList.Count; i++)
					{
						if (dataList[i][1] == "赤")
						{
							// 開始位置の取得
							int startPoint = int.Parse(dataList[i][0].Substring(0, 2)) * 3600 + int.Parse(dataList[i][0].Substring(3, 2)) * 60 + int.Parse(dataList[i][0].Substring(6, 2));
							int endPoint;
							// 今回が最後終了位置の取得
							if (i == dataList.Count - 1)
							{
								// 今日のデータ以外なら23:59:59まで
								if (dt.ToShortDateString() != DateTime.Now.ToShortDateString())
									endPoint = 86400;
								// 今日なら今の時間まで描画
								else
									endPoint = DateTime.Now.Hour * 3600 + DateTime.Now.Minute * 60 + DateTime.Now.Second;
							}
							else
							{
								// 今回が最後でなければ次の要素の開始時間が赤の終了時間となる
								endPoint = int.Parse(dataList[i + 1][0].Substring(0, 2)) * 3600 + int.Parse(dataList[i + 1][0].Substring(3, 2)) * 60 + int.Parse(dataList[i + 1][0].Substring(6, 2));
							}
							// アラーム時刻が「赤」の開始時間から終了時間までの間に入っているデータを全て抽出
							List <alarmInformation> selectAlarm = new List<alarmInformation>(alaemList.Where(a => a.startPos >= startPoint && a.startPos <= endPoint));
							// アラーム情報がなければ「なし」を入れる
							if (selectAlarm.Count == 0)
							{
								alarmInformation none;
								none.startPos = startPoint;
								none.endPos = endPoint;
								none.errMessage = "アラーム情報なし";
								alarm.Add(none);
							}
							else
							{
								// 抽出したデータを戻り値に追加
								foreach (var al in selectAlarm)
								{
									alarmInformation addData;
									addData.startPos = startPoint;
									addData.endPos = endPoint;
									addData.errMessage = al.errMessage;
									alarm.Add(addData);
								}
							}
						}
					}
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
			return alarm;
		}

		/// <summary>
		/// 指定された日の操作時間を集計する
		/// </summary>
		/// <param name="dt"></param>
		/// <returns></returns>
		internal Dictionary<string, string> OperationList(DateTime dt)
		{
			Dictionary<string, string> list = new Dictionary<string, string>();
			try
			{
				// データファイルのパス
				StringBuilder dataFile = new StringBuilder(basePath);
				dataFile.Append(Path.Combine(dt.ToString("yyyy年"), dt.ToString("MM月"), dt.ToString("dd日"))).Append("操作.csv");
				// データファイルが存在していたら開く
				if (File.Exists(dataFile.ToString()))
				{
					List<operationInformation> opeInfo = new List<operationInformation>();
					// データファイルを開く
					using (FileStream fs = new FileStream(dataFile.ToString(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// ストリームの末尾まで繰り返す
						while (!sr.EndOfStream)
						{
							// 1行読み、追加する
							string[] str = sr.ReadLine().Split(',');
							operationInformation ope = new operationInformation();
							ope.operationName = str[1];
							ope.operationTime = TimeSpan.Parse(str[0]);
							ope.operationState = str[2] == "OFF" ? false : true;
							opeInfo.Add(ope);
						}
					}
					while (opeInfo.Count != 0)
					{
						// まず先頭のデータの操作名と同じデータを抽出する
						List<operationInformation> select = opeInfo.FindAll(o => o.operationName == opeInfo[0].operationName);
						TimeSpan totalTime = new TimeSpan(0, 0, 0);
						// 抽出したデータがなくなるまでループ
						while (select.Count != 0)
						{
							// 先頭が「OFF」なら開始時刻が分からないので破棄する
							if (select[0].operationState == false)
							{
								select.RemoveAt(0);
							}
							// 先頭が「ON」なら
							else
							{
								// まだデータがあれば
								if (select.Count > 1)
								{
									// 次もONなら最初の「ON」は破棄して先頭からやり直し
									if (select[1].operationState == true)
									{
										select.RemoveAt(0);
									}
									// 時間の計算
									else
									{
										totalTime += (select[1].operationTime - select[0].operationTime);
										select.RemoveRange(0, 2);
									}
								}
								// これが最後のデータなら
								else
								{
									// 今日なら今の時間の差とする
									if (dt.Date == DateTime.Now.Date)
									{
										totalTime += (new TimeSpan(DateTime.Now.Hour, DateTime.Now.Minute,DateTime.Now.Second) - select[0].operationTime);
									}
									// 23:59:59が停止時間
									else
									{
										totalTime += (new TimeSpan(23, 59, 59) - select[0].operationTime);
									}
									select.RemoveAt(0);
								}
							}
						}
						// 戻り値に値をセット
						list.Add(opeInfo[0].operationName, totalTime.ToString());

						// 処理の終わった操作を削除する
						string opename = opeInfo[0].operationName;
						opeInfo.RemoveAll(o => o.operationName == opename);
					}
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
			return list;
		}

		/// <summary>
		/// 文字色や背景色に最新の設定値を反映させる
		/// </summary>
		/// <param name="setNumber">設定する色に該当する数値</param>
		internal void SetColor(int setNumber)
		{
			try
			{
				lightState = setNumber;
				if (machineCheck == false)
				{
					errTimer.Stop();
					BackColor = Properties.Settings.Default.Panel1StopBackColor;
					ForeColor = Properties.Settings.Default.Panel1StopForeColor;
				}
				else
				{
					// 状況に応じて点滅タイマを動作
					if (lightState == 2 || lightState == 4)
						errTimer.Start();
					else
						errTimer.Stop();
					// 背景色を変える
					switch (lightState)
					{
						case 0:
							// シグナルタワーは緑
							BackColor = Properties.Settings.Default.Panel1GreenBackColor;
							ForeColor = Properties.Settings.Default.Panel1GreenForeColor;
							break;
						case 1:
							// シグナルタワーは黄
							BackColor = Properties.Settings.Default.Panel1YellowBackColor;
							ForeColor = Properties.Settings.Default.Panel1YellowForeColor;
							break;
						case 3:
							// シグナルタワーは消灯
							BackColor = Properties.Settings.Default.Panel1EraseBackColor;
							ForeColor = Properties.Settings.Default.Panel1EraseForeColor;
							break;
						case 5:
							// データがない
							BackColor = Properties.Settings.Default.Panel1DontBackColor;
							ForeColor = Properties.Settings.Default.Panel1DontForeColor;
							break;
					}
				}
				BeginInvoke((Action)(() => this.Refresh()));
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}
		internal void SetColor()
		{
			try
			{
				if (machineCheck == false)
				{
					errTimer.Stop();
					BackColor = Properties.Settings.Default.Panel1StopBackColor;
					ForeColor = Properties.Settings.Default.Panel1StopForeColor;
				}
				else
				{
					// 状況に応じて点滅タイマを動作
					if (lightState == 2 || lightState == 4)
						errTimer.Start();
					else
						errTimer.Stop();
					// 背景色を変える
					switch (lightState)
					{
						case 0:
							// シグナルタワーは緑
							BackColor = Properties.Settings.Default.Panel1GreenBackColor;
							ForeColor = Properties.Settings.Default.Panel1GreenForeColor;
							break;
						case 1:
							// シグナルタワーは黄
							BackColor = Properties.Settings.Default.Panel1YellowBackColor;
							ForeColor = Properties.Settings.Default.Panel1YellowForeColor;
							break;
						case 3:
							// シグナルタワーは消灯
							BackColor = Properties.Settings.Default.Panel1EraseBackColor;
							ForeColor = Properties.Settings.Default.Panel1EraseForeColor;
							break;
						case 5:
							// データなし
							BackColor = Properties.Settings.Default.Panel1DontBackColor;
							ForeColor = Properties.Settings.Default.Panel1DontForeColor;
							break;
					}
				}
				BeginInvoke((Action)(() => this.Refresh()));
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 自分の座標とコントロールの大きさを確認しはみ出してたら表示できる場所まで移動する
		/// </summary>
		internal void CheckLocation()
		{
			if (this.Location.X < 0)
				this.Location = new Point(0, this.Location.Y);
			if (this.Location.Y < 0)
				this.Location = new Point(this.Location.X, 0);
			if (this.Location.X + this.Size.Width > this.Parent.Size.Width)
				this.Location = new Point(this.Parent.Size.Width - this.Size.Width, this.Location.Y);
			if (this.Location.Y + this.Size.Height > this.Parent.Size.Height)
				this.Location = new Point(this.Location.X, this.Parent.Size.Height - this.Size.Height);
		}

		#endregion internalメソッド ----------------------------------------------------------------------------------------------------

		#region マウスイベント記述 +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// マウスのボタンが押された時のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Machine_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				
				// サイズ変更可能状態でなければ移動に関する処理
				if (this.Size.Height - 10 < e.Y && this.Size.Width - 10 < e.X)
				{
					System.Windows.Forms.Cursor.Current = Cursors.SizeNWSE;
					isSize = true;
					mouseX = e.X;
					mouseY = e.Y;
					this.label1.Visible = true;
					this.label2.Visible = true;
					this.label1.ForeColor = Color.Black;
					this.label2.ForeColor = Color.Black;
					this.label1.BackColor = Color.White;
					this.label2.BackColor = Color.White;
					this.label1.Location = new Point(0, 0);
					this.label2.Location = new Point(0, 16);
					this.label1.Font = new Font("ＭＳ ゴシック", 12);
					this.label2.Font = new Font("ＭＳ ゴシック", 12);
				}
				else
				{
					x = e.X;
					y = e.Y;
					Cursor.Current = Cursors.Hand;
					// マウスがドラッグされた事を示すフラグをセット
					isDraggable = true;
					// グリッドスナップが有効ならコントロール上の座標でマウスポインタの位置を取得する
					if (Properties.Settings.Default.IsGridSnap == true)
					{
						// 最初はスナップさせないのでフラグはfalse
						snap = false;
						//画面座標でマウスポインタの位置を取得してクライアント座標に変換する
						Point cp = this.PointToClient(Cursor.Position);
						//X座標を取得する
						mouseX = cp.X;
						//Y座標を取得する
						mouseY = cp.Y;
					}
				}
			}
		}

		/// <summary>
		/// マウスのボタンが離された時のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Machine_MouseUp(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
			{
				// サイズ変更可能状態でなければ移動に関する処理
				if (isSize == false)
				{
					Cursor.Current = Cursors.Default;
					isDraggable = false;
					this.CheckLocation();
					machineDifference();
					// 全体の差異チェック
					Frm.Difference();
				}
				else if(isSize == true)
				{
					Cursor.Current = Cursors.Default;
					this.label1.Visible = false;
					this.label2.Visible = false;
					isSize = false;
				}
			}
		}

		/// <summary>
		/// マウスが移動してる時のイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Machine_MouseMove(object sender, MouseEventArgs e)
		{
			// マウスがドラッグされた状態で移動した時の処理
			if (isDraggable == true)
			{
				// プロパティへのアクセスは遅いので頻度が高い場合は変数へ入れる
				int left = this.Left;
				int top = this.Top;
				// グリッド近くに来たらグリッドまでワープ 但し機能が無効の場合はsnapはtrueにならないので何もしない
				if (snap == true)
				{
					// グリッドから一定範囲内に来たらグリッドに移動
					if ((Math.Abs(left % 50) < 10 || Math.Abs(left % 50) > 40) && (Math.Abs(top % 50) < 10 || Math.Abs(top % 50) > 40))
					{
						if (Math.Abs(left % 50) < 10)
							this.Left = left / 50 * 50;
						else
							this.Left = left / 50 * 50 + 50;

						if (Math.Abs(top % 50) < 10)
							this.Top = top / 50 * 50;
						else
							this.Top = top / 50 * 50 + 50;
						snap = false;
						mouseWait = true;
						// クライアント座標を画面座標に変換してマウスポインタの位置を設定する(コントロールがワープするのでマウスカーソルもワープ)
						Cursor.Position = this.PointToScreen(new Point(mouseX, mouseY));
						// グリッドにスナップした後、すぐにスナップするのを抑制させるため現在のマウス座標を取得
						endMouseX = e.X;
						endMouseY = e.Y;
					}
					else
					{
						// 通常の移動処理
						this.Left = left + e.X - x;
						this.Top = top + e.Y - y;
					}
				}
				else
				{
					// グリッドにスナップした後はマウスが一定以上動作しないとコントロールは移動させない（スナップしたのにすぐ移動するのを抑制する)
					if (mouseWait == true)
					{
						if (Math.Abs(endMouseX - e.X) > 10 || Math.Abs(endMouseY - e.Y) > 10)
							mouseWait = false;
					}
					else
					{
						// 通常の移動処理
						this.Left = left + e.X - x;
						this.Top = top + e.Y - y;
						// コントロールが一定以上グリッドから離れたら次のスナップを有効にする(離れたのにすぐスナップするのを避ける)
						if (Properties.Settings.Default.IsGridSnap == true)
						{
							if ((Math.Abs(left % 50) > 15 && Math.Abs(left % 50) < 35) || (Math.Abs(top % 50) > 15 && Math.Abs(top % 50) < 35))
							{
								snap = true;
							}
						}
					}
				}
			}
			// コントロールのサイズ変更が行われている時
			else if (isSize == true)
			{
				Sz = new Size(this.Size.Width + (e.X - mouseX), this.Size.Height + (e.Y - mouseY));
				label3.Location = new Point(this.Width / 2 - label3.Width / 2, 1);
				this.label1.Text = "高さ：" + Sz.Height.ToString();
				this.label2.Text = "幅：" + Sz.Width.ToString();
				mouseX = e.X;
				mouseY = e.Y;
			}
			else
			{
				if (this.Size.Height - 10 < e.Y && this.Size.Width - 10 < e.X)
				{
					System.Windows.Forms.Cursor.Current = Cursors.SizeNWSE;
				}
				else
				{
					System.Windows.Forms.Cursor.Current = Cursors.Default;
				}
			}
		}

		#endregion マウスイベント記述 ----------------------------------------------------------------------------------------------------

		#region コンテキストメニュー記述 +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// コンテキストメニューの+20%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem2_Click(object sender, EventArgs e)
		{
			this.setMachineSize(2);
		}

		/// <summary>
		/// コンテキストメニューの+10%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem3_Click(object sender, EventArgs e)
		{
			this.setMachineSize(1);
		}

		/// <summary>
		/// コンテキストメニューの0%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem4_Click(object sender, EventArgs e)
		{
			this.setMachineSize(0);
		}

		/// <summary>
		/// コンテキストメニューの-10%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem5_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-1);
		}

		/// <summary>
		/// コンテキストメニューの-20%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem6_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-2);
		}

		/// <summary>
		/// コンテキストメニューの-30%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem7_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-3);
		}

		/// <summary>
		/// コンテキストメニューの-40%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem8_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-4);
		}

		/// <summary>
		/// コンテキストメニューの-50%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem9_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-5);
		}

		/// <summary>
		/// コンテキストメニューの-60%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem11_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-6);
		}

		/// <summary>
		/// コンテキストメニューの-70%
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void toolStripMenuItem12_Click(object sender, EventArgs e)
		{
			this.setMachineSize(-7);
		}

		/// <summary>
		/// データが入っているフォルダをエクスプローラで開く
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void データフォルダを開くToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start("EXPLORER.EXE", basePath);
		}

		/// <summary>
		/// 画面サイズ変更画面の表示
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 表示サイズ変更ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (Form3 f3 = new Form3(this.Size))
			{
				f3.Left = Cursor.Position.X - 81;
				f3.Top = Cursor.Position.Y - 107;
				f3.Size = new Size(162, 214);
				f3.ShowDialog(this);
				this.Size = f3.ReturnSize;
			}
			this.machineDifference();
			Frm.Difference();
		}

		/// <summary>
		/// ネットワーク接続
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 接続ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.connect() == false)
			{
				MessageBox.Show("ネットワークに接続できませんでした。", "機械番号：" + machineNumber + " ネットワーク接続", MessageBoxButtons.OK, MessageBoxIcon.Error);
				this.lightOperation(4);
			}
			else
			{
				userClose = false;
				this.setData(true);
			}
		}

		/// <summary>
		/// ネットワーク切断
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 切断ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			userClose = true;
			this.close();
			this.lightOperation(5);
		}

		/// <summary>
		/// 接続、切断が表示される直前
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ネットワークToolStripMenuItem_DropDownOpened(object sender, EventArgs e)
		{
			if (machineCheck == false)
			{
				接続ToolStripMenuItem.Enabled = false;
				切断ToolStripMenuItem.Enabled = false;
			}
			else
			{
				接続ToolStripMenuItem.Enabled = isConnect == false ? true : false;
				切断ToolStripMenuItem.Enabled = isConnect == false ? false : true;
			}
		}

		/// <summary>
		/// 保持点灯の消灯
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void 保持点灯を消灯ToolStripMenuItem_Click(object sender, EventArgs e)
		{
			using (Form6 f6 = new Form6())
			{
				if (f6.ShowDialog(this) == DialogResult.OK)
				{
					isErase = true;
					setData();
				}
			}
		}

		private void contextMenuStrip1_Opened(object sender, EventArgs e)
		{
			// 保持設定がされていて、シグナルタワーが点灯していれば「保持点灯の消灯」を有効にする
			if ((lightState == 0 && greenKeep == true) || (lightState == 1 && yellowKeep == true) || (lightState == 2 && redKeep == true))
				保持点灯を消灯ToolStripMenuItem.Enabled = true;
			else
				保持点灯を消灯ToolStripMenuItem.Enabled = false;
		}

		#endregion コンテキストメニュー記述 ----------------------------------------------------------------------------------------------------

		#region オリジナルメソッド記述 +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 文字サイズを変更する
		/// </summary>
		/// <param name="size">2=+20% 1=+10% 0=0% -1=-10% -2=-20% -3=-30% -4=-40% -5=-50%</param>
		private void setMachineSize(int size)
		{
			try
			{
				Point nowPoint = this.Location;
				Size nowSize = this.Size;
				zoom = size;

				// とりあえず全部チェックを外す
				toolStripMenuItem2.Checked = false;
				toolStripMenuItem3.Checked = false;
				toolStripMenuItem4.Checked = false;
				toolStripMenuItem5.Checked = false;
				toolStripMenuItem6.Checked = false;
				toolStripMenuItem7.Checked = false;
				toolStripMenuItem8.Checked = false;
				toolStripMenuItem9.Checked = false;
				toolStripMenuItem11.Checked = false;
				toolStripMenuItem12.Checked = false;
				switch (size)
				{
					case -7:
						toolStripMenuItem12.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 7);
						break;
					case -6:
						toolStripMenuItem11.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 8);
						break;
					case -5:
						toolStripMenuItem9.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 9);
						break;
					case -4:
						toolStripMenuItem8.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 10);
						break;
					case -3:
						toolStripMenuItem7.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 11);
						break;
					case -2:
						toolStripMenuItem6.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 12);
						break;
					case -1:
						toolStripMenuItem5.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 13);
						break;
					case 0:
						toolStripMenuItem4.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 14);
						break;
					case 1:
						toolStripMenuItem3.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 15);
						break;
					case 2:
						toolStripMenuItem2.Checked = true;
						this.Font = new Font("ＭＳ ゴシック", 16);
						break;
				}
				this.Location = nowPoint;
				this.Size = nowSize;
				label3.Font = this.Font;
				label3.Font = new Font(label3.Font, FontStyle.Bold);
				label3.Size = label3.PreferredSize;
				label3.Location = new Point(this.Width / 2 - label3.Width / 2, 1);
				this.Refresh();
				this.machineDifference();
				Frm.Difference();
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 装置を監視するか否かの値が変化した時の処理
		/// </summary>
		/// <param name="value"></param>
		private void checkOperation(bool value)
		{
			// 現在保存している値と違う時だけ処理を行う
			if (machineCheck != value)
			{
				machineCheck = value;
				Task.Run(() =>
				{
					if (machineCheck == false)
					{
						// 通信方式がネットワークの場合、ネットワークを切断
						userClose = true;
						this.close();
						// 任意の切断なので状態は「データなし」
						this.lightOperation(5);
						BackColor = Properties.Settings.Default.Panel1StopBackColor;
						ForeColor = Properties.Settings.Default.Panel1StopForeColor;
					}
					else
					{
						// 通信方式がネットワークの場合、ネットワークのセットアップを行う
						if (this.connect() == false)
						{
							MessageBox.Show("ネットワークに接続できませんでした。", "機械番号：" + machineNumber + " ネットワーク接続", MessageBoxButtons.OK, MessageBoxIcon.Error);
							this.lightOperation(4);
						}
						else
						{
							setData(isSetting);
							userClose = false;
						}
					}
				});
			}
		}

		/// <summary>
		/// シグナルタワーの状態
		/// </summary>
		/// <param name="value">シグナルタワーの状態 0=緑 1=黄 2=赤 3=消灯 4=通信断 5=データなし</param>
		private void lightOperation(int value)
		{
			// 現在設定している値と違う時だけ処理を行う
			if (lightState != value)
			{
				// 他の色から赤に変わった時だけエラーカウントを加算
				if (value == 2)
				{
					errorCount++;
					errorToday++;
				}
				Task.Run(() =>
				{
					// 背景色を変える
					this.SetColor(value);
					// ログを書き込む
					this.LogWrite(value);
				});
			}
		}

		/// <summary>
		/// 現在のシグナルタワーの状態を書き込む
		/// </summary>
		/// <param name="setColor">設定する色に該当する数値　0=緑 1=黄 2=赤 3=消灯 4=通信断 5=データなし</param>
		private void LogWrite(int setColor)
		{
			try
			{
				// パスを生成
				StringBuilder SettingPath = new StringBuilder(basePath);
				SettingPath.Append(Path.Combine(DateTime.Now.ToString("yyyy年"), DateTime.Now.ToString("MM月"))).Append(@"\");
				// フォルダチェック
				if (!Directory.Exists(SettingPath.ToString()))
					Directory.CreateDirectory(SettingPath.ToString());
				// ファイル名生成
				SettingPath.Append(DateTime.Now.ToString("dd日")).Append(".csv");
				using (FileStream fs = new FileStream(SettingPath.ToString(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					sw.WriteLine(DateTime.Now.ToString("HH:mm:ss,") + (setColor == 0 ? "緑" : setColor == 1 ? "黄" : setColor == 2 ? "赤" : setColor == 3 ? "消" : setColor == 4 ? "断" : "無"));
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// アラームログを生成する
		/// </summary>
		/// <param name="alarm">アラーム文字列</param>
		private void alarmLog(string alarm)
		{
			try
			{
				string writeStr = DateTime.Now.ToString("HH:mm:ss,") + alarm;
				// パスを生成
				StringBuilder SettingPath = new StringBuilder(basePath);
				SettingPath.Append(Path.Combine(DateTime.Now.ToString("yyyy年"), DateTime.Now.ToString("MM月"))).Append(@"\");
				// フォルダチェック
				if (!Directory.Exists(SettingPath.ToString()))
					Directory.CreateDirectory(SettingPath.ToString());
				// ファイル名生成
				SettingPath.Append(DateTime.Now.ToString("dd日")).Append("アラーム.csv");
				using (FileStream fs = new FileStream(SettingPath.ToString(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					sw.WriteLine(writeStr);
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 操作ログを生成する
		/// </summary>
		/// <param name="index">操作文字列の要素番号</param>
		/// <param name="state">ボタンの状態 false=OFF true=ON</param>
		private void operationLog(int index, bool state)
		{
			try
			{
				if (operationList.Count > 0)
				{
					string writeStr = DateTime.Now.ToString("HH:mm:ss,") + operationList[index] + "," + (state == false ? "OFF" : "ON");
					// パスを生成
					StringBuilder SettingPath = new StringBuilder(basePath);
					SettingPath.Append(Path.Combine(DateTime.Now.ToString("yyyy年"), DateTime.Now.ToString("MM月"))).Append(@"\");
					// フォルダチェック
					if (!Directory.Exists(SettingPath.ToString()))
						Directory.CreateDirectory(SettingPath.ToString());
					// ファイル名生成
					SettingPath.Append(DateTime.Now.ToString("dd日")).Append("操作.csv");
					using (FileStream fs = new FileStream(SettingPath.ToString(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
					using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						sw.WriteLine(writeStr);
					}
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// 操作ログを生成する
		/// </summary>
		/// <param name="operationName">操作文字列</param>
		/// <param name="state">ボタンの状態 false=OFF true=ON</param>
		private void operationLog(string operationName, bool state)
		{
			try
			{
				string writeStr = DateTime.Now.ToString("HH:mm:ss,") + operationName + "," + (state == false ? "OFF" : "ON");
				// パスを生成
				StringBuilder SettingPath = new StringBuilder(basePath);
				SettingPath.Append(Path.Combine(DateTime.Now.ToString("yyyy年"), DateTime.Now.ToString("MM月"))).Append(@"\");
				// フォルダチェック
				if (!Directory.Exists(SettingPath.ToString()))
					Directory.CreateDirectory(SettingPath.ToString());
				// ファイル名生成
				SettingPath.Append(DateTime.Now.ToString("dd日")).Append("操作.csv");
				using (FileStream fs = new FileStream(SettingPath.ToString(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					sw.WriteLine(writeStr);
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// エラーログを生成する
		/// </summary>
		/// /// <param name="methodName">エラーを送信したメソッド名</param>
		/// <param name="errMessage">エラー文字列</param>
		private void errLog(string methodName, string errMessage)
		{
			try
			{
				string writeStr = DateTime.Now.ToString("HH:mm:ss,") + methodName + "," +  errMessage;
				// パスを生成
				StringBuilder SettingPath = new StringBuilder(basePath);
				SettingPath.Append(Path.Combine(DateTime.Now.ToString("yyyy年"), DateTime.Now.ToString("MM月"))).Append(@"\");
				// フォルダチェック
				if (!Directory.Exists(SettingPath.ToString()))
					Directory.CreateDirectory(SettingPath.ToString());
				// ファイル名生成
				SettingPath.Append(DateTime.Now.ToString("dd日")).Append("エラー.csv");
				using (FileStream fs = new FileStream(SettingPath.ToString(), FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
				using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift-JIS")))
				{
					sw.WriteLineAsync(writeStr);
				}
			}
			catch (Exception exc)
			{
				Frm.SysrtmError(exc.StackTrace);
			}
		}

		/// <summary>
		/// プロパティ初期値と入力値との比較
		/// </summary>
		/// <returns>false:不一致 true:一致</returns>
		private bool machineDifference()
		{
			if (InitialFlag == false)
			{
				if (diffmachineCheck != machineCheck)
					return false;
				if (diffMaker != maker)
					return false;
				if (diffModelNumber != modelNumber)
					return false;
				if (diffMachineNumber != machineNumber)
					return false;
				if (diffMaintenanceDate != maintenanceDate)
					return false;
				if (diffMachineAddress.ToString() != ipaddress.ToString())
					return false;
				if (diffAlarmFile != alarmFile)
					return false;
				if (diffOperationFile != operationFile)
					return false;
				if (diffMemo != memo)
					return false;
				if (diffGreenKeep != greenKeep)
					return false;
				if (diffYellowKeep != yellowKeep)
					return false;
				if (diffRedKeep != redKeep)
					return false;
				if (diffPos.X != this.Location.X)
					return false;
				if (diffPos.Y != this.Location.Y)
					return false;
				if (diffZoom != zoom)
					return false;
				if (diffSize != this.Size)
					return false;
			}
			return true;
		}

		/// <summary>
		/// アラームリストの取得
		/// </summary>
		private void getAlarm()
		{
			if (alarmFile != "")
			{
				string path = AppDomain.CurrentDomain.BaseDirectory + Path.Combine(alarmFile);
				if (File.Exists(path))
				{
					using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						alarmList = new List<string>(sr.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
					}
					// 先頭は解説なので削除
					alarmList.RemoveAt(0);
				}
				else
				{
					MessageBox.Show(alarmFile + "がありません。\r\nファイルの有無、ファイル名を確認してください。", "機械番号：" + machineNumber + " アラームリストファイル読み込み", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				alarmList.Clear();
			}
		}

		/// <summary>
		/// 操作情報の取得
		/// </summary>
		internal void getOperation()
		{
			List<string> oldOperationList = new List<string>(operationList);
			List<string> diffOperation;
			if (operationFile != "")
			{
				// パスを生成
				string path = AppDomain.CurrentDomain.BaseDirectory + Path.Combine(operationFile);
				if (File.Exists(path))
				{
					using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding("Shift-JIS")))
					{
						// 文字列を改行で区切り配列に入れる(空の文字列を含む配列要素は除く)
						operationList = new List<string>(sr.ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
					}
					// 新しい情報(比較用)の要素数をループで回せるだけ増やす
					diffOperation = new List<string>(operationList);
					while (diffOperation.Count < operationState.Length)
					{
						diffOperation.Add("");
					}
					// 古い情報の要素数をループで回せるだけ増やす
					while (oldOperationList.Count < operationState.Length)
					{
						oldOperationList.Add("");
					}
					/***** 以前の操作情報と内容が違っていてONのままならOFFにする *****/
					for (int i = 0; i < operationState.Length; i++)
					{
						if (operationState[i] == true)
						{
							if (oldOperationList[i] != diffOperation[i])
							{
								operationState[i] = false;
								if (oldOperationList[i] != "")
									operationLog(oldOperationList[i], false);
							}
						}
					}
				}
				else
				{
					MessageBox.Show("操作リストファイルがありません。\r\nファイルの有無、ファイル名を確認してください。", "機械番号：" + machineNumber + " 操作リストファイル読み込み", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
			else
			{
				operationList.Clear();
				// ONのまま削除された操作項目があるかもしれないのでチェックしONのままなら終了ログを書き込む
				for(int i = 0; i < operationState.Length; i++)
				{
					if (operationState[i] == true)
					{
						operationState[i] = false;
						operationLog(oldOperationList[i], false);
					}
				}
			}
		}

		#endregion オリジナルメソッド記述 ----------------------------------------------------------------------------------------------------

		#region ネットワーク関連記述 ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 装置のネットワーク接続
		/// </summary>
		/// <returns>false:接続できず true:接続完了</returns>
		private bool connect()
		{
			lock (Lock)
			{
				// 既に接続状態ならtrueを返す
				if (isConnect == true)
					return true;

				// socketの実態が存在していたら一度削除する
				if (socket != null)
				{
					socket.Close();
					socket.Dispose();
					socket = null;
				}
				// IPアドレスがデフォルトの0.0.0.0のままなら警告して終了
				if (ipaddress == IPAddress.Parse("0.0.0.0"))
				{
					MessageBox.Show("IPアドレスが正しく設定されてません。\r\n設定を確認してください。", "機械番号：" + machineNumber + " 機器ネットワーク接続", MessageBoxButtons.OK, MessageBoxIcon.Error);
					return false;
				}
				else
				{
					socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
					try
					{
						connectEvent.Reset();
						// 非同期接続
						socket.BeginConnect(ipaddress, port, ConnectCallback, socket);
						// イベントが設定された時間に到達したらタイムアウト処理
						if (!connectEvent.WaitOne(1000))
						{
							socket.Close();
							socket = null;
							this.lightOperation(4);
							return false;
						}
					}
					catch (SocketException se)
					{
						// ソケットエラーが発生(プログラムの致命的不具合ではないので例外エラー表示はしない)
						Frm.SysrtmError(se.StackTrace, "SocketException", false);
						this.close();
						socket = null;
						this.lightOperation(4);
						return false;
					}
					catch (ObjectDisposedException ode)
					{
						Frm.SysrtmError(ode.StackTrace, "ObjectDisposedException");
						this.close();
						socket = null;
						this.lightOperation(4);
						return false;
					}
					if (socket.Connected == false)
					{
						this.close();
						socket = null;
						this.lightOperation(4);
						return false;
					}
					isConnect = true;
					/***** 受信動作開始 *****/
					// 非同期通信でデータが入るインスタンス生成
					StateObject stateObject = new StateObject();
					stateObject.workSocket = socket;
					// 非同期受信
					stateObject.workSocket.BeginReceive(stateObject.buffer, 0, stateObject.buffer.Length, SocketFlags.None, new AsyncCallback(this.ReceiveCallback), stateObject);
					// 設定値を含め一度送信する
					this.setData(true);
				}
				return true;
			}
		}

		/// <summary>
		/// Socket接続コールバック関数
		/// </summary>
		/// <param name="result"></param>
		private void ConnectCallback(IAsyncResult result)
		{
			// 接続が完了したらイベントフラグをシグナル状態にする
			connectEvent.Set();
		}

		/// <summary>
		/// 装置のネットワーク切断
		/// </summary>
		private void close()
		{
			if (socket != null)
			{
				try
				{
					// 送受信中に閉じるのはダメなので排他処理をする
					lock (Lock)
					{
						if (socket.Connected == true)
						{
							// 終了コマンドを送信
							socket.Shutdown(SocketShutdown.Both);
							socket.Close();
						}
						socket.Dispose();
						socket = null;
						// 意図的な切断でない場合はタイマを動作させ定期的に接続をチャレンジする
						if (userClose == false)
						{
							connectTimer.Start();
						}
					}
				}
				catch (Exception exc)
				{
					Frm.SysrtmError(exc.StackTrace);
				}
			}
			isConnect = false;
		}

		/// <summary>
		/// TCP非同期受信
		/// </summary>
		/// <param name="ar"></param>
		private void ReceiveCallback(IAsyncResult ar)
		{
			if (socket != null)
			{
				try
				{
					StateObject stateObject = (StateObject)ar.AsyncState;
					try
					{
						// 受信サイズが0なら終了
						if (stateObject.workSocket.EndReceive(ar) == 0)
						{
							this.close();
							this.lightOperation(4);
							return;
						}
					}
					catch (SocketException)
					{
						this.close();
						this.lightOperation(4);
						return;
					}

					// 取得したデータを改行コードごとに分割する
					List<string> str = new List<string>(Encoding.GetEncoding("UTF-8").GetString(stateObject.buffer).TrimEnd('\0').Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries));
					// 受信バッファの内容をクリアする
					Array.Clear(stateObject.buffer, 0, stateObject.buffer.Length);
					// 改行ごとに取得したデータの数だけ処理を行う
					for (int i = 0; i < str.Count(); i++)
					{
						List<string> getStr = new List<string>(str[i].Split(','));

						switch (getStr[0])
						{
							case "State":
								// 配列の先頭の数値がシグナルタワーの状態となる
								// 緑
								if (getStr[1] == "0")
								{
									this.lightOperation(0);
									alarmstring.Clear();
								}
								// 黄
								else if (getStr[1] == "1")
								{
									this.lightOperation(1);
									alarmstring.Clear();
								}
								// 赤
								else if (getStr[1] == "2")
								{
									this.lightOperation(2);
								}
								// 消
								else if (getStr[1] == "3")
								{
									this.lightOperation(3);
									alarmstring.Clear();
								}
								break;
							case "RequestSetting":
								setData(true);
								break;
							case "Alarm":
								// アラームは「赤」の状態の時だけ受け付ける
								if (lightState == 2)
								{
									// 受信データのシグナルタワー状態を削除
									getStr.RemoveAt(0);
									// 「alarmstring」にない文字列だけを抽出しログに出力し、「alarmstring」に追加
									foreach (var al in getStr.Except(alarmstring))
									{
										alarmLog(al);
										alarmstring.Add(al);
									}
								}
								break;
							case "Operation":
								// データの先頭を削除
								getStr.RemoveAt(0);
								for (int j = 0; j < operationState.Length; j++)
								{
									// 直前までの状態がOFF
									if (operationState[j] == false)
									{
										// この要素番号と同じ文字列が含まれていればスイッチONとする
										if (getStr.Contains(j.ToString()))
										{
											operationState[j] = true;
											// ログ
											operationLog(j, true);
										}
									}
									// 直前までの状態がON
									else
									{
										// この要素番号と同じ文字列が含まれてなければスイッチOFFとする
										if (!getStr.Contains(j.ToString()))
										{
											operationState[j] = false;
											// ログ
											operationLog(j, false);
										}
									}
								}
								break;
						}
					}
					// 定期確認のIntervalはあらためてセット
					operationTimer.Interval = Properties.Settings.Default.RegularlyTime;
					// 次受信開始
					stateObject.workSocket.BeginReceive(stateObject.buffer, 0,
						stateObject.buffer.Length, SocketFlags.None,
						new AsyncCallback(this.ReceiveCallback), stateObject);
				}
				catch (Exception exc)
				{
					// 例外発生時は一度ネットワークを閉じる
					this.close();
					Frm.SysrtmError(exc.StackTrace, null, false);
				}
			}
		}

		/// <summary>
		/// TCP送信
		/// </summary>
		/// <param name="isSendSetting">false:設定値は送信しない true:設定値を送信する</param>
		internal void setData(bool isSendSetting = false)
		{
			if (socket != null)
			{
				try
				{
					// 送信中に終了されるのはダメなので排他処理をする
					lock (Lock)
					{
						// 接続されていれば送信を行う
						if (isConnect == true)
						{
							StringBuilder sendStr = new StringBuilder();
							// 最初にセットする文字を決める
							// 保持点灯の消灯でも状態が返ってくるので「ST」と「Erase」を使い分ける
							if (isErase == true)
							{
								isErase = false;
								sendStr.Append("Erase");
							}
							else
							{
								sendStr.Append("ST");
							}
							// 設定が更新されていたらステータスと一緒に設定情報を送る
							if (isSendSetting == true)
							{
								/***** アラームリスト *****/
								sendStr.Append("\r\nAlarm");
								if (Frm.checkBox2.Checked)
								{
									if (alarmList.Count != 0)
									{
										foreach (var str in alarmList)
										{
											sendStr.Append(",").Append(str);
										}
									}
									else
									{
										sendStr.Append(",Delete List");
									}
								}
								else
								{
									sendStr.Append(",List is not");
								}
								/***** 操作リスト *****/
								sendStr.Append("\r\nOperation");
								if (Frm.checkBox2.Checked)
								{
									if (operationList.Count != 0)
									{
										foreach (var str in operationList)
										{
											sendStr.Append(",").Append(str);
										}
									}
									else
									{
										sendStr.Append(",Delete List");
									}
								}
								else
								{
									sendStr.Append(",List is not");
								}
								/***** 信号保持設定 *****/
								sendStr.Append("\r\nHold");
								if (Frm.checkBox2.Checked == true)
								{
									sendStr.Append(greenKeep == false ? ",0," : ",1,");
									sendStr.Append(yellowKeep == false ? "0," : "1,");
									sendStr.Append(redKeep == false ? "0" : "1");
								}
								else
								{
									sendStr.Append(",List is not");
								}
								// 設定値変更フラグは念のためクリア
								isSetting = false;
							}
							// ステータスコマンド送信(ソケット通信の例外はここでキャッチ)
							try
							{
								socket.Send(Encoding.GetEncoding("UTF-8").GetBytes(sendStr.ToString()));
							}
							catch (SocketException)
							{
								// 一度ネットワークを切断する
								this.close();
								this.lightOperation(4);
								return;
							}
							// 定期確認のIntervalはあらためてセット
							operationTimer.Interval = Properties.Settings.Default.RegularlyTime;
						}
					}
				}
				catch (Exception exc)
				{
					// 例外発生時は一度ネットワークを閉じる
					this.close();
					this.lightOperation(4);
					Frm.SysrtmError(exc.StackTrace);
				}
			}
		}

		#endregion ネットワーク関連記述 -------------------------------------------------------------------------------------------------

		#region タイマー記述 ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

		/// <summary>
		/// 日付が変わった時にログファイルが存在してなければ現在の点灯情報をベースにログファイルを生成する
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void checkLogFile(object sender, System.Timers.ElapsedEventArgs e)
		{
			// 次のターゲットとなる時間をセット(誤差で日付が変わってなければ微小な値がセットされる)
			newDayTimer.Interval = 86400000 - (e.SignalTime.Hour * 3600000 + e.SignalTime.Minute * 60000 + e.SignalTime.Second * 1000 + e.SignalTime.Millisecond);
			// 日にちが変わっていたら新しいログファイルを作る(既にファイルが存在してる場合は何もしない)
			if (oldDay != e.SignalTime.Day)
			{
				oldDay = e.SignalTime.Day;
				LogWrite(lightState);
				// 操作はONしている部分だけ書き込む
				for(int i = 0; i < operationState.Length; i++)
				{
					if (operationState[i] == true)
						operationLog(i, true);
				}
				// 今日のエラー回数をクリアする
				errorToday = 0;
			}
			// 月が変わっていたらエラー回数をクリアする
			if (oldMonth != e.SignalTime.Month)
			{
				oldMonth = e.SignalTime.Month;
				errorCount = 0;
				Occupancy.Clear();
				// すぐ参照されるかもしれないので1つだけ要素を入れておく
				Occupancy.Add(0);
			}
		}

		/// <summary>
		/// 1分ごとに稼働率を計算するタイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OccupancyTime(object sender, System.Timers.ElapsedEventArgs e)
		{
			BeginInvoke((Action)(() =>
			{
				occupancyRateTimer.Stop();
				// 稼働率を入れるListの要素数が今日の日にちより少なかったら要素数を増やす
				if (Occupancy.Count < DateTime.Now.Day)
					Occupancy.Add(0);
				int[] getTime = this.GetTotalTime(DateTime.Now);
				// データがあれば計算する
				if (getTime != null)
				{
					Occupancy[DateTime.Now.Day - 1] = getTime.Sum() == 0 ? 0 : (int)((double)getTime[0] / getTime.Sum() * 100);
				}
				// データがない(ファイルが存在しない)場合は「-1」を入れる
				else
				{
					Occupancy[DateTime.Now.Day - 1] = -1;
				}
				this.Refresh();
				occupancyRateTimer.Interval = 60010 - DateTime.Now.Second * 1000 + DateTime.Now.Millisecond;
				occupancyRateTimer.Start();
			}));
		}

		/// <summary>
		/// 稼動情報を収集するタイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OperationTime(object sender, System.Timers.ElapsedEventArgs e)
		{
			// 接続フラグがセットされていれば接続チェックを兼ねた状態取得
			if(isConnect == true)
				this.setData();
		}

		/// <summary>
		/// ネットワーク接続エラータイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ErrTime(object sender, System.Timers.ElapsedEventArgs e)
		{
			// 稼働マップが表示されている時のみ点滅表示を行う
			if (Frm.viewPanel == 0)
			{
				if (isFlashing)
				{
					BackColor = Color.White;
					ForeColor = Color.Black;
				}
				else
				{
					// シグナルタワーが要因なら赤
					if (lightState == 2)
					{
						BackColor = Properties.Settings.Default.Panel1RedBackColor;
						ForeColor = Properties.Settings.Default.Panel1RedForeColor;
					}
					// ネットワーク接続エラー
					else if (lightState == 4)
					{
						BackColor = Properties.Settings.Default.Panel1CutBackColor;
						ForeColor = Properties.Settings.Default.Panel1CutForeColor;
					}
				}
				isFlashing = !isFlashing;
			}
		}

		/// <summary>
		/// 切断時、定期的に接続を試みるタイマ
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConnectTime(object sender, System.Timers.ElapsedEventArgs e)
		{
			connectTimer.Stop();
			if (isConnect == false)
			{
				if (this.connect() == false)
					connectTimer.Start();
			}
		}

		#endregion タイマー記述 -------------------------------------------------------------------------------------------------

		/// <summary>
		/// 非同期通信でデータが入るクラス
		/// </summary>
		internal class StateObject
		{
			// Client socket.
			public Socket workSocket = null;
			// Receive buffer.
			public byte[] buffer = new byte[2048];
		}
	}
}
