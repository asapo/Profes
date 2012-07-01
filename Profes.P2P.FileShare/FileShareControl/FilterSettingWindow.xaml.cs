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
using Profes.BinaryEditorBase;
using System.Text.RegularExpressions;
using Profes.P2P.FileShare.Properties;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FilterSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class FilterSettingWindow : Window, IDisposable
    {
        public FilterSettingWindow()
        {
            InitializeComponent();
        }

        public void SetFilter(Filter filter)
        {
            FileNameTextBox.Text = filter.Name;
            IDTextBox.Text = filter.ID;

            if (filter.Hash == null) HashTextBox.Text = "";
            else HashTextBox.Text = BinaryEditor.BytesToHexString(filter.Hash);

            SizeLimitTextBox.Text = filter.LimitSize.ToString();
            SizeLowerTextBox.Text = filter.LowerSize.ToString();

            EffectCheckBox.IsChecked = filter.Effect;

            foreach (string ss in filter.Category)
            {
                QueryTextBox.Text += "\"" + ss + "\",";
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Filter f = new Filter();
            f.Name = FileNameTextBox.Text;
            f.ID = IDTextBox.Text;
            f.Hash = BinaryEditor.HexStringToBytes(HashTextBox.Text);

            if (SizeLimitTextBox.Text.Trim() == "" || !Regex.IsMatch(SizeLimitTextBox.Text, "^[0-9]*$")) f.LimitSize = 0;
            else f.LimitSize = long.Parse(SizeLimitTextBox.Text);

            if (SizeLowerTextBox.Text.Trim() == "" || !Regex.IsMatch(SizeLowerTextBox.Text, "^[0-9]*$")) f.LowerSize = 0;
            else f.LowerSize = long.Parse(SizeLowerTextBox.Text);

            f.Category = new string[3] { "", "", "" };

            int i = 0;
            foreach (Match m in Regex.Matches(QueryTextBox.Text, "\"(.*?)\""))
            {
                if (i >= 3) break;
                f.Category[i] = m.Value.Trim('\"');
                i++;
            }

            f.Effect = EffectCheckBox.IsChecked == true ? true : false;

            Settings.Default._filterList.Add(f);

            this.DialogResult = true;
            this.Close();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ClearButton1_Click(object sender, RoutedEventArgs e)
        {
            FileNameTextBox.Text = "";
        }

        private void ClearButton2_Click(object sender, RoutedEventArgs e)
        {
            QueryTextBox.Text = "";
        }

        private void ClearButton3_Click(object sender, RoutedEventArgs e)
        {
            IDTextBox.Text = "";
        }

        private void ClearButton4_Click(object sender, RoutedEventArgs e)
        {
            HashTextBox.Text = "";
        }

        private void ClearButton5_Click(object sender, RoutedEventArgs e)
        {
            SizeLimitTextBox.Text = "";
        }

        private void ClearButton6_Click(object sender, RoutedEventArgs e)
        {
            SizeLowerTextBox.Text = "";
        }

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。NodeSettingWindow によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        #endregion
    }
}