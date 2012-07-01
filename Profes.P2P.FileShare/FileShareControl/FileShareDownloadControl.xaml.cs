using System.Windows.Controls;
using System.Linq;
using Profes.P2P.FileShare.Properties;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Profes.BinaryEditorBase;
using System.Windows;
using System.Windows.Threading;
using System;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareDownloadControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareDownloadControl : UserControl
    {
        DispatcherTimer timer;

        /// <summary>
        /// FileShareDownloadControlクラスの新しいインスタンスを初期化します
        /// </summary>
        public FileShareDownloadControl()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            DownloadListView.Items.Refresh();
        }

        private void 削除_D_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var items = DownloadListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (CacheListViewItem cl in items.OfType<CacheListViewItem>().ToArray())
                {
                    Settings.Default._downloadList.Remove(cl);
                }
            }
        }

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e)
        {
            if (e != null)
            {
                GridViewColumnHeader headerClicked = e.OriginalSource as GridViewColumnHeader;
                ListSortDirection direction;

                if (headerClicked != null)
                {
                    if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        string header = headerClicked.Column.Header as string;
                        int index = 0;
                        while ((string)DownloadListViewGridView.Columns[index].Header != header) index++;

                        Sort(index, direction);

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
            else if (e == null && _lastHeaderClicked != null)
            {
                string header = _lastHeaderClicked.Column.Header as string;
                int index = 0;
                while ((string)DownloadListViewGridView.Columns[index].Header != header) index++;

                Sort(index, _lastDirection);
            }
        }

        private void Sort(int sortBy, ListSortDirection direction)
        {
            if (Settings.Default._downloadList.Count == 0) return;

            var slist = new List<CacheListViewItem>();

            switch (sortBy)
            {
                // キャッシュファイル名の比較
                case 0:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Name.CompareTo(y.Name);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Name.CompareTo(x.Name);
                        });
                    }
                    break;

                // キャッシュIDの比較
                case 1:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(x.ID, y.ID);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(y.ID, x.ID);
                        });
                    }
                    break;

                // キャッシュサイズの比較
                case 2:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Size.CompareTo(y.Size);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Size.CompareTo(x.Size);
                        });
                    }
                    break;

                // キャッシュ率の比較
                case 3:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Rate.CompareTo(y.Rate);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Rate.CompareTo(x.Rate);
                        });
                    }
                    break;

                // キャッシュハッシュの比較
                case 4:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(x.Hash, y.Hash);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._downloadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(y.Hash, x.Hash);
                        });
                    }
                    break;
            }

            Settings.Default._downloadList.Clear();

            foreach (CacheListViewItem cc in slist)
            {
                Settings.Default._downloadList.Add(cc);
            }
        }
    }
}