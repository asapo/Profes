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
using Profes.P2P.FileShare.Properties;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// CategorySettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CategorySettingWindow : Window, IDisposable
    {
        public CategorySettingWindow()
        {
            InitializeComponent();

            comboBox1.SelectedIndex = Settings.Default.SelectedIndexCategory;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.SelectedIndexCategory = comboBox1.SelectedIndex;
            this.DialogResult = true;

            this.Close();
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public string[] Category
        {
            get
            {
                return new string[]{
                    CategoryTextBox1.Text.Trim(),
                    CategoryTextBox2.Text.Trim(),
                    CategoryTextBox3.Text.Trim()
                };
            }
            set
            {
                CategoryTextBox1.Text = value[0];
                CategoryTextBox2.Text = value[1];
                CategoryTextBox3.Text = value[2];
            }
        }

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。SettingWindow によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        #endregion

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            var ss = comboBox1.Text as string;

            if (ss != null && !Settings.Default._categoryList.Contains(ss))
            {
                Settings.Default._categoryList.Add(ss);
                Settings.Default._categoryDir.Add(ss, this.Category);
            }
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            var ss = comboBox1.Text as string;

            if (ss != null && Settings.Default._categoryList.Contains(ss))
            {
                Settings.Default._categoryList.Remove(ss);
                Settings.Default._categoryDir.Remove(ss);
            }
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            var ss = comboBox1.Text as string;

            try
            {
                if (ss != null && Settings.Default._categoryList.Contains(ss))
                {
                    Settings.Default._categoryDir[ss] = this.Category;
                }
            }
            catch { }
        }

        private void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var ss = comboBox1.SelectedItem as string;

            if (ss != null)
            {
                this.Category = Settings.Default._categoryDir[ss];
            }
        }
    }
}