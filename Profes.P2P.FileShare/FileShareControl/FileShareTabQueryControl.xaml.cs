using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Profes.BinaryEditorBase;
using Profes.P2P.FileShare.Properties;
using Profes.P2P.FileShare.ServiceModel;
using Profes.Security.Cryptography;
using System.Collections;
using System.ComponentModel;
using System.Text;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareTabQueryControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareTabQueryControl : UserControl
    {
        public FileShareTabQueryControl()
        {
            InitializeComponent();

            if (Settings.Default.TabQueryList != null)
            {
                foreach (string ss in Settings.Default.TabQueryList)
                {
                    ButtonAdd(ss);
                }
            }
        }

        Button RightDownButton = null;
        Button DragButton = null;

        void ButtonAdd(string message)
        {
            if (wrapPanel1.Children.Cast<Button>().Select(n => n.Content.ToString()).Contains(message))
            {
                return;
            }
            else if (message.Trim().Length == 0)
            {
                return;
            }

            Button b = new Button()
            {
                Content = message,
                AllowDrop = true,
                MinWidth = 50,
                MaxHeight = 24,
                Margin = new Thickness(2),
            };

            b.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(b_PreviewMouseLeftButtonDown);
            b.PreviewMouseLeftButtonUp += new MouseButtonEventHandler(b_PreviewMouseLeftButtonUp);
            b.PreviewMouseMove += new MouseEventHandler(b_PreviewMouseMove);
            b.PreviewMouseRightButtonDown += new MouseButtonEventHandler(b_PreviewMouseRightButtonDown);
            wrapPanel1.Children.Add(b);

            Settings.Default.TabQueryList = wrapPanel1.Children.Cast<Button>().Select(n => n.Content.ToString()).ToArray();
        }

        void b_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            RightDownButton = sender as Button;
        }

        void b_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                DragButton = null;
            }
        }

        void b_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var oldDragButton = sender as Button;

            if (oldDragButton != null)
            {
                List<CacheListViewItem> searchCacheList = new List<CacheListViewItem>();
                string searchWord = oldDragButton.Content.ToString();

                foreach (CacheListViewItem item in FileShareFilterControl.CacheListViewItemListFilter(Settings.Default._keyController.CacheListViewItemList))
                {
                    bool cpyFlag = true;

                    cpyFlag &= searchWord.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                        .All(mat => (!mat.StartsWith("-") && item.Name.Contains(mat)) || (mat.StartsWith("-") && !item.Name.Contains(mat.Substring(1))));

                    cpyFlag &= Convert.ToBase64String(HashFunction.HashCreate(item.PublicKey)).StartsWith(textBox2.Text);

                    if (cpyFlag == true) searchCacheList.Add(item);
                }

                for (int i = 0; i < searchCacheList.Count; i++)
                {
                    if (Settings.Default._downloadList.Any(n => BinaryEditor.ArrayEquals(n.Hash, searchCacheList[i].Hash)))
                    {
                        searchCacheList[i].IsChecked = true;
                    }
                }

                // -----queryListViewにリストを追加する-----
                queryListView.ItemsSource = searchCacheList.ToArray();
                GridViewColumnHeaderClickedHandler(this, null);

                HitLabel.Content = "Hit: " + searchCacheList.Count.ToString();
            }

            if (DragButton != null && oldDragButton != null)
            {
                textBox1.Text = DragButton.Content.ToString();

                if (DragButton == oldDragButton)
                {
                    DragButton = null;
                }
                else
                {
                    try
                    {
                        wrapPanel1.Children.Remove(DragButton);
                        wrapPanel1.Children.Insert(wrapPanel1.Children.IndexOf(oldDragButton), DragButton);
                    }
                    catch
                    {
                        wrapPanel1.Children.Add(DragButton);
                    }
                }
            }

            e.Handled = true;
        }

        void b_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragButton = sender as Button;

            e.Handled = true;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((sender == null && e == null) || e.Key == System.Windows.Input.Key.Enter)
            {
                ButtonAdd(textBox1.Text);
                textBox1.Text = "";
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            textBox1_KeyDown(null, null);
        }

        private void 削除_D_Click(object sender, RoutedEventArgs e)
        {
            if (RightDownButton != null)
            {
                wrapPanel1.Children.Remove(RightDownButton);
                Settings.Default.TabQueryList = wrapPanel1.Children.Cast<Button>().Select(n => n.Content.ToString()).ToArray();
            }
        }

        private void queryListView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = queryListView.SelectedItem as CacheListViewItem;

            if (item != null)
            {
                if (Settings.Default._downloadList.Any(n => BinaryEditor.ArrayEquals(n.Hash, item.Hash)))
                {
                    int index = 0;
                    while (!BinaryEditor.ArrayEquals(Settings.Default._downloadList[index].Hash, item.Hash)) index++;
                    Settings.Default._downloadList.RemoveAt(index);
                    item.IsChecked = false;
                }
                else
                {
                    Settings.Default._downloadList.Add(item);
                    item.IsChecked = true;
                }
            }
        }

        private void ダウンロード_D_Click(object sender, RoutedEventArgs e)
        {
            var items = queryListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (CacheListViewItem cl in items.OfType<CacheListViewItem>().ToArray())
                {
                    Settings.Default._downloadList.Add(cl);
                }
            }
        }

        private void アップロード_U_Click(object sender, RoutedEventArgs e)
        {
            var items = queryListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (CacheListViewItem cl in items.OfType<CacheListViewItem>().ToArray())
                {
                    if (cl.Rate != 100) continue;

                    cl.UploadBlockBitmap = new bool[cl.UploadBlockBitmap.Length];
                    Settings.Default._uploadList.Add(cl);
                }
            }
        }

        private void コピー_C_Click(object sender, RoutedEventArgs e)
        {
            var item = queryListView.SelectedItem as CacheListViewItem;

            if (item != null)
            {
                StringBuilder sb = new StringBuilder();

                // ファイル名
                sb.Append(item.Name);
                sb.Append("\r\n");

                // カテゴリリスト
                foreach (string ss in item.Category)
                {
                    sb.Append("\"" + ss + "\",");
                }
                sb.Append("\r\n");

                // サイン
                sb.Append(item.Sign);
                sb.Append("\r\n");

                // ID
                sb.Append(Convert.ToBase64String(item.SignatureHash));
                sb.Append("\r\n");

                // サイズ
                sb.Append(item.Size.ToString());
                sb.Append("\r\n");

                // ハッシュ
                sb.Append(BinaryEditor.BytesToHexString(item.Hash));
                sb.Append("\r\n");

                Clipboard.SetText(sb.ToString());
            }
        }

        private void 変換_T_Click(object sender, RoutedEventArgs e)
        {
            var items = queryListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (CacheListViewItem cl in items.OfType<CacheListViewItem>().ToArray())
                {
                    Cache cache;

                    lock (Settings.Default._cacheController.CacheList)
                    {
                        cache = Settings.Default._cacheController.CacheList.
                            FirstOrDefault(n => BinaryEditor.ArrayEquals(n.SignatureHash, cl.SignatureHash));
                    }

                    if (cache == null) continue;

                    if (Settings.Default._cacheController.Rate(cache) == 100)
                    {
                        try
                        {
                            Settings.Default._cacheController.CacheToFile(cache, Settings.Default.DownloadDirectoryPath);
                        }
                        catch (ApplicationException ex)
                        {
                            MessageBox.Show(ex.Message, cl.Name);
                        }
                    }
                    else
                    {
                        MessageBox.Show("キャッシュ率が100%ではありません", cl.Name);
                    }
                }
            }
        }

        CommentsWindow cw;

        private void 評価_R_Click(object sender, RoutedEventArgs e)
        {
            var item = queryListView.SelectedItem as CacheListViewItem;

            if (item != null)
            {
                cw = new CommentsWindow();

                double newLeft = Mouse.GetPosition(Application.Current.Windows[0]).X + Application.Current.Windows[0].Left;
                double newTop = Mouse.GetPosition(Application.Current.Windows[0]).Y + Application.Current.Windows[0].Top;

                double maxTop = System.Windows.Forms.Screen.FromPoint(
                    new System.Drawing.Point((int)newLeft, (int)newTop)).Bounds.Bottom - cw.Height;
                double maxLeft = System.Windows.Forms.Screen.FromPoint(
                    new System.Drawing.Point((int)newLeft, (int)newTop)).Bounds.Right - cw.Width;

                cw.Left = Math.Min(maxLeft, newLeft);
                cw.Top = Math.Min(maxTop, newTop);

                cw.SignatureHash = item.SignatureHash;
                cw.Show();
                System.Diagnostics.Debug.WriteLine(cw.Width.ToString());
            }
        }

        private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            評価_R_Click(null, null);
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
                        while ((string)queryListViewGridView.Columns[index].Header != header) index++;

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
                while ((string)queryListViewGridView.Columns[index].Header != header) index++;

                Sort(index, _lastDirection);
            }
        }

        private void Sort(int sortBy, ListSortDirection direction)
        {
            if (queryListView.ItemsSource == null) return;

            var slist = new List<CacheListViewItem>();

            switch (sortBy)
            {
                // キャッシュファイル名の比較
                case 0:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Name.CompareTo(y.Name);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Name.CompareTo(x.Name);
                        });
                    }
                    break;

                // キャッシュカテゴリーリストの長さの比較
                case 1:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Category.Count(n => n == "").CompareTo(y.Category.Count(n => n == ""));
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Category.Count(n => n == "").CompareTo(x.Category.Count(n => n == ""));
                        });
                    }
                    break;

                // サインの比較
                case 2:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            if (x.Sign != null && y.Sign != null)
                                return x.Sign.CompareTo(y.Sign);
                            else if (x.Sign == null && y.Sign == null)
                                return 0;
                            else if (x.Sign == null)
                                return -1;
                            else
                                return 1;
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            if (x.Sign != null && y.Sign != null)
                                return y.Sign.CompareTo(x.Sign);
                            else if (x.Sign == null && y.Sign == null)
                                return 0;
                            else if (y.Sign == null)
                                return -1;
                            else
                                return 1;
                        });
                    }
                    break;

                // キャッシュIDの比較
                case 3:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(x.ID, y.ID);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(y.ID, x.ID);
                        });
                    }
                    break;

                // キャッシュサイズの比較
                case 4:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Size.CompareTo(y.Size);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Size.CompareTo(x.Size);
                        });
                    }
                    break;

                // キャッシュ率の比較
                case 5:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.Rate.CompareTo(y.Rate);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.Rate.CompareTo(x.Rate);
                        });
                    }
                    break;

                // Review率の比較
                case 6:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.ReviewRate.CompareTo(y.ReviewRate);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.ReviewRate.CompareTo(x.ReviewRate);
                        });
                    }
                    break;

                // キャッシュ作成時間の比較
                case 7:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return x.CreationTime.CompareTo(y.CreationTime);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return y.CreationTime.CompareTo(x.CreationTime);
                        });
                    }
                    break;

                // キャッシュハッシュの比較
                case 8:
                    if (direction == ListSortDirection.Ascending)
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(x.Hash, y.Hash);
                        });
                    }
                    else
                    {
                        slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
                        slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                        {
                            return BinaryEditor.ByteArraryCompare(y.Hash, x.Hash);
                        });
                    }
                    break;
            }

            queryListView.ItemsSource = slist.ToArray();
        }
    }
}