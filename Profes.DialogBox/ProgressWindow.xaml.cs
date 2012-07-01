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

namespace Profes.DialogBox
{
    /// <summary>
    /// ProgressWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgressWindow : Window, IDisposable
    {
        public ProgressWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。ProgressWindow によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        #endregion
    }
}