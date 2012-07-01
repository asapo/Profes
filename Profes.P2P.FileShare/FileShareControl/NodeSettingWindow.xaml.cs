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
using Profes.P2P.FileShare.ServiceModel;
using System.ServiceModel;
using System.Diagnostics;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// NodeSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NodeSettingWindow : Window, IDisposable
    {
        public NodeSettingWindow()
        {
            InitializeComponent();
        }

        private void button4_Click(object sender, RoutedEventArgs e)
        {
            Uri address = null;

            try
            {
                if (IpRegister.Text.Contains(":"))
                {
                    address = new Uri(string.Format("net.tcp://[{0}]:{1}/FileShareService", IpRegister.Text.Trim(), PortRegister.Text.Trim()));
                }
                else
                {
                    address = new Uri(string.Format("net.tcp://{0}:{1}/FileShareService", IpRegister.Text.Trim(), PortRegister.Text.Trim()));
                }
            }
            catch
            {
                return;
            }

            Settings.Default._routeTable.Add(new Node() { Endpoint = new EndpointAddress(address) });

            IpRegister.Text = "";
            PortRegister.Text = "";
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            foreach (string ss in EncryptedEndpointAddressRegister.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    Uri address = new Uri(string.Format("net.tcp://{0}/FileShareService", EndpointAddressEncryption.Decrypt(ss)));
                    Settings.Default._routeTable.Add(new Node() { Endpoint = new EndpointAddress(address) });
                }
                catch { }
            }

            EncryptedEndpointAddressRegister.Text = "";
        }

        private void button7_Click(object sender, RoutedEventArgs e)
        {
            string address = null;

            if (IpEncryption.Text.Contains(":"))
            {
                address = string.Format("[{0}]:{1}", IpEncryption.Text.Trim(), PortEncryption.Text.Trim());
            }
            else
            {
                address = string.Format("{0}:{1}", IpEncryption.Text.Trim(), PortEncryption.Text.Trim());
            }

            EncryptedEndpointAddress.Text = EndpointAddressEncryption.Encrypt(address);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            this.Dispose();
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