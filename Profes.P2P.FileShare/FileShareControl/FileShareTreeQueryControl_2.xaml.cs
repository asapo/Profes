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
using System.Collections;
using System.Runtime.Serialization;
using Profes.P2P.FileShare.Properties;
using Profes.BinaryEditorBase;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using Profes.P2P.FileShare.ServiceModel;
using Profes.Security.Cryptography;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;
using Profes.DialogBox;
using System.Reflection;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareTreeQueryControl_2.xaml の相互作用ロジック
    /// </summary>
    public partial class FileShareTreeQueryControl_2 : UserControl
    {
        private RefineSearchTreeViewItem _searchTreeViewItem;

        public FileShareTreeQueryControl_2()
        {
            InitializeComponent();

            _searchTreeViewItem = Clone.DeepCopyClone<RefineSearchTreeViewItem>(Settings.Default.SearchTreeViewItem);
            queryTreeView.Items.Add(_searchTreeViewItem);
            _searchTreeViewItem.IsSelected = true;
        }

        /// <summary>
        /// queryTreeViewのFilterTreeViewItemの親をたどる
        /// </summary>
        /// <param name="item">検索元TreeViewItem</param>
        private RefineSearchTreeViewItem QueryTreeViewItemParent(RefineSearchTreeViewItem item)
        {
            var itemList = new List<RefineSearchTreeViewItem>();
            var hitItem = new RefineSearchTreeViewItem();

            hitItem = AddRefineToTheRefine(hitItem, _searchTreeViewItem);
            itemList.Add(_searchTreeViewItem);

            for (int i = 0; i < itemList.Count; i++)
            {
                foreach (RefineSearchTreeViewItem f in itemList[i].Items)
                {
                    itemList.Add(f);

                    if (item.IsDescendantOf(f))
                    {
                        hitItem = AddRefineToTheRefine(hitItem, f);
                    }
                }
            }

            return hitItem;
        }

        /// <summary>
        /// xとyを統合する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public RefineSearchTreeViewItem AddRefineToTheRefine(RefineSearchTreeViewItem x, RefineSearchTreeViewItem y)
        {
            RefineSearchTreeViewItem item = new RefineSearchTreeViewItem();

            // Header
            item.RefineSearchName = x.RefineSearchName + " > " + y.RefineSearchName;

            // RefineSearchFileNameList
            if (x.RefineSearchFileNameListEnabled)
            {
                foreach (var i in x.RefineSearchFileNameList) item.RefineSearchFileNameList.Add(i);
                item.RefineSearchFileNameListEnabled = true;
            }
            if (y.RefineSearchFileNameListEnabled)
            {
                foreach (var i in y.RefineSearchFileNameList) item.RefineSearchFileNameList.Add(i);
                item.RefineSearchFileNameListEnabled = true;
            }

            // RefineSearchRegexFileNameList
            if (x.RefineSearchRegexFileNameListEnabled)
            {
                foreach (var i in x.RefineSearchRegexFileNameList) item.RefineSearchRegexFileNameList.Add(i);
                item.RefineSearchRegexFileNameListEnabled = true;
            }
            if (y.RefineSearchRegexFileNameListEnabled)
            {
                foreach (var i in y.RefineSearchRegexFileNameList) item.RefineSearchRegexFileNameList.Add(i);
                item.RefineSearchRegexFileNameListEnabled = true;
            }

            // RefineSearchCategoryList
            if (x.RefineSearchCategoryListEnabled)
            {
                foreach (var i in x.RefineSearchCategoryList) item.RefineSearchCategoryList.Add(i);
                item.RefineSearchCategoryListEnabled = true;
            }
            if (y.RefineSearchCategoryListEnabled)
            {
                foreach (var i in y.RefineSearchCategoryList) item.RefineSearchCategoryList.Add(i);
                item.RefineSearchCategoryListEnabled = true;
            }

            // RefineSearchIdList
            if (x.RefineSearchIdListEnabled)
            {
                foreach (var i in x.RefineSearchIdList) item.RefineSearchIdList.Add(i);
                item.RefineSearchIdListEnabled = true;
            }
            if (y.RefineSearchIdListEnabled)
            {
                foreach (var i in y.RefineSearchIdList) item.RefineSearchIdList.Add(i);
                item.RefineSearchIdListEnabled = true;
            }

            // RefineSearchSize
            if (x.RefineSearchSizeEnabled == true && y.RefineSearchSizeEnabled == true)
            {
                item.RefineSearchSize.UpperLimit = Math.Max(x.RefineSearchSize.UpperLimit, y.RefineSearchSize.UpperLimit);
                item.RefineSearchSize.LowerLimit = Math.Min(x.RefineSearchSize.LowerLimit, y.RefineSearchSize.LowerLimit);
                item.RefineSearchSizeEnabled = true;
            }
            else if (x.RefineSearchSizeEnabled)
            {
                item.RefineSearchSize.UpperLimit = x.RefineSearchSize.UpperLimit;
                item.RefineSearchSize.LowerLimit = x.RefineSearchSize.LowerLimit;
                item.RefineSearchSizeEnabled = true;
            }
            else if (y.RefineSearchSizeEnabled)
            {
                item.RefineSearchSize.UpperLimit = y.RefineSearchSize.UpperLimit;
                item.RefineSearchSize.LowerLimit = y.RefineSearchSize.LowerLimit;
                item.RefineSearchSizeEnabled = true;
            }

            // RefineSearchDownloadSuccessRate
            if (x.RefineSearchDownloadSuccessRateEnabled == true && y.RefineSearchDownloadSuccessRateEnabled == true)
            {
                item.RefineSearchDownloadSuccessRate.UpperLimit = Math.Max(x.RefineSearchDownloadSuccessRate.UpperLimit, y.RefineSearchDownloadSuccessRate.UpperLimit);
                item.RefineSearchDownloadSuccessRate.LowerLimit = Math.Min(x.RefineSearchDownloadSuccessRate.LowerLimit, y.RefineSearchDownloadSuccessRate.LowerLimit);
                item.RefineSearchDownloadSuccessRateEnabled = true;
            }
            else if (x.RefineSearchDownloadSuccessRateEnabled)
            {
                item.RefineSearchDownloadSuccessRate.UpperLimit = x.RefineSearchDownloadSuccessRate.UpperLimit;
                item.RefineSearchDownloadSuccessRate.LowerLimit = x.RefineSearchDownloadSuccessRate.LowerLimit;
                item.RefineSearchDownloadSuccessRateEnabled = true;
            }
            else if (y.RefineSearchDownloadSuccessRateEnabled)
            {
                item.RefineSearchDownloadSuccessRate.UpperLimit = y.RefineSearchDownloadSuccessRate.UpperLimit;
                item.RefineSearchDownloadSuccessRate.LowerLimit = y.RefineSearchDownloadSuccessRate.LowerLimit;
                item.RefineSearchDownloadSuccessRateEnabled = true;
            }

            // RefineSearchStatus
            if (x.RefineSearchStatusEnabled == true && y.RefineSearchStatusEnabled == true)
            {
                item.RefineSearchStatus.UpperLimit = Math.Max(x.RefineSearchStatus.UpperLimit, y.RefineSearchStatus.UpperLimit);
                item.RefineSearchStatus.LowerLimit = Math.Min(x.RefineSearchStatus.LowerLimit, y.RefineSearchStatus.LowerLimit);
                item.RefineSearchStatusEnabled = true;
            }
            else if (x.RefineSearchStatusEnabled)
            {
                item.RefineSearchStatus.UpperLimit = x.RefineSearchStatus.UpperLimit;
                item.RefineSearchStatus.LowerLimit = x.RefineSearchStatus.LowerLimit;
                item.RefineSearchStatusEnabled = true;
            }
            else if (y.RefineSearchStatusEnabled)
            {
                item.RefineSearchStatus.UpperLimit = y.RefineSearchStatus.UpperLimit;
                item.RefineSearchStatus.LowerLimit = y.RefineSearchStatus.LowerLimit;
                item.RefineSearchStatusEnabled = true;
            }

            // RefineSearchReview
            if (x.RefineSearchReviewEnabled == true && y.RefineSearchReviewEnabled == true)
            {
                item.RefineSearchReview.UpperLimit = Math.Max(x.RefineSearchReview.UpperLimit, y.RefineSearchReview.UpperLimit);
                item.RefineSearchReview.LowerLimit = Math.Min(x.RefineSearchReview.LowerLimit, y.RefineSearchReview.LowerLimit);
                item.RefineSearchReviewEnabled = true;
            }
            else if (x.RefineSearchReviewEnabled)
            {
                item.RefineSearchReview.UpperLimit = x.RefineSearchReview.UpperLimit;
                item.RefineSearchReview.LowerLimit = x.RefineSearchReview.LowerLimit;
                item.RefineSearchReviewEnabled = true;
            }
            else if (y.RefineSearchReviewEnabled)
            {
                item.RefineSearchReview.UpperLimit = y.RefineSearchReview.UpperLimit;
                item.RefineSearchReview.LowerLimit = y.RefineSearchReview.LowerLimit;
                item.RefineSearchReviewEnabled = true;
            }

            // RefineSearchStatus
            if (x.RefineSearchStatusEnabled == true && y.RefineSearchStatusEnabled == true)
            {
                item.RefineSearchStatus.UpperLimit = Math.Max(x.RefineSearchStatus.UpperLimit, y.RefineSearchStatus.UpperLimit);
                item.RefineSearchStatus.LowerLimit = Math.Min(x.RefineSearchStatus.LowerLimit, y.RefineSearchStatus.LowerLimit);
                item.RefineSearchStatusEnabled = true;
            }
            else if (x.RefineSearchStatusEnabled)
            {
                item.RefineSearchStatus.UpperLimit = x.RefineSearchStatus.UpperLimit;
                item.RefineSearchStatus.LowerLimit = x.RefineSearchStatus.LowerLimit;
                item.RefineSearchStatusEnabled = true;
            }
            else if (y.RefineSearchStatusEnabled)
            {
                item.RefineSearchStatus.UpperLimit = y.RefineSearchStatus.UpperLimit;
                item.RefineSearchStatus.LowerLimit = y.RefineSearchStatus.LowerLimit;
                item.RefineSearchStatusEnabled = true;
            }

            // RefineSearchHashList
            if (x.RefineSearchHashListEnabled)
            {
                foreach (var i in x.RefineSearchHashList) item.RefineSearchHashList.Add(i);
                item.RefineSearchHashListEnabled = true;
            }
            if (y.RefineSearchHashListEnabled)
            {
                foreach (var i in y.RefineSearchHashList) item.RefineSearchHashList.Add(i);
                item.RefineSearchHashListEnabled = true;
            }

            return item;
        }

        bool _searchRunning = false;

        private void queryTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var selectItem = e.Source as RefineSearchTreeViewItem;
            List<CacheListViewItem> searchCacheList = new List<CacheListViewItem>();

            if (selectItem == null) return;
            var addItem = QueryTreeViewItemParent(selectItem);

            ProgressWindow pw = new ProgressWindow();
            pw.Title = addItem.RefineSearchName;
            pw.Topmost = true;
            pw.MessageLabel.Content = "Key情報の検索中...";
            pw.ProgressBar.IsIndeterminate = true;

            Thread progressThread = new Thread(new ThreadStart(delegate()
            {
                if (_searchRunning == true) return;
                _searchRunning = true;

                try
                {
                    Thread searchThread = new Thread(new ThreadStart(delegate()
                    {
                        foreach (var item in FileShareFilterControl.CacheListViewItemListFilter(Settings.Default._keyController.CacheListViewItemList))
                        {
                            if (addItem.RefineSearchFileNameListEnabled)
                            {
                                foreach (var ss in addItem.RefineSearchFileNameList)
                                {
                                    if (ss.Include && !Cache.CategoryRegularization(item.Name).Contains(Cache.CategoryRegularization(ss.Value)))
                                        goto End;
                                    else if (!ss.Include && Cache.CategoryRegularization(item.Name).Contains(Cache.CategoryRegularization(ss.Value)))
                                        goto End;
                                }
                            }

                            if (addItem.RefineSearchRegexFileNameListEnabled)
                            {
                                foreach (var ss in addItem.RefineSearchRegexFileNameList)
                                {
                                    if (ss.Include && !Regex.IsMatch(item.Name, ss.Value))
                                        goto End;
                                    else if (!ss.Include && Regex.IsMatch(item.Name, ss.Value))
                                        goto End;
                                }
                            }

                            if (addItem.RefineSearchCategoryListEnabled)
                            {
                                foreach (var ss in addItem.RefineSearchCategoryList)
                                {
                                    if (ss.Include && !addItem.RefineSearchCategoryList.Any(n => item.Category.Any(m => Cache.CategoryRegularization(n.Value) == Cache.CategoryRegularization(m))))
                                        goto End;
                                    else if (!ss.Include && addItem.RefineSearchCategoryList.Any(n => item.Category.Any(m => Cache.CategoryRegularization(n.Value) == Cache.CategoryRegularization(m))))
                                        goto End;
                                }
                            }

                            if (addItem.RefineSearchIdListEnabled)
                            {
                                foreach (var ss in addItem.RefineSearchIdList)
                                {
                                    if (ss.Include && System.Convert.ToBase64String(HashFunction.HashCreate(item.PublicKey)) != ss.Value)
                                        goto End;
                                    if (!ss.Include && System.Convert.ToBase64String(HashFunction.HashCreate(item.PublicKey)) == ss.Value)
                                        goto End;
                                }
                            }

                            if (addItem.RefineSearchSizeEnabled &&
                                (item.Size > addItem.RefineSearchSize.UpperLimit || item.Size < addItem.RefineSearchSize.LowerLimit))
                            {
                                goto End;
                            }

                            if (addItem.RefineSearchDownloadSuccessRateEnabled &&
                                (item.DownloadRate > addItem.RefineSearchDownloadSuccessRate.UpperLimit || item.DownloadRate < addItem.RefineSearchDownloadSuccessRate.LowerLimit))
                            {
                                goto End;
                            }

                            if (addItem.RefineSearchStatusEnabled &&
                                (item.Rate > addItem.RefineSearchStatus.UpperLimit || item.Rate < addItem.RefineSearchStatus.LowerLimit))
                            {
                                goto End;
                            }

                            if (addItem.RefineSearchReviewEnabled &&
                                (item.ReviewRate > addItem.RefineSearchReview.UpperLimit || item.ReviewRate < addItem.RefineSearchReview.LowerLimit))
                            {
                                goto End;
                            }

                            if (addItem.RefineSearchCreationTimeEnabled &&
                                (item.CreationTime > addItem.RefineSearchCreationTime.UpperLimit || item.CreationTime < addItem.RefineSearchCreationTime.LowerLimit))
                            {
                                goto End;
                            }

                            if (addItem.RefineSearchHashListEnabled)
                            {
                                foreach (var ss in addItem.RefineSearchHashList)
                                {
                                    if (ss.Include && BinaryEditor.BytesToHexString(item.Hash) != ss.Value)
                                        goto End;
                                    if (!ss.Include && BinaryEditor.BytesToHexString(item.Hash) == ss.Value)
                                        goto End;
                                }
                            }

                            if (Settings.Default._downloadList.Any(n => BinaryEditor.ArrayEquals(n.Hash, item.Hash)))
                            {
                                item.IsChecked = true;
                            }

                            searchCacheList.Add(item);

                        End: ;
                        }

                        this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                        {
                            selectItem.Hit = searchCacheList.Count;
                            queryListView.ItemsSource = searchCacheList.ToArray();
                            GridViewColumnHeaderClickedHandler(this, null);
                        }));

                        this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                        {
                            pw.Dispose();
                        }));
                    }));

                    searchThread.Priority = ThreadPriority.BelowNormal;
                    searchThread.Start();
                    searchThread.Join(5000);

                    if (searchThread.ThreadState != System.Threading.ThreadState.Stopped)
                    {
                        this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                        {
                            try
                            {
                                pw.ShowDialog();
                                pw.Button.Click += delegate(object pw_sender, RoutedEventArgs pw_e)
                                {
                                    searchThread.Abort();
                                };
                            }
                            catch { }
                        }));
                    }
                }
                catch { }
                finally
                {
                    _searchRunning = false;

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                    {
                        selectItem.IsSelected = true;
                    }));
                }
            }));

            progressThread.Priority = ThreadPriority.Highest;
            progressThread.Start();
        }

        private void クエリ追加_Q_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            RefineSearchTreeViewItemSettingWindow.Item = new RefineSearchTreeViewItem();
            using (var window = new RefineSearchTreeViewItemSettingWindow())
            {
                window.ShowDialog();
                if (window.DialogResult != true) return;
            }

            var item = queryTreeView.SelectedItem as RefineSearchTreeViewItem;

            if (item != null)
            {
                item.IsExpanded = true;
                item.Items.Add(RefineSearchTreeViewItemSettingWindow.Item);
            }

            Settings.Default.SearchTreeViewItem = _searchTreeViewItem;
        }

        private void queryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != System.Windows.Input.Key.Enter) return;

            var item = new RefineSearchTreeViewItem();
            item.RefineSearchName = queryTextBox.Text.Trim();
            item.RefineSearchFileNameListEnabled = true;
            item.RefineSearchFileNameList.Add(
                new RefineSearchString()
                {
                    Include = true,
                    Value = queryTextBox.Text.Trim()
                });

            var selectItem = queryTreeView.SelectedItem as RefineSearchTreeViewItem;
            if (selectItem != null)
            {
                selectItem.Items.Add(item);
            }

            Settings.Default.SearchTreeViewItem = _searchTreeViewItem;
        }

        private void 編集_E_Click(object sender, RoutedEventArgs e)
        {
            var selectItem = queryTreeView.SelectedItem as RefineSearchTreeViewItem;
            if (selectItem != null)
            {
                RefineSearchTreeViewItemSettingWindow.Item = selectItem;
                using (var window = new RefineSearchTreeViewItemSettingWindow())
                {
                    window.ShowDialog();
                }

                Settings.Default.SearchTreeViewItem = _searchTreeViewItem;
            }
        }

        private void 削除_D_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var item = queryTreeView.SelectedItem as RefineSearchTreeViewItem;

            if (item == null || item == (RefineSearchTreeViewItem)_searchTreeViewItem) return;

            List<RefineSearchTreeViewItem> itemList = new List<RefineSearchTreeViewItem>();
            List<RefineSearchTreeViewItem> hitList = new List<RefineSearchTreeViewItem>();

            itemList.Add((RefineSearchTreeViewItem)_searchTreeViewItem);
            hitList.Add((RefineSearchTreeViewItem)_searchTreeViewItem);

            for (int i = 0; i < itemList.Count; i++)
            {
                foreach (RefineSearchTreeViewItem tv in itemList[i].Items)
                {
                    itemList.Add(tv);

                    if (item.IsDescendantOf(tv))
                    {
                        hitList.Add(tv);
                    }
                }
            }

            hitList[hitList.Count - 2].Items.Remove(item);

            Settings.Default.SearchTreeViewItem = _searchTreeViewItem;
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

                        Sort(headerClicked.Column.Header.ToString(), direction);

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
            else if (e == null && _lastHeaderClicked != null)
            {
                Sort(_lastHeaderClicked.Column.Header.ToString(), _lastDirection);
            }
        }

        private void Sort(string sortBy, ListSortDirection direction)
        {
            if (queryListView.ItemsSource == null) return;

            var slist = queryListView.ItemsSource.OfType<CacheListViewItem>().ToList();
            var odp = ((ObjectDataProvider)System.Windows.Application.Current.Resources["ResourcesInstance"]);

            if (sortBy == odp.ObjectType.InvokeMember("FileName", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.Name.CompareTo(y.Name);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.Name.CompareTo(x.Name);
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("CategoryList", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.Category.Count(n => n == "").CompareTo(y.Category.Count(n => n == ""));
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.Category.Count(n => n == "").CompareTo(x.Category.Count(n => n == ""));
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("Sign", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
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
            }
            else if (sortBy == odp.ObjectType.InvokeMember("ID", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return BinaryEditor.ByteArraryCompare(x.ID, y.ID);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return BinaryEditor.ByteArraryCompare(y.ID, x.ID);
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("Size", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.Size.CompareTo(y.Size);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.Size.CompareTo(x.Size);
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("DownloadRate", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.DownloadRate.CompareTo(y.DownloadRate);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.DownloadRate.CompareTo(x.DownloadRate);
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("Status", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.Rate.CompareTo(y.Rate);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.Rate.CompareTo(x.Rate);
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("Review", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.ReviewRate.CompareTo(y.ReviewRate);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.ReviewRate.CompareTo(x.ReviewRate);
                    });
                }
            }

            else if (sortBy == odp.ObjectType.InvokeMember("CreationTime", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return x.CreationTime.CompareTo(y.CreationTime);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return y.CreationTime.CompareTo(x.CreationTime);
                    });
                }
            }
            else if (sortBy == odp.ObjectType.InvokeMember("Hash", BindingFlags.GetProperty, null, odp.ObjectInstance, null).ToString())
            {
                if (direction == ListSortDirection.Ascending)
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return BinaryEditor.ByteArraryCompare(x.Hash, y.Hash);
                    });
                }
                else
                {
                    slist.Sort(delegate(CacheListViewItem x, CacheListViewItem y)
                    {
                        return BinaryEditor.ByteArraryCompare(y.Hash, x.Hash);
                    });
                }
            }

            queryListView.ItemsSource = slist.ToArray();
        }
    }

    /// <summary>
    /// 絞込み検索情報を設定できるTreeViewItemを提供します
    /// </summary>
    [Serializable]
    public class RefineSearchTreeViewItem : TreeViewItem, ISerializable
    {
        public RefineSearchTreeViewItem()
            : base()
        {
            this.Header = "";
            this.Hit = 0;

            this.RefineSearchFileNameList = new List<RefineSearchString>();
            this.RefineSearchRegexFileNameList = new List<RefineSearchString>();
            this.RefineSearchCategoryList = new List<RefineSearchString>();
            this.RefineSearchIdList = new List<RefineSearchString>();
            this.RefineSearchSize = new RefineSearchRange<long>(long.MaxValue, 0);
            this.RefineSearchDownloadSuccessRate = new RefineSearchRange<double>(100, 0);
            this.RefineSearchStatus = new RefineSearchRange<double>(100, 0);
            this.RefineSearchReview = new RefineSearchRange<double>(100, 0);
            this.RefineSearchCreationTime = new RefineSearchRange<DateTime>(DateTime.MaxValue, DateTime.MinValue);
            this.RefineSearchHashList = new List<RefineSearchString>();
        }

        protected RefineSearchTreeViewItem(SerializationInfo info, StreamingContext context)
            : base()
        {
            this.RefineSearchName = info.GetString("RefineSearchName");
            this.Hit = info.GetInt32("Hit");

            base.Dispatcher.Invoke(new Action(() =>
            {
                base.IsExpanded = info.GetBoolean("IsExpanded");
            }));

            this.RefineSearchFileNameList = ((RefineSearchString[])info.GetValue("RefineSearchFileNameList", typeof(RefineSearchString[]))).ToList();
            this.RefineSearchRegexFileNameList = ((RefineSearchString[])info.GetValue("RefineSearchRegexFileNameList", typeof(RefineSearchString[]))).ToList();
            this.RefineSearchCategoryList = ((RefineSearchString[])info.GetValue("RefineSearchCategoryList", typeof(RefineSearchString[]))).ToList();
            this.RefineSearchIdList = ((RefineSearchString[])info.GetValue("RefineSearchIdList", typeof(RefineSearchString[]))).ToList();
            this.RefineSearchHashList = ((RefineSearchString[])info.GetValue("RefineSearchHashList", typeof(RefineSearchString[]))).ToList();

            this.RefineSearchSize = new RefineSearchRange<long>(long.MaxValue, 0);
            this.RefineSearchDownloadSuccessRate = new RefineSearchRange<double>(100, 0);
            this.RefineSearchStatus = new RefineSearchRange<double>(100, 0);
            this.RefineSearchReview = new RefineSearchRange<double>(100, 0);
            this.RefineSearchCreationTime = new RefineSearchRange<DateTime>(DateTime.MaxValue, DateTime.MinValue);

            this.RefineSearchSize.UpperLimit = (long)info.GetInt64("RefineSearchSize_UpperLimit");
            this.RefineSearchSize.LowerLimit = (long)info.GetInt64("RefineSearchSize_LowerLimit");
            this.RefineSearchDownloadSuccessRate.UpperLimit = (double)info.GetDouble("RefineSearchDownloadSuccessRate_UpperLimit");
            this.RefineSearchDownloadSuccessRate.LowerLimit = (double)info.GetDouble("RefineSearchDownloadSuccessRate_LowerLimit");
            this.RefineSearchStatus.UpperLimit = (double)info.GetDouble("RefineSearchStatus_UpperLimit");
            this.RefineSearchStatus.LowerLimit = (double)info.GetDouble("RefineSearchStatus_LowerLimit");
            this.RefineSearchReview.UpperLimit = (double)info.GetDouble("RefineSearchReview_UpperLimit");
            this.RefineSearchReview.LowerLimit = (double)info.GetDouble("RefineSearchReview_LowerLimit");
            this.RefineSearchCreationTime.UpperLimit = (DateTime)info.GetValue("RefineSearchCreationTime_UpperLimit", typeof(DateTime));
            this.RefineSearchCreationTime.LowerLimit = (DateTime)info.GetValue("RefineSearchCreationTime_LowerLimit", typeof(DateTime));

            this.RefineSearchFileNameListEnabled = info.GetBoolean("RefineSearchFileNameListEnabled");
            this.RefineSearchRegexFileNameListEnabled = info.GetBoolean("RefineSearchRegexFileNameListEnabled");
            this.RefineSearchCategoryListEnabled = info.GetBoolean("RefineSearchCategoryListEnabled");
            this.RefineSearchIdListEnabled = info.GetBoolean("RefineSearchIdListEnabled");
            this.RefineSearchSizeEnabled = info.GetBoolean("RefineSearchSizeEnabled");
            this.RefineSearchDownloadSuccessRateEnabled = info.GetBoolean("RefineSearchDownloadSuccessRateEnabled");
            this.RefineSearchStatusEnabled = info.GetBoolean("RefineSearchStatusEnabled");
            this.RefineSearchReviewEnabled = info.GetBoolean("RefineSearchReviewEnabled");
            this.RefineSearchCreationTimeEnabled = info.GetBoolean("RefineSearchCreationTimeEnabled");
            this.RefineSearchHashListEnabled = info.GetBoolean("RefineSearchHashListEnabled");

            if (info.GetBoolean("ItemsSource_Binary_whether") == true)
            {
                using (MemoryStream stream = new MemoryStream((byte[])info.GetValue("ItemsSource_Binary", typeof(byte[]))))
                {
                    BinaryFormatter formatter = new BinaryFormatter();

                    foreach (var v in (RefineSearchTreeViewItem[])formatter.Deserialize(stream))
                    {
                        base.Items.Add(v);
                    }
                }
            }
        }

        new public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("RefineSearchName", this.RefineSearchName);
            info.AddValue("Hit", this.Hit);

            base.Dispatcher.Invoke(new Action(() =>
            {
                info.AddValue("IsExpanded", base.IsExpanded);
            }));

            info.AddValue("RefineSearchFileNameList", this.RefineSearchFileNameList.ToArray(), typeof(RefineSearchString[]));
            info.AddValue("RefineSearchRegexFileNameList", this.RefineSearchRegexFileNameList.ToArray(), typeof(RefineSearchString[]));
            info.AddValue("RefineSearchCategoryList", this.RefineSearchCategoryList.ToArray(), typeof(RefineSearchString[]));
            info.AddValue("RefineSearchIdList", this.RefineSearchIdList.ToArray(), typeof(RefineSearchString[]));
            info.AddValue("RefineSearchHashList", this.RefineSearchHashList.ToArray(), typeof(RefineSearchString[]));

            info.AddValue("RefineSearchSize_UpperLimit", this.RefineSearchSize.UpperLimit);
            info.AddValue("RefineSearchSize_LowerLimit", this.RefineSearchSize.LowerLimit);
            info.AddValue("RefineSearchDownloadSuccessRate_UpperLimit", this.RefineSearchDownloadSuccessRate.UpperLimit);
            info.AddValue("RefineSearchDownloadSuccessRate_LowerLimit", this.RefineSearchDownloadSuccessRate.LowerLimit);
            info.AddValue("RefineSearchStatus_UpperLimit", this.RefineSearchStatus.UpperLimit);
            info.AddValue("RefineSearchStatus_LowerLimit", this.RefineSearchStatus.LowerLimit);
            info.AddValue("RefineSearchReview_UpperLimit", this.RefineSearchReview.UpperLimit);
            info.AddValue("RefineSearchReview_LowerLimit", this.RefineSearchReview.LowerLimit);
            info.AddValue("RefineSearchCreationTime_UpperLimit", this.RefineSearchCreationTime.UpperLimit, typeof(DateTime));
            info.AddValue("RefineSearchCreationTime_LowerLimit", this.RefineSearchCreationTime.LowerLimit, typeof(DateTime));

            info.AddValue("RefineSearchFileNameListEnabled", this.RefineSearchFileNameListEnabled);
            info.AddValue("RefineSearchRegexFileNameListEnabled", this.RefineSearchRegexFileNameListEnabled);
            info.AddValue("RefineSearchCategoryListEnabled", this.RefineSearchCategoryListEnabled);
            info.AddValue("RefineSearchIdListEnabled", this.RefineSearchIdListEnabled);
            info.AddValue("RefineSearchSizeEnabled", this.RefineSearchSizeEnabled);
            info.AddValue("RefineSearchDownloadSuccessRateEnabled", this.RefineSearchDownloadSuccessRateEnabled);
            info.AddValue("RefineSearchStatusEnabled", this.RefineSearchStatusEnabled);
            info.AddValue("RefineSearchReviewEnabled", this.RefineSearchReviewEnabled);
            info.AddValue("RefineSearchCreationTimeEnabled", this.RefineSearchCreationTimeEnabled);
            info.AddValue("RefineSearchHashListEnabled", this.RefineSearchHashListEnabled);

            if (base.Items.Count != 0)
            {
                info.AddValue("ItemsSource_Binary_whether", true);

                using (MemoryStream stream = new MemoryStream())
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, base.Items.OfType<RefineSearchTreeViewItem>().ToArray());
                    stream.Position = 0;

                    info.AddValue("ItemsSource_Binary", stream.ToArray(), typeof(byte[]));
                }
            }
            else
            {
                info.AddValue("ItemsSource_Binary_whether", false);
            }
        }

        private string _refineSearchName;
        public string RefineSearchName 
        {
            get { return _refineSearchName; }
            set
            {
                _refineSearchName = value;
                base.Header = string.Format("{0} ({1})", this._refineSearchName, this._hit);
            }
        }

        private int _hit;
        public int Hit
        {
            get { return _hit; }
            set
            {
                _hit = value;
                base.Header = string.Format("{0} ({1})", this._refineSearchName, this._hit);
            }
        }

        public IList<RefineSearchString> RefineSearchFileNameList { get; private set; }
        public bool RefineSearchFileNameListEnabled { get; set; }

        public IList<RefineSearchString> RefineSearchRegexFileNameList { get; private set; }
        public bool RefineSearchRegexFileNameListEnabled { get; set; }

        public IList<RefineSearchString> RefineSearchCategoryList { get; private set; }
        public bool RefineSearchCategoryListEnabled { get; set; }

        public IList<RefineSearchString> RefineSearchIdList { get; private set; }
        public bool RefineSearchIdListEnabled { get; set; }

        public RefineSearchRange<long> RefineSearchSize { get; private set; }
        public bool RefineSearchSizeEnabled { get; set; }

        public RefineSearchRange<double> RefineSearchDownloadSuccessRate { get; private set; }
        public bool RefineSearchDownloadSuccessRateEnabled { get; set; }

        public RefineSearchRange<double> RefineSearchStatus { get; private set; }
        public bool RefineSearchStatusEnabled { get; set; }

        public RefineSearchRange<double> RefineSearchReview { get; private set; }
        public bool RefineSearchReviewEnabled { get; set; }

        public RefineSearchRange<DateTime> RefineSearchCreationTime { get; private set; }
        public bool RefineSearchCreationTimeEnabled { get; set; }

        public IList<RefineSearchString> RefineSearchHashList { get; private set; }
        public bool RefineSearchHashListEnabled { get; set; }
    }

    [Serializable]
    public class RefineSearchString
    {
        /// <summary>
        /// 文字列を取得または設定します
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// この文字列を含むかを取得または設定します。
        /// </summary>
        public bool Include { get; set; }
    }

    /// <summary>
    /// 絞込み検索のための範囲を取得するメソッドを提供します。
    /// </summary>
    //[Serializable]
    public class RefineSearchRange<T> where T : IComparable
    {
        T _max;
        T _min;

        /// <summary>
        /// RefineSearchRangeクラスの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="max">UpperLimitの上限</param>
        /// <param name="min">LowerLimitの下限</param>
        public RefineSearchRange(T max, T min)
        {
            this._max = max;
            this._min = min;
        }

        //protected RefineSearchRange(SerializationInfo info, StreamingContext context)
        //{
        //    _max = (T)info.GetValue("_max", _max.GetType());
        //    _min = (T)info.GetValue("_min", _min.GetType());
        //    this.UpperLimit = (T)info.GetValue("UpperLimit", this.UpperLimit.GetType());
        //    this.LowerLimit = (T)info.GetValue("LowerLimit", this.LowerLimit.GetType());
        //}

        //public void GetObjectData(SerializationInfo info, StreamingContext context)
        //{
        //    info.AddValue("_max", _max, _max.GetType());
        //    info.AddValue("_min", _min, _min.GetType());
        //    info.AddValue("UpperLimit", this.UpperLimit, this.UpperLimit.GetType());
        //    info.AddValue("LowerLimit", this.LowerLimit, this.LowerLimit.GetType());
        //}

        T _upperLimit;
        /// <summary>
        /// 上限を取得または設定します
        /// </summary>
        public T UpperLimit
        {
            get { return _upperLimit; }
            set
            {
                _upperLimit = value;
                _upperLimit = (_max.CompareTo(_upperLimit) < 0) ? _max : _upperLimit;
                _upperLimit = (this.LowerLimit.CompareTo(_upperLimit) > 0) ? this.LowerLimit : _upperLimit;
            }
        }

        T _lowerLimit;
        /// <summary>
        /// 下限を取得または設定します
        /// </summary>
        public T LowerLimit
        {
            get { return _lowerLimit; }
            set
            {
                _lowerLimit = value;
                _lowerLimit = (_min.CompareTo(_lowerLimit) > 0) ? _min : _lowerLimit;
                _lowerLimit = (this.UpperLimit.CompareTo(_lowerLimit) < 0) ? this.UpperLimit : _lowerLimit;
            }
        }

        /// <summary>
        /// valueが範囲内かどうかを検証します
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool Verify(T value)
        {
            if (value.CompareTo(this.LowerLimit) < 0 || value.CompareTo(this.UpperLimit) > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}