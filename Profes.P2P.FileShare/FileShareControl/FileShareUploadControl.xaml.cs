using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Profes.P2P.FileShare.Properties;
using Profes.P2P.FileShare.ServiceModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Threading;
using System.Windows.Threading;
using System;
using System.Collections;
using Profes.BinaryEditorBase;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareUploadControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareUploadControl : UserControl
    {
        DispatcherTimer timer;

        /// <summary>
        /// FileShareUploadControlクラスの新しいインスタンスを初期化します
        /// </summary>
        public FileShareUploadControl()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(timer_Tick);
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Start();
        }

        Dictionary<string, string[]> upFileDic = new Dictionary<string, string[]>();

        void timer_Tick(object sender, EventArgs e)
        {
            UploadListView.Items.Refresh();

            var dic = upFileDic;
            upFileDic = new Dictionary<string, string[]>();

            Thread thread = new Thread(new ThreadStart(delegate()
            {
                foreach (string path in dic.Keys)
                {
                    Cache item = null;

                    try
                    {
                        item = Settings.Default._cacheController.FileToCache(path, dic[path]);
                    }
                    catch (ApplicationException ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        if (item != null)
                            Settings.Default._uploadList.Add(new CacheListViewItem(item));
                    }));
                }
            }));

            thread.IsBackground = true;
            thread.Start();
        }

        private void UploadListView_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void UploadListView_PreviewDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            foreach (string uploadFilePath in ((string[])e.Data.GetData(DataFormats.FileDrop)).Where(item => File.Exists(item)).ToArray())
            {
                if (new FileInfo(uploadFilePath).Length == 0) continue;

                string[] category = null;

                using (CategorySettingWindow settingWindow = new CategorySettingWindow())
                {
                    settingWindow.Title = Path.GetFileName(uploadFilePath);
                    settingWindow.ShowDialog();

                    if (settingWindow.DialogResult != true) continue;
                    if (settingWindow.Category.Count(n => n != "") == 0)
                    {
                        MessageBox.Show("カテゴリを少なくとも一つ以上指定してください");

                        continue;
                    }

                    category = settingWindow.Category;
                }

                upFileDic.Add(uploadFilePath, category);
            }
        }

        private void 削除_D_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var items = UploadListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (CacheListViewItem cl in items.OfType<CacheListViewItem>().ToArray())
                {
                    Settings.Default._uploadList.Remove(cl);
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
                        while ((string)UploadListViewGridView.Columns[index].Header != header) index++;

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
                while ((string)UploadListViewGridView.Columns[index].Header != header) index++;

                Sort(index, _lastDirection);
            }
        }

        private void Sort(int sortBy, ListSortDirection direction)
        {
            if (Settings.Default._uploadList.Count == 0) return;

            var slist = new List<CacheListViewItem>();

            switch (sortBy)
            {
                // キャッシュファイル名の比較
                case 0:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Name.CompareTo(y.Name);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._uploadList.ToList();
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
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(x.ID, y.ID);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._uploadList.ToList();
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
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Size.CompareTo(y.Size);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._uploadList.ToList();
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
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.UploadRate.CompareTo(y.UploadRate);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.UploadRate.CompareTo(x.UploadRate);
                        });
                    }
                    break;

                // キャッシュハッシュの比較
                case 4:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(x.Hash, y.Hash);
                        });
                    }
                    else
                    {
                        slist = Settings.Default._uploadList.ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(y.Hash, x.Hash);
                        });
                    }
                    break;
            }

            Settings.Default._uploadList.Clear();

            foreach (CacheListViewItem cc in slist)
            {
                Settings.Default._uploadList.Add(cc);
            }
        }
    }
}