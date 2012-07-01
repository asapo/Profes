using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.Text.RegularExpressions;
using System.Linq;
using System.Windows;
using Profes.BinaryEditorBase;
using Profes.P2P.FileShare.Properties;
using Profes.P2P.FileShare.ServiceModel;
using Profes.Security.Cryptography;
using System.Management;
using Profes.UPnP;
using System.Threading;
using Profes.DialogBox;
using System.Windows.Threading;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// SettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SettingWindow : Window, IDisposable
    {
        /// <summary>
        /// SettingWindowクラスの新しいインスタンスを初期化します
        /// </summary>
        public SettingWindow()
        {
            InitializeComponent();

            // QueryListTextBox
            foreach (string ss in Settings.Default.QueryList)
            {
                QueryListTextBox.Text += Cache.CategoryRegularization(ss) + "\r\n";
            }

            // IdLabel
            IdLabel.Content = Convert.ToBase64String(HashFunction.HashCreate(this.PublicKeyTextBox.Text));

            // ConnectionType
            switch (Settings.Default.ConnectionType)
            {
                case ConnectionType.Direct:
                    ConnectionType_DirectRadioButton.IsChecked = true;
                    break;

                case ConnectionType.UPnP:
                    ConnectionType_UPnPRadioButton.IsChecked = true;
                    break;

                case ConnectionType.Other:
                    ConnectionType_OtherRadioButton.IsChecked = true;
                    break;
            }

            try
            {
                IpAddressComboBox.Text = Settings.Default.DirectConnectionInformation.IPAddress;
                PortNamberTextBox.Text = Settings.Default.DirectConnectionInformation.Port.ToString();
                GlobalIPAddressTextBox.Text = Settings.Default.UpnpConnectionInformation.GlobalIPAddress;
                MachineIPAddressTextBox.Text = Settings.Default.UpnpConnectionInformation.MachineIPAddress;
                GatewayIPAddressTextBox.Text = Settings.Default.UpnpConnectionInformation.GatewayIPAddress;
                ExternalPortTextBox.Text = Settings.Default.UpnpConnectionInformation.ExternalPort.ToString();
                InternalPortTextBox.Text = Settings.Default.UpnpConnectionInformation.InternalPort.ToString();
                OtherIpAddressTextBox.Text = Settings.Default.OtherConnectionInformation.IPAddress.ToString();
                OtherPortNamberTextBox.Text = Settings.Default.OtherConnectionInformation.Port.ToString();
            }
            catch { }

            // PublicKeyTextBox PrivateKeyTextBox
            if (PublicKeyTextBox.Text.Trim() == "" && PrivateKeyTextBox.Text.Trim() == "")
            {
                button3_Click(this, null);
            }

            // DownloadDirectoryTextBox
            if (this.DownloadDirectoryTextBox.Text.Trim() == "")
            {
                string path = Directory.GetCurrentDirectory() + @"\Download";
                Directory.CreateDirectory(path);
                this.DownloadDirectoryTextBox.Text = path;
            }

            // CacheDirectoryTextBox
            if (this.CacheDirectoryTextBox.Text.Trim() == "")
            {
                string path = Directory.GetCurrentDirectory() + @"\Cache";
                Directory.CreateDirectory(path);
                this.CacheDirectoryTextBox.Text = path;
            }
        }

        void SettingWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.ConnectionType == ConnectionType.Direct)
                ConnectionType_DirectRadioButton_Click(null, null);

            if (Settings.Default.ConnectionType == ConnectionType.UPnP)
                ConnectionType_UPnPRadioButton_Click(null, null);
        }

        /// <summary>
        /// 直接接続情報を取得します
        /// </summary>
        /// <param name="Cancel"></param>
        private void GetDirect(bool Cancel)
        {
            using (var pw = new ProgressWindow())
            {
                pw.Topmost = true;
                pw.MessageLabel.Content = "IPアドレス情報を読み込んでいます...";
                pw.ProgressBar.IsIndeterminate = true;
                pw.Button.IsEnabled = Cancel;

                IpAddressComboBox.Items.Clear();

                Thread startThread = new Thread(new ThreadStart(delegate()
                {
                    string query = "SELECT * FROM Win32_NetworkAdapterConfiguration";
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                    ManagementObjectCollection queryCollection = searcher.Get();

                    foreach (ManagementObject mo in queryCollection)
                    {
                        if ((bool)mo["IPEnabled"])
                        {
                            foreach (string ip in (string[])mo["IPAddress"])
                            {
                                if (Verification.VerificationIPAddress(ip))
                                {
                                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                                    {
                                        IpAddressComboBox.Items.Add(ip);
                                    }));
                                }
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                    {
                        pw.Close();
                    }));
                }));
                startThread.Start();

                pw.ShowDialog();
                pw.Button.Click += delegate(object pw_sender, RoutedEventArgs pw_e)
                {
                    startThread.Abort();
                };
            }
        }

        /// <summary>
        /// UPnP情報を取得します
        /// </summary>
        /// <param name="Cancel"></param>
        private void GetUPnP(bool Cancel)
        {
            using (var pw = new ProgressWindow())
            {
                pw.Topmost = true;
                pw.MessageLabel.Content = "UPnP情報を読み込んでいます...";
                pw.ProgressBar.IsIndeterminate = true;
                pw.Button.IsEnabled = Cancel;

                Thread startThread = new Thread(new ThreadStart(delegate()
                {
                    foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
                    {
                        foreach (var machineIP in nic.GetIPProperties().UnicastAddresses
                            .Select(n => n.Address)
                            .Where(n => n.AddressFamily == AddressFamily.InterNetwork))
                        {
                            foreach (var firewallIP in nic.GetIPProperties().GatewayAddresses
                                .Select(n => n.Address)
                                .Where(n => n.AddressFamily == AddressFamily.InterNetwork))
                            {
                                string value = null;

                                if (null != (value = UPnPClient.GetExternalIPAddress(firewallIP.ToString())) &&
                                    Verification.VerificationIPAddress(value))
                                {
                                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                                    {
                                        GlobalIPAddressTextBox.Text = value;
                                        MachineIPAddressTextBox.Text = machineIP.ToString();
                                        GatewayIPAddressTextBox.Text = firewallIP.ToString();

                                        pw.Close();
                                    }));

                                    return;
                                }
                            }
                        }
                    }

                    this.Dispatcher.Invoke(DispatcherPriority.Send, new Action(() =>
                    {
                        GlobalIPAddressTextBox.Text = "";
                        MachineIPAddressTextBox.Text = "";
                        GatewayIPAddressTextBox.Text = "";

                        pw.Close();

                        MessageBox.Show("UPnPの情報が取得できませんでした。");
                    }));
                }));
                startThread.Start();

                pw.Owner = this;
                pw.ShowDialog();
                pw.Button.Click += delegate(object pw_sender, RoutedEventArgs pw_e)
                {
                    startThread.Abort();
                };
            }
        }

        private void ConnectionType_DirectRadioButton_Click(object sender, RoutedEventArgs e)
        {
            GetDirect(true);
        }

        private void ConnectionType_UPnPRadioButton_Click(object sender, RoutedEventArgs e)
        {
            GetUPnP(true);
        }

        private void ConnectionType_OtherRadioButton_Click(object sender, RoutedEventArgs e)
        {
            GetDirect(false);
            if (IpAddressComboBox.Items.Count != 0)
            {
                ConnectionType_DirectRadioButton.IsChecked = true;
                return;
            }

            GetUPnP(false);
            if (GlobalIPAddressTextBox.Text != "")
            {
                ConnectionType_UPnPRadioButton.IsChecked = true;
                return;
            }

            ConnectionType_OtherRadioButton.IsChecked = true;
        }

        private void UpnpClearButton_Click(object sender, RoutedEventArgs e)
        {
            UPnPClient.Clear();
        }

        /// <summary>
        /// 設定情報のチェック
        /// </summary>
        public string DataFormatCheck()
        {
            if (ConnectionType_DirectRadioButton.IsChecked == true)
            {
                if (!Verification.VerificationIPAddress(IpAddressComboBox.Text.Trim()))
                {
                    return "IP Addressが不正です";
                }
                if (PortNamberTextBox.Text.Trim() == "" || !Regex.IsMatch(PortNamberTextBox.Text, "^[0-9]*$") ||
                    int.Parse(PortNamberTextBox.Text) <= 0 || int.Parse(PortNamberTextBox.Text) > 65536)
                {
                    return "ポート番号が不正です";
                }
            }
            else if (ConnectionType_UPnPRadioButton.IsChecked == true)
            {
                if (GlobalIPAddressTextBox.Text.ToString().Trim() == "")
                {
                    return "グローバルIPアドレスが取得できていません";
                }
                if (MachineIPAddressTextBox.Text.ToString().Trim() == "")
                {
                    return "マシンIPアドレスが取得できていません";
                }
                if (GatewayIPAddressTextBox.Text.ToString().Trim() == "")
                {
                    return "ゲートウェイIPアドレスが取得できていません";
                }

                if (ExternalPortTextBox.Text.Trim() == "" || !Regex.IsMatch(ExternalPortTextBox.Text, "^[0-9]*$") ||
                    int.Parse(ExternalPortTextBox.Text) <= 0 || int.Parse(ExternalPortTextBox.Text) > 65536)
                {
                    return "外部ポート番号が不正です";
                }
                if (InternalPortTextBox.Text.Trim() == "" || !Regex.IsMatch(InternalPortTextBox.Text, "^[0-9]*$") ||
                    int.Parse(InternalPortTextBox.Text) <= 0 || int.Parse(InternalPortTextBox.Text) > 65536)
                {
                    return "内部ポート番号が不正です";
                }
            }
            else if (ConnectionType_OtherRadioButton.IsChecked == true)
            {
                if (!Verification.VerificationIPAddress(OtherIpAddressTextBox.Text.Trim()) ||
                    IPAddress.Parse(OtherIpAddressTextBox.Text.Trim()).AddressFamily != AddressFamily.InterNetwork)
                {
                    return "IP Addressが不正です";
                }
                if (OtherPortNamberTextBox.Text.Trim() == "" || !Regex.IsMatch(OtherPortNamberTextBox.Text, "^[0-9]*$") ||
                    int.Parse(OtherPortNamberTextBox.Text) <= 0 || int.Parse(OtherPortNamberTextBox.Text) > 65536)
                {
                    return "ポート番号が不正です";
                }
            }
            else
            {
                return "未指定";
            }

            if (PublicKeyTextBox.Text.Trim() == "")
            {
                return "Public Keyが不正です";
            }
            if (PrivateKeyTextBox.Text.Trim() == "")
            {
                return "Private Keyが不正です";
            }
            if (SignTextBox.Text.Trim().Length > 10)
            {
                return "サインが長すぎます。10文字以下にしてください";
            }

            if (!Directory.Exists(CacheDirectoryTextBox.Text))
            {
                return "キャッシュフォルダへのパスが不正です";
            }
            if (!Directory.Exists(DownloadDirectoryTextBox.Text))
            {
                return "ダウンロードフォルダへのパスが不正です";
            }

            try
            {
                byte[] testData = { 0, 127, 255 };

                if (!BinaryEditor.ArrayEquals(testData,
                    RSA_Encryption.Decrypt(RSA_Encryption.Encrypt(testData, PublicKeyTextBox.Text), PrivateKeyTextBox.Text)))
                {
                    return "Public Key または Private Keyが不正です";
                }
            }
            catch
            {
                return "Public Key または Private Keyが不正です";
            }

            return "";
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            string chk = DataFormatCheck();

            if ("" != chk)
            {
                MessageBox.Show(chk);
                return;
            }

            if (ConnectionType_DirectRadioButton.IsChecked == true)
            {
                Settings.Default.ConnectionType = ConnectionType.Direct;
                Settings.Default.DirectConnectionInformation.IPAddress = IpAddressComboBox.Text.Trim();
                Settings.Default.DirectConnectionInformation.Port = int.Parse(PortNamberTextBox.Text.Trim());
            }
            else if (ConnectionType_UPnPRadioButton.IsChecked == true)
            {
                Settings.Default.ConnectionType = ConnectionType.UPnP;
                Settings.Default.UpnpConnectionInformation.GlobalIPAddress = GlobalIPAddressTextBox.Text.Trim();
                Settings.Default.UpnpConnectionInformation.MachineIPAddress = MachineIPAddressTextBox.Text.Trim();
                Settings.Default.UpnpConnectionInformation.GatewayIPAddress = GatewayIPAddressTextBox.Text.Trim();
                Settings.Default.UpnpConnectionInformation.ExternalPort = int.Parse(ExternalPortTextBox.Text.Trim());
                Settings.Default.UpnpConnectionInformation.InternalPort = int.Parse(InternalPortTextBox.Text.Trim());
            }
            else if (ConnectionType_OtherRadioButton.IsChecked == true)
            {
                Settings.Default.ConnectionType = ConnectionType.Other;
                Settings.Default.OtherConnectionInformation.IPAddress = OtherIpAddressTextBox.Text.Trim();
                Settings.Default.OtherConnectionInformation.Port = int.Parse(OtherPortNamberTextBox.Text.Trim());
            }

            Settings.Default.Sign = SignTextBox.Text.Trim();
            Settings.Default.PublicKey = PublicKeyTextBox.Text;
            Settings.Default.PrivateKey = PrivateKeyTextBox.Text;

            Settings.Default.CacheDirectoryPath = CacheDirectoryTextBox.Text;
            Settings.Default.DownloadDirectoryPath = DownloadDirectoryTextBox.Text;

            Settings.Default._cacheController.CacheDirectoryPath = Settings.Default.CacheDirectoryPath;
            Settings.Default._cacheController.PrivateKey = Settings.Default.PrivateKey;
            Settings.Default._cacheController.PublicKey = Settings.Default.PublicKey;
            Settings.Default._cacheController.Sign = Settings.Default.Sign;

            Settings.Default.TabRadioButton = (bool)NormalRadioButton.IsChecked;
            Settings.Default.TreeRadioButton = (bool)TreeRadioButton.IsChecked;

            Settings.Default.QueryList = QueryListTextBox.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(n => Cache.CategoryRegularization(n))
                .ToArray();

            Settings.Default.DisplaySize_1 = (bool)DisplaySize_1_RadioButton.IsChecked;
            Settings.Default.DisplaySize_2 = (bool)DisplaySize_2_RadioButton.IsChecked;
            Settings.Default.DisplaySize_3 = (bool)DisplaySize_3_RadioButton.IsChecked;

            try
            {
                this.DialogResult = true;
            }
            catch (InvalidOperationException) { }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            PublicKeyTextBox.Text = Settings.Default.PublicKey;
            PrivateKeyTextBox.Text = Settings.Default.PrivateKey;
            CacheDirectoryTextBox.Text = Settings.Default.CacheDirectoryPath;
            DownloadDirectoryTextBox.Text = Settings.Default.DownloadDirectoryPath;

            try
            {
                this.DialogResult = false;
            }
            catch (InvalidOperationException) { }
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            string publickey, privatekey;

            RSA_Encryption.CreateKeys(out publickey, out privatekey);

            PublicKeyTextBox.Text = publickey;
            PrivateKeyTextBox.Text = privatekey;
        }

        private void DownloadDirectoryTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                DownloadDirectoryTextBox.Text = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            }
        }

        private void DownloadDirectoryTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void CacheDirectoryTextBox_PreviewDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                CacheDirectoryTextBox.Text = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
            }
        }

        private void CacheDirectoryTextBox_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.All;
            e.Handled = true;
        }

        private void PublicKeyTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            try
            {
                IdLabel.Content = Convert.ToBase64String(HashFunction.HashCreate(this.PublicKeyTextBox.Text));
            }
            catch (NullReferenceException) { }
        }

        private void downloadButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "ダウンロードディレクトリを設定します";
            dialog.SelectedPath = DownloadDirectoryTextBox.Text.Trim();

            if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
            {
                DownloadDirectoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void cacheButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "キャッシュディレクトリを設定します";
            dialog.SelectedPath = CacheDirectoryTextBox.Text.Trim();

            if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
            {
                CacheDirectoryTextBox.Text = dialog.SelectedPath;
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
    }

    /// <summary>
    /// Profesの接続方法
    /// </summary>
    [Serializable]
    public enum ConnectionType
    {
        /// <summary>
        /// 直接接続しています
        /// </summary>
        Direct = 0,

        /// <summary>
        /// UPnP対応ルータ越しに接続しています
        /// </summary>
        UPnP = 1,

        /// <summary>
        /// その他
        /// </summary>
        Other = 2,
    }

    /// <summary>
    /// 直接接続情報を提供します
    /// </summary>
    [Serializable]
    public class DirectConnectionInformation
    {
        /// <summary>
        /// アドレス
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// ポート
        /// </summary>
        public int Port { get; set; }
    }

    /// <summary>
    /// UPnP接続情報を提供します
    /// </summary>
    [Serializable]
    public class UpnpConnectionInformation
    {
        /// <summary>
        /// グローバルIPアドレス
        /// </summary>
        public string GlobalIPAddress { get; set; }

        /// <summary>
        /// マシンIPアドレス
        /// </summary>
        public string MachineIPAddress { get; set; }

        /// <summary>
        /// ゲートウェイIPアドレス
        /// </summary>
        public string GatewayIPAddress { get; set; }

        /// <summary>
        /// 外部ポート
        /// </summary>
        public int ExternalPort { get; set; }

        /// <summary>
        /// 内部ポート
        /// </summary>
        public int InternalPort { get; set; }
    }

    /// <summary>
    /// その他の接続情報を提供します
    /// </summary>
    [Serializable]
    public class OtherConnectionInformation
    {
        /// <summary>
        /// アドレス
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// ポート
        /// </summary>
        public int Port { get; set; }
    }
}