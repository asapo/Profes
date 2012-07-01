using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Xml;
using Profes.BinaryEditorBase;
using Profes.P2P.FileShare.Properties;
using Profes.P2P.FileShare.ServiceModel;
using System.Timers;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Data;
using System.Text;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareQueryControl.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareTreeQueryControl : UserControl
    {
        #region メソッド

        private XmlDocument importXmlDocument;
        private XmlDocument exportXmlDocument;
        
        /// <summary>
        /// FileShareQueryControlクラスの新しいインスタンスを初期化します
        /// </summary>
        public FileShareTreeQueryControl()
        {
            InitializeComponent();

            TreeViewItem tv = ImportXml();
            if (tv != null)
            {
                while (0 < tv.Items.Count)
                {
                    TreeViewItem ci = (TreeViewItem)tv.Items[0];
                    tv.Items.Remove(tv.Items[0]);
                    SearchTreeViewItem.Items.Add(ci);
                }
            }

            Settings.Default.QueryList = GetQuery();
        }

        /// <summary>
        /// クエリを取得します
        /// </summary>
        private string[] GetQuery()
        {
            List<string> hitList = new List<string>();

            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                foreach (TreeViewItem tv in SearchTreeViewItem.Items)
                {
                    hitList.Add((string)tv.Header);
                }
            }));

            return hitList.ToArray();
        }

        /// <summary>
        /// queryTreeViewのTreeViewItemの親をたどる
        /// </summary>
        /// <param name="item">検索元TreeViewItem</param>
        private string[] QueryTreeViewItemParent(TreeViewItem item)
        {
            var itemList = new List<TreeViewItem>();
            var hitList = new List<string>();

            itemList.Add(SearchTreeViewItem);

            for (int i = 0; i < itemList.Count; i++)
            {
                foreach (TreeViewItem tv in itemList[i].Items)
                {
                    itemList.Add(tv);

                    if (item.IsDescendantOf(tv))
                    {
                        hitList.Add((string)tv.Header);
                    }
                }
            }

            return hitList.ToArray();
        }

        private void queryTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (queryTextBox.Text.Trim() == "") return;

            if ((sender == null && e == null) || e.Key == System.Windows.Input.Key.Enter)
            {
                var selectItem = queryTreeView.SelectedItem as TreeViewItem;
                var newItem = new TreeViewItem { Header = queryTextBox.Text };
                newItem.Collapsed += new RoutedEventHandler(newItem_Collapsed);
                newItem.Expanded += new RoutedEventHandler(newItem_Expanded);
                newItem.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(newItem_PreviewMouseDown);
                newItem.Selected += new System.Windows.RoutedEventHandler(newItem_Selected);

                selectItem.IsExpanded = true;
                selectItem.Items.Add(newItem);

                ExportXml(SearchTreeViewItem);
            }

            Settings.Default.QueryList = GetQuery();
        }

        void newItem_Collapsed(object sender, RoutedEventArgs e)
        {
            ExportXml(SearchTreeViewItem);
        }

        void newItem_Expanded(object sender, RoutedEventArgs e)
        {
            ExportXml(SearchTreeViewItem);
        }

        void newItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectItem = sender as TreeViewItem;
            List<string> searchWordList = new List<string>(QueryTreeViewItemParent(selectItem));
            List<CacheListViewItem> searchCacheList = new List<CacheListViewItem>();

            if (searchWordList.Count == 0)
            {
                // -----保有するクエリをすべて表示する-----
                foreach (CacheListViewItem item in Settings.Default._keyController.CacheListViewItemList)
                {
                    searchCacheList.Add(item);
                }
            }
            else
            {
                // -----保有するクエリから検索する-----
                foreach (CacheListViewItem item in FileShareFilterControl.CacheListViewItemListFilter(Settings.Default._keyController.CacheListViewItemList))
                {
                    if (item.Category.Any(n => n.ToLower() == searchWordList[0].ToLower()))
                    {
                        bool cpyFlag = true;
                         
                        for (int j = 1; j < searchWordList.Count; j++)
                        {
                            cpyFlag &= searchWordList[j].Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                                .All(mat => (!mat.StartsWith("-") && item.Name.Contains(mat)) || (mat.StartsWith("-") && !item.Name.Contains(mat.Substring(1))));
                        }

                        if (cpyFlag == true) searchCacheList.Add(item);
                    }
                }
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
        }

        void newItem_Selected(object sender, System.Windows.RoutedEventArgs e)
        {
        }

        private void queryListView_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
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

        private void クエリ追加_Q_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            queryTextBox_KeyDown(null, null);
        }

        private void 削除_D_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = queryTreeView.SelectedItem as TreeViewItem;

            if (item == null || item == (TreeViewItem)SearchTreeViewItem) return;

            List<TreeViewItem> itemList = new List<TreeViewItem>();
            List<TreeViewItem> hitList = new List<TreeViewItem>();

            itemList.Add((TreeViewItem)SearchTreeViewItem);
            hitList.Add((TreeViewItem)SearchTreeViewItem);

            for (int i = 0; i < itemList.Count; i++)
            {
                foreach (TreeViewItem tv in itemList[i].Items)
                {
                    itemList.Add(tv);

                    if (item.IsDescendantOf(tv))
                    {
                        hitList.Add(tv);
                    }
                }
            }

            hitList[hitList.Count - 2].Items.Remove(item);

            ExportXml(SearchTreeViewItem);
        }

        private void ダウンロード_D_Click(object sender, RoutedEventArgs e)
        {
            var items = queryListView.SelectedItems as IList;

            if (items != null)
            {
                foreach (CacheListViewItem cl in items.OfType<CacheListViewItem>().ToArray())
                {
                    Settings.Default._downloadList.Add(cl);
                    cl.IsChecked = true;
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

        #endregion

        public TreeViewItem ImportXml()
        {
            importXmlDocument = new XmlDocument();

            if (!new FileInfo("QueryTreeViewNode.xml").Exists) return null;

            using (FileStream stream = new FileStream("QueryTreeViewNode.xml", FileMode.Open))
            {
                importXmlDocument.Load(stream);
                XmlNode rootXmlNode = importXmlDocument.DocumentElement;

                NodeInfo ni = new NodeInfo();
                ni.Query = ((XmlElement)rootXmlNode).GetAttribute("name");
                ni.IsExpanded = ((XmlElement)rootXmlNode).GetAttribute("expand") == "true" ? true : false;

                TreeViewItem rootTreeNode = new TreeViewItem() { Header = ni.Query, IsExpanded = ni.IsExpanded };
                RecursiveXmlToTreeViewItem(rootXmlNode, rootTreeNode);

                return rootTreeNode;
            }
        }

        public void ExportXml(TreeViewItem item)
        {
            exportXmlDocument = new XmlDocument();

            using (FileStream stream = new FileStream("QueryTreeViewNode.xml", FileMode.Create))
            {
                XmlDeclaration xmlDeclaration = exportXmlDocument.CreateXmlDeclaration("1.0", "UTF-8", null);
                exportXmlDocument.AppendChild(xmlDeclaration);

                NodeInfo ni = new NodeInfo() { Query = (string)item.Header, IsExpanded = item.IsExpanded };
                XmlElement xe = exportXmlDocument.CreateElement("item");
                xe.SetAttribute("name", ni.Query);

                if (item.IsExpanded)
                {
                    xe.SetAttribute("expand", "true");
                }
                // ルートノードをXmlDocumentに追加
                exportXmlDocument.AppendChild(xe);

                // 再帰的にツリーノードを読み込み、XmlDocument構築
                RecursiveTreeViewItemToXml(item, xe);

                // ファイルに出力
                exportXmlDocument.Save(stream);
            }
        }


        private void RecursiveXmlToTreeViewItem(XmlNode Parentnode, TreeViewItem Parenttreeviewitem)
        {
            foreach (XmlNode childXmlNode in Parentnode.ChildNodes)
            {
                NodeInfo ni = new NodeInfo();

                ni.Query = ((XmlElement)childXmlNode).GetAttribute("name");
                ni.IsExpanded = ((XmlElement)childXmlNode).GetAttribute("expand") == "true" ? true : false;

                if (ni.Query.Trim()=="") continue;

                TreeViewItem newItem = new TreeViewItem() { Header = ni.Query, IsExpanded = ni.IsExpanded };
                newItem.Collapsed += new RoutedEventHandler(newItem_Collapsed);
                newItem.Expanded += new RoutedEventHandler(newItem_Expanded);
                newItem.PreviewMouseDown += new System.Windows.Input.MouseButtonEventHandler(newItem_PreviewMouseDown);
                newItem.Selected += new System.Windows.RoutedEventHandler(newItem_Selected);

                Parenttreeviewitem.Items.Add(newItem);
                RecursiveXmlToTreeViewItem(childXmlNode, newItem);
            }
        }

        private void RecursiveTreeViewItemToXml(TreeViewItem Parenttreeviewitem, XmlNode ParentXmlNode)
        {
            foreach (TreeViewItem childTreeNode in Parenttreeviewitem.Items)
            {
                NodeInfo ni = new NodeInfo() { Query = (string)childTreeNode.Header, IsExpanded = childTreeNode.IsExpanded };

                XmlElement xe = exportXmlDocument.CreateElement("item");

                xe.SetAttribute("name", ni.Query);
                if (childTreeNode.IsExpanded == true)
                {
                    xe.SetAttribute("expand", "true");
                }

                ParentXmlNode.AppendChild(xe);
                RecursiveTreeViewItemToXml(childTreeNode, xe);
            }
        }

        public class NodeInfo
        {
            public NodeInfo() { }

            public string Query { get; set; }
            public bool IsExpanded { get; set; }
        }
    }
}