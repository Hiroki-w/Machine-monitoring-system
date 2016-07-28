using System;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace 装置監視システム
{
	static class Program
	{
		/// <summary>
		/// アプリケーションのメイン エントリ ポイントです。
		/// </summary>
		[STAThread]
		static void Main()
		{
			// ThreadExceptionイベント・ハンドラを登録する
			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);

			// UnhandledExceptionイベント・ハンドラを登録する
			System.Threading.Thread.GetDomain().UnhandledException += new UnhandledExceptionEventHandler(Application_UnhandledException);

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);

			// Mutex の新しいインスタンスを生成する (多重起動防止用)
			System.Threading.Mutex hMutex = new System.Threading.Mutex(false, Application.ProductName);
			if (hMutex.WaitOne(0, false))
				Application.Run(new Form1());
			// GC.KeepAlive メソッドが呼び出されるまで、ガベージ コレクション対象から除外される (多重起動防止用)
			GC.KeepAlive(hMutex);
			// Mutex を閉じる (多重起動防止用)
			hMutex.Close();

		}

		public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			Exception ex = e.Exception as Exception;
			SysrtmError(ex.StackTrace);
		}

		public static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Exception ex = e.ExceptionObject as Exception;
			SysrtmError(ex.StackTrace);
		}
		static void SysrtmError(string message, string addstring = null)
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
		}
	}
}
