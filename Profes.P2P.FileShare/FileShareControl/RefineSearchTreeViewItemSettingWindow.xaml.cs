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
using System.Windows.Shapes;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// RefineSearchTreeViewItemSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class RefineSearchTreeViewItemSettingWindow : Window, IDisposable
    {
        private RefineSearchTreeViewItem _oldItem;

        public RefineSearchTreeViewItemSettingWindow()
        {
            try
            {
                if (Item == null) Item = new RefineSearchTreeViewItem();
                _oldItem = Clone.DeepCopyClone<RefineSearchTreeViewItem>(Item);
            }
            catch { }

            InitializeComponent();

            FileNameListView.SelectionChanged+=new SelectionChangedEventHandler(FileNameListView_SelectionChanged);
            RegexFileNameListView.SelectionChanged+=new SelectionChangedEventHandler(RegexFileNameListView_SelectionChanged);
            CategoryListView.SelectionChanged+=new SelectionChangedEventHandler(CategoryListView_SelectionChanged);
            IdListView.SelectionChanged+=new SelectionChangedEventHandler(IdListView_SelectionChanged);
            HashListView.SelectionChanged+=new SelectionChangedEventHandler(HashListView_SelectionChanged);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.DialogResult != true)
            {
                Item = _oldItem;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SizeUpperLimitTextBox.Text = Item.RefineSearchSize.UpperLimit.ToString();
            SizeLowerLimitTextBox.Text = Item.RefineSearchSize.LowerLimit.ToString();

            DownloadSuccessRateUpperLimitTextBox.Text = Item.RefineSearchDownloadSuccessRate.UpperLimit.ToString();
            DownloadSuccessRateLowerLimitTextBox.Text = Item.RefineSearchDownloadSuccessRate.LowerLimit.ToString();

            StatusUpperLimitTextBox.Text = Item.RefineSearchStatus.UpperLimit.ToString();
            StatusLowerLimitTextBox.Text = Item.RefineSearchStatus.LowerLimit.ToString();

            CreationTimeUpperLimitTextBox.Text = Item.RefineSearchCreationTime.UpperLimit.ToString("yyyy/MM/dd HH:mm:ss");
            CreationTimeLowerLimitTextBox.Text = Item.RefineSearchCreationTime.LowerLimit.ToString("yyyy/MM/dd HH:mm:ss");

            ReviewGoodCheckBox.IsChecked = (Item.RefineSearchReview.Verify(100)) ? true : false;
            ReviewMiddleCheckBox.IsChecked = (Item.RefineSearchReview.Verify(50)) ? true : false;
            ReviewBadCheckBox.IsChecked = (Item.RefineSearchReview.Verify(0)) ? true : false;
        }

        private void Ok_Button_Click(object sender, RoutedEventArgs e)
        {
            long sizeUpperLimit;
            long sizeLowerLimit;
            double downloadSuccessRateUpperLimit;
            double downloadSuccessRateLowerLimit;
            double statusUpperLimit;
            double statusLowerLimit;
            DateTime creationTimeUpperLimit;
            DateTime creationTimeLowerLimit;
            double reviewUpperLimit = 0;
            double reviewLowerLimit = 100;

            if (!long.TryParse(SizeUpperLimitTextBox.Text.Trim(), out sizeUpperLimit))
            {
                MessageBox.Show("サイズ上限が不正です");
                return;
            }
            if (!long.TryParse(SizeLowerLimitTextBox.Text.Trim(), out sizeLowerLimit))
            {
                MessageBox.Show("サイズ下限が不正です");
                return;
            }

            if (!double.TryParse(DownloadSuccessRateUpperLimitTextBox.Text.Trim(), out downloadSuccessRateUpperLimit))
            {
                MessageBox.Show("ダウンロード成功率上限が不正です");
                return;
            }
            if (!double.TryParse(DownloadSuccessRateLowerLimitTextBox.Text.Trim(), out downloadSuccessRateLowerLimit))
            {
                MessageBox.Show("ダウンロード成功率下限が不正です");
                return;
            }

            if (!double.TryParse(StatusUpperLimitTextBox.Text.Trim(), out statusUpperLimit))
            {
                MessageBox.Show("キャッシュ率上限が不正です");
                return;
            }
            if (!double.TryParse(StatusLowerLimitTextBox.Text.Trim(), out statusLowerLimit))
            {
                MessageBox.Show("キャッシュ率下限が不正です");
                return;
            }

            if (!DateTime.TryParseExact(CreationTimeUpperLimitTextBox.Text.Trim(), "yyyy/MM/dd HH:mm:ss", null,
                System.Globalization.DateTimeStyles.None, out creationTimeUpperLimit))
            {
                MessageBox.Show("作成日時上限が不正です");
                return;
            }
            if (!DateTime.TryParseExact(CreationTimeLowerLimitTextBox.Text.Trim(), "yyyy/MM/dd HH:mm:ss", null,
                System.Globalization.DateTimeStyles.None, out creationTimeLowerLimit))
            {
                MessageBox.Show("作成日時下限が不正です");
                return;
            }

            if (ReviewBadCheckBox.IsChecked == true)
            {
                reviewLowerLimit = Math.Min(reviewLowerLimit, 0);
                reviewUpperLimit = Math.Max(reviewUpperLimit, 33);
            }
            if (ReviewMiddleCheckBox.IsChecked == true)
            {
                reviewLowerLimit = Math.Min(reviewLowerLimit, 33);
                reviewUpperLimit = Math.Max(reviewUpperLimit, 66);
            }
            if (ReviewGoodCheckBox.IsChecked == true)
            {
                reviewLowerLimit = Math.Min(reviewLowerLimit, 66);
                reviewUpperLimit = Math.Max(reviewUpperLimit, 100);
            }

            Item.RefineSearchSize.UpperLimit = sizeUpperLimit;
            Item.RefineSearchSize.LowerLimit = sizeLowerLimit;
            Item.RefineSearchDownloadSuccessRate.UpperLimit = downloadSuccessRateUpperLimit;
            Item.RefineSearchDownloadSuccessRate.LowerLimit = downloadSuccessRateLowerLimit;
            Item.RefineSearchStatus.UpperLimit = statusUpperLimit;
            Item.RefineSearchStatus.LowerLimit = statusLowerLimit;
            Item.RefineSearchCreationTime.UpperLimit = creationTimeUpperLimit;
            Item.RefineSearchCreationTime.LowerLimit = creationTimeLowerLimit;
            Item.RefineSearchReview.UpperLimit = reviewUpperLimit;
            Item.RefineSearchReview.LowerLimit = reviewLowerLimit;

            this.DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public static RefineSearchTreeViewItem Item { get; set; }

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。SettingWindow によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        #endregion

        #region FileName イベント

        private void FileNameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = FileNameListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            FileNameSearchRadioButton.IsChecked = item.Include;
            FileNameFilterRadioButton.IsChecked = !item.Include;
            FileNameTextBox.Text = item.Value;
        }

        private void FileName_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            Item.RefineSearchFileNameList.Add(new RefineSearchString()
            {
                Include = (bool)FileNameSearchRadioButton.IsChecked,
                Value = FileNameTextBox.Text,
            });

            FileNameListView.Items.Refresh();
        }

        private void FileName_Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = FileNameListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            item.Include = (bool)FileNameSearchRadioButton.IsChecked;
            item.Value = FileNameTextBox.Text;

            FileNameListView.Items.Refresh();
        }

        private void FileName_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = FileNameListView.SelectedItem as RefineSearchString;
            Item.RefineSearchFileNameList.Remove(item);

            FileNameListView.Items.Refresh();
        }

        #endregion

        #region RegexFileName イベント

        private void RegexFileNameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = RegexFileNameListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            RegexFileNameMatchRadioButton.IsChecked = item.Include;
            RegexFileNameMismatchRadioButton.IsChecked = !item.Include;
            RegexFileNameTextBox.Text = item.Value;
        }

        private void RegexFileName_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            Item.RefineSearchRegexFileNameList.Add(new RefineSearchString()
            {
                Include = (bool)RegexFileNameMatchRadioButton.IsChecked,
                Value = RegexFileNameTextBox.Text,
            });

            RegexFileNameListView.Items.Refresh();
        }

        private void RegexFileName_Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = RegexFileNameListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            item.Include = (bool)RegexFileNameMatchRadioButton.IsChecked;
            item.Value = RegexFileNameTextBox.Text;

            RegexFileNameListView.Items.Refresh();
        }

        private void RegexFileName_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = RegexFileNameListView.SelectedItem as RefineSearchString;
            Item.RefineSearchRegexFileNameList.Remove(item);

            RegexFileNameListView.Items.Refresh();
        }

        #endregion

        #region Category イベント

        private void CategoryListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = CategoryListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            CategorySearchRadioButton.IsChecked = item.Include;
            CategoryFilterRadioButton.IsChecked = !item.Include;
            CategoryTextBox.Text = item.Value;
        }

        private void Category_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            Item.RefineSearchCategoryList.Add(new RefineSearchString()
            {
                Include = (bool)CategorySearchRadioButton.IsChecked,
                Value = CategoryTextBox.Text,
            });

            CategoryListView.Items.Refresh();
        }

        private void Category_Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = CategoryListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            item.Include = (bool)CategorySearchRadioButton.IsChecked;
            item.Value = CategoryTextBox.Text;

            CategoryListView.Items.Refresh();
        }

        private void Category_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = CategoryListView.SelectedItem as RefineSearchString;
            Item.RefineSearchCategoryList.Remove(item);

            CategoryListView.Items.Refresh();
        }

        #endregion

        #region Id イベント

        private void IdListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = IdListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            IdSearchRadioButton.IsChecked = item.Include;
            IdFilterRadioButton.IsChecked = !item.Include;
            IdTextBox.Text = item.Value;
        }

        private void Id_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            Item.RefineSearchIdList.Add(new RefineSearchString()
            {
                Include = (bool)IdSearchRadioButton.IsChecked,
                Value = IdTextBox.Text,
            });

            IdListView.Items.Refresh();
        }

        private void Id_Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = IdListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            item.Include = (bool)IdSearchRadioButton.IsChecked;
            item.Value = IdTextBox.Text;

            IdListView.Items.Refresh();
        }

        private void Id_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = IdListView.SelectedItem as RefineSearchString;
            Item.RefineSearchIdList.Remove(item);

            IdListView.Items.Refresh();
        }

        #endregion

        #region Hash イベント

        private void HashListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = HashListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            HashSearchRadioButton.IsChecked = item.Include;
            HashFilterRadioButton.IsChecked = !item.Include;
            HashTextBox.Text = item.Value;
        }

        private void Hash_Add_Button_Click(object sender, RoutedEventArgs e)
        {
            Item.RefineSearchHashList.Add(new RefineSearchString()
            {
                Include = (bool)HashSearchRadioButton.IsChecked,
                Value = HashTextBox.Text,
            });

            HashListView.Items.Refresh();
        }

        private void Hash_Edit_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = HashListView.SelectedItem as RefineSearchString;

            if (item == null) return;

            item.Include = (bool)HashSearchRadioButton.IsChecked;
            item.Value = HashTextBox.Text;

            HashListView.Items.Refresh();
        }

        private void Hash_Delete_Button_Click(object sender, RoutedEventArgs e)
        {
            var item = HashListView.SelectedItem as RefineSearchString;
            Item.RefineSearchHashList.Remove(item);

            HashListView.Items.Refresh();
        }

        #endregion
    }
}