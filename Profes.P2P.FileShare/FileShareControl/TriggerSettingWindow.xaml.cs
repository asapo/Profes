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
    /// TriggerSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TriggerSettingWindow : Window, IDisposable
    {
        public TriggerSettingWindow()
        {
            InitializeComponent();
        }

        public void SetTrigger(Trigger trigger)
        {
            FileNameTextBox.Text = trigger.Name;
            IDTextBox.Text = trigger.ID;

            if (trigger.Hash == null) HashTextBox.Text = "";
            else HashTextBox.Text = BinaryEditor.BytesToHexString(trigger.Hash);

            SizeLimitTextBox.Text = trigger.LimitSize.ToString();
            SizeLowerTextBox.Text = trigger.LowerSize.ToString();

            EffectCheckBox.IsChecked = trigger.Effect;
            TriggerDeleteCheckBox.IsChecked = trigger.Remove;

            foreach (string ss in trigger.Category)
            {
                QueryTextBox.Text += "\"" + ss + "\",";
            }
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            Trigger t = new Trigger();
            t.Name = FileNameTextBox.Text;
            t.ID = IDTextBox.Text;
            t.Hash = BinaryEditor.HexStringToBytes(HashTextBox.Text);

            if (SizeLimitTextBox.Text.Trim() == "" || !Regex.IsMatch(SizeLimitTextBox.Text, "^[0-9]*$")) t.LimitSize = 0;
            else t.LimitSize = long.Parse(SizeLimitTextBox.Text);

            if (SizeLowerTextBox.Text.Trim() == "" || !Regex.IsMatch(SizeLowerTextBox.Text, "^[0-9]*$")) t.LowerSize = 0;
            else t.LowerSize = long.Parse(SizeLowerTextBox.Text);

            t.Category = new string[3] { "", "", "" };

            int i = 0;
            foreach (Match m in Regex.Matches(QueryTextBox.Text, "\"(.*?)\""))
            {
                if (i >= 3) break;
                t.Category[i] = m.Value.Trim('\"');
                i++;
            }

            t.Effect = EffectCheckBox.IsChecked == true ? true : false;
            t.Remove = TriggerDeleteCheckBox.IsChecked == true ? true : false;

            Settings.Default._triggerList.Add(t);

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