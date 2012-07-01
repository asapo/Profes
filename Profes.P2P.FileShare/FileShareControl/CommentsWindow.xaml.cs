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
using Profes.P2P.FileShare.ServiceModel;
using Profes.P2P.FileShare.Properties;
using Profes.BinaryEditorBase;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// CommentsWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class CommentsWindow : Window
    {
        bool _closeFlag = true;

        public CommentsWindow()
        {
            InitializeComponent();
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            if (_closeFlag == true)
                this.Close();
        }

        private byte[] _signatureHash;

        /// <summary>
        /// 評価を表示するキャッシュの電子署名のハッシュ
        /// </summary>
        public byte[] SignatureHash
        {
            get { return _signatureHash; }
            set
            {
                _signatureHash = value;

                var list = new List<CacheReview>(Settings.Default._keyController.SearchReview(_signatureHash));

                list.Sort(delegate(CacheReview x, CacheReview y)
                {
                    return x.CreateTime.CompareTo(y.CreateTime);
                });

                commentsListBox.ItemsSource = list.ToArray();
            }
        }

        /// <summary>
        /// 悪い
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, RoutedEventArgs e)
        {
            if (commentsTextBox.Text.Trim().Length == 0)
            {
                _closeFlag = false;
                MessageBox.Show("コメントが入力されていません");
                _closeFlag = true;
                return;
            }
            else if (commentsTextBox.Text.Split('\n').Length > 10)
            {
                _closeFlag = false;
                MessageBox.Show("10行以内にコメントを収めてください");
                _closeFlag = true;
                return;
            }

            CacheReview cr = new CacheReview();
            cr.Review = Review.悪い;
            cr.ReviewComments = commentsTextBox.Text.Trim();
            cr.Sign = Settings.Default.Sign;

            cr.PublicKey = Settings.Default.PublicKey;
            cr.CreateDigitalSignature(Settings.Default.PrivateKey);

            Settings.Default._keyController.AddReview(this.SignatureHash, cr);

            _closeFlag = false;
            this.Close();
        }

        /// <summary>
        /// 良い
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (commentsTextBox.Text.Trim().Length == 0)
            {
                _closeFlag = false;
                MessageBox.Show("コメントが入力されていません");
                _closeFlag = true;
                return;
            }
            else if (commentsTextBox.Text.Split('\n').Length > 10)
            {
                _closeFlag = false;
                MessageBox.Show("10行以内にコメントを収めてください");
                _closeFlag = true;
                return;
            }

            CacheReview cr = new CacheReview();
            cr.Review = Review.良い;
            cr.ReviewComments = commentsTextBox.Text.Trim();
            cr.Sign = Settings.Default.Sign;

            cr.PublicKey = Settings.Default.PublicKey;
            cr.CreateDigitalSignature(Settings.Default.PrivateKey);

            Settings.Default._keyController.AddReview(this.SignatureHash, cr);

            _closeFlag = false;
            this.Close();
        }

        /// <summary>
        /// なし
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, RoutedEventArgs e)
        {
            _closeFlag = false;
            this.Close();
        }
    }
}