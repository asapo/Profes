using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using System.Windows.Threading;
using Profes.P2P.FileShare.ServiceModel;
using Profes.P2P.FileShare.Properties;
using System.Text.RegularExpressions;
using Profes.BinaryEditorBase;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareNodeControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareNodeControl : UserControl
    {
        DispatcherTimer _timer;
        DispatcherTimer _countTimer;

        public FileShareNodeControl()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Tick += new EventHandler(_timer_Tick);
            _timer.Interval = new TimeSpan(0, 0, 1);
            _timer.Start();

            _countTimer = new DispatcherTimer();
            _countTimer.Tick += new EventHandler(_countTimer_Tick);
            _countTimer.Interval = new TimeSpan(0, 0, 5);
            _countTimer.Start();
            _countTimer_Tick(null, null);

            if (Settings.Default.QueryTimerMaxCount > 3)
            {
                QueryTimerMaxCountTextBox.Text = "3";
            }

            if (Settings.Default.StoreTimerMaxCount > 3)
            {
                StoreTimerMaxCountTextBox.Text = "3";
            }

            if (Settings.Default.DownloadTimerMaxCount > 20)
            {
                DownloadTimerMaxCountTextBox.Text = "20";
            }

            if (Settings.Default.UploadTimerMaxCount > 20)
            {
                UploadTimerMaxCountTextBox.Text = "20";
            }
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            NodeListView.Items.Refresh();
        }

        void _countTimer_Tick(object sender, EventArgs e)
        {
            NodeCountTextBox.Text = Settings.Default._routeTable.Count.ToString();
            KeyCountTextBox.Text = Settings.Default._keyController.KeyList.
                Count(n => !BinaryEditor.ArrayEquals(n.FileLocation.NodeID, Settings.Default._routeTable.MyNode.NodeID)).ToString();
        }

        private void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {

        }

        private void QueryTimerMaxCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Regex.IsMatch(QueryTimerMaxCountTextBox.Text.Trim(), "^[0-9]+$"))
            {
                Settings.Default.QueryTimerMaxCount = int.Parse(QueryTimerMaxCountTextBox.Text.Trim());

                if (Settings.Default.QueryTimerMaxCount > 3)
                {
                    QueryTimerMaxCountTextBox.Text = "3";
                }
            }
        }

        private void StoreTimerMaxCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Regex.IsMatch(StoreTimerMaxCountTextBox.Text.Trim(), "^[0-9]+$"))
            {
                Settings.Default.StoreTimerMaxCount = int.Parse(StoreTimerMaxCountTextBox.Text.Trim());

                if (Settings.Default.StoreTimerMaxCount > 3)
                {
                    StoreTimerMaxCountTextBox.Text = "3";
                }
            }
        }

        private void DownloadTimerMaxCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Regex.IsMatch(DownloadTimerMaxCountTextBox.Text.Trim(), "^[0-9]+$"))
            {
                Settings.Default.DownloadTimerMaxCount = int.Parse(DownloadTimerMaxCountTextBox.Text.Trim());

                if (Settings.Default.DownloadTimerMaxCount > 20)
                {
                    DownloadTimerMaxCountTextBox.Text = "20";
                }
            }
        }

        private void UploadTimerMaxCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Regex.IsMatch(UploadTimerMaxCountTextBox.Text.Trim(), "^[0-9]+$"))
            {
                Settings.Default.UploadTimerMaxCount = int.Parse(UploadTimerMaxCountTextBox.Text.Trim());

                if (Settings.Default.UploadTimerMaxCount > 20)
                {
                    UploadTimerMaxCountTextBox.Text = "20";
                }
            }
        }
    }

    [Serializable]
    public class NodeListViewItem : INotifyPropertyChanged
    {
        DateTime _creationTime;

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public NodeListViewItem()
        {
            _creationTime = DateTime.Now;
        }

        void timer_Tick(object sender, EventArgs e)
        {
            NotifyPropertyChanged(null);
        }

        /// <summary>
        /// 通信タイプを取得または設定します
        /// </summary>
        public string CommunicationType { get; set; }

        /// <summary>
        /// ノード接続に対する説明を取得または設定します
        /// </summary>
        public string Description { get; set; }

        public Node Node { get; set; }

        /// <summary>
        /// ノード接続時間を取得します
        /// </summary>
        public DateTime ConnectionTime { get { return new DateTime((DateTime.Now - _creationTime).Ticks); } }
    }
}