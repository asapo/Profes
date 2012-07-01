using Profes.Window.Properties;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Windows;
using System.Resources;
using System.Reflection;
using System.Threading;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Linq;
using System.Windows.Data;

namespace Profes.Window
{
    /// <summary>
    /// Window1.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        System.Windows.Forms.NotifyIcon notifyIcon = new System.Windows.Forms.NotifyIcon();

        public MainWindow()
        {
#if DEBUG
#else
            // ThreadExceptionイベント・ハンドラを登録する
            System.Windows.Forms.Application.ThreadException += new
              ThreadExceptionEventHandler(Application_ThreadException);

            // UnhandledExceptionイベント・ハンドラを登録する
            Thread.GetDomain().UnhandledException += new
              UnhandledExceptionEventHandler(Application_UnhandledException);
#endif
          
            InitializeComponent();            
        }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            System.Drawing.Icon myIcon = new System.Drawing.Icon(myAssembly.GetManifestResourceStream("Profes.Window.Profes.ico"));

            notifyIcon.Click += new EventHandler(notifyIcon_Click);
            notifyIcon.Visible = true;
            notifyIcon.Icon = new System.Drawing.Icon(myIcon, new System.Drawing.Size(16, 16));
        }

        private void SkinComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string skin = SkinComboBox.SelectedItem as string;
            SkinManager.ChangeSkin(skin);
        }

        private void CultureComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string lang = CultureComboBox.SelectedItem as string;
            LanguageManager.ChangeLanguage(lang);
        }

        bool windowShow = true;
        WindowState windowsState = WindowState.Normal;

        void notifyIcon_Click(object sender, EventArgs e)
        {
            if (windowShow == true)
            {
                this.Hide();
                this.windowsState = this.WindowState;
                windowShow = false;
            }
            else
            {
                this.Show();
                this.WindowState = this.windowsState;
                this.Activate();
                windowShow = true;
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized && this.windowShow == true && TaskTrayCheckBox.IsChecked == true)
            {
                this.Hide();
                this.windowsState = WindowState.Normal;
                windowShow = false;
            }
            else
            {
                this.windowsState = this.WindowState;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Settings.Default.ShutdownQuestionCheckBox_IsChecked == true)
            {
                var Result = System.Windows.MessageBox.Show("Profesを終了しますか？",
                    "Profes, Profes.Window.exe",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question,
                    MessageBoxResult.No);

                if (Result == MessageBoxResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            
            Settings.Default.Window_Height = this.RestoreBounds.Height;
            Settings.Default.Window_Left = this.RestoreBounds.Left;
            Settings.Default.Window_Top = this.RestoreBounds.Top;
            Settings.Default.Window_Width = this.RestoreBounds.Width;

            Settings.Default.Save();

            fileShareControl.Dispose();

            notifyIcon.Visible = false;
        }

        // 未処理例外をキャッチするイベント・ハンドラ
        // （Windowsアプリケーション用）
        public static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            ShowErrorMessage(e.Exception, "Application_ThreadExceptionによる例外通知です。");
        }

        // 未処理例外をキャッチするイベント・ハンドラ
        // （主にコンソール・アプリケーション用）
        public static void Application_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            if (ex != null)
            {
                ShowErrorMessage(ex, "Application_UnhandledExceptionによる例外通知です。");
            }
        }

        // ユーザー・フレンドリなダイアログを表示するメソッド
        public static void ShowErrorMessage(Exception ex, string extraMessage)
        {
            System.Windows.Forms.MessageBox.Show(extraMessage + " \n――――――――\n\n" +
              "エラーが発生しました。\nエラー内容は実行ファイルと同じフォルダのerror.logというファイルに書かれています。" +
              "error.logの内容を開発元にお知らせいただくか、送付してください\n\n" +
              "【エラー内容】\n" + ex.Message + "\n\n" +
              "【スタックトレース】\n" + ex.StackTrace);
            using( StreamWriter sw = new StreamWriter("error.log"))
            {
                sw.WriteLine("\n\n");
                sw.WriteLine(ex.Message.ToString());
                sw.WriteLine("\n\n");
                sw.WriteLine(ex.StackTrace);
            }
        }
    }
}