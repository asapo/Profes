using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Profes.BinaryEditorBase;
using Profes.P2P.FileShare.Properties;
using Profes.P2P.FileShare.ServiceModel;
using Profes.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization.Formatters.Soap;
using Profes.UPnP;
using Profes.DialogBox;

namespace Profes.P2P.FileShare.FileShareControl
{
    /// <summary>
    /// FileShareControl.xaml �̑��ݍ�p���W�b�N
    /// </summary>
    public partial class FileShareControl : UserControl, IDisposable
    {
        Thread _serviceHostThread = null;

        /// <summary>
        /// FileShareControl�N���X�̐V�����C���X�^���X�����������܂�
        /// </summary>
        public FileShareControl()
        {
            InitializeComponent();
            EventInit();

            FileShareService.DebugWrite += new FileShareServiceDebugWriteEventHandler(FileShareService_DebugWrite);

            if (Settings.Default.TreeRadioButton == true)
                QueryGrid.Children.Add(new FileShareTreeQueryControl_2());

            if (Settings.Default.TabRadioButton == true)
                QueryGrid.Children.Add(new FileShareTabQueryControl());

            string chk = "";

            using (SettingWindow settingWindow = new SettingWindow())
            {
                chk = settingWindow.DataFormatCheck();
            }

            if (chk == "")
            {
                StartService();
            }
        }

        /// <summary>
        /// �T�[�r�X���J�n����
        /// </summary>
        private void StartService()
        {
            LogWrite("�T�[�r�X�J�n");

            Node node = new Node();
            switch (Settings.Default.ConnectionType)
            {
                case ConnectionType.Direct:
                    if (Settings.Default.DirectConnectionInformation.IPAddress.Contains(":"))
                    {
                        node.Endpoint = new EndpointAddress(string.Format("net.tcp://[{0}]:{1}/FileShareService",
                            Settings.Default.DirectConnectionInformation.IPAddress, Settings.Default.DirectConnectionInformation.Port));
                    }
                    else
                    {
                        node.Endpoint = new EndpointAddress(string.Format("net.tcp://{0}:{1}/FileShareService",
                            Settings.Default.DirectConnectionInformation.IPAddress, Settings.Default.DirectConnectionInformation.Port));
                    }

                    Settings.Default._routeTable.MyNode = node;

                    break;

                case ConnectionType.UPnP:
                    node.Endpoint = new EndpointAddress(string.Format("net.tcp://{0}:{1}/FileShareService",
                        Settings.Default.UpnpConnectionInformation.GlobalIPAddress, Settings.Default.UpnpConnectionInformation.ExternalPort));

                    if (!UPnPClient.OpenFirewallPort(Settings.Default.UpnpConnectionInformation.MachineIPAddress,
                        Settings.Default.UpnpConnectionInformation.GatewayIPAddress,
                        Settings.Default.UpnpConnectionInformation.ExternalPort,
                        Settings.Default.UpnpConnectionInformation.InternalPort))
                    {
                        MessageBox.Show("�|�[�g���J���ł��܂���ł����B");

                        this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            MessageBoxStatusBarItem.Content = "UPnP�ɂ��|�[�g�J�����s";
                        }));

                        return;
                    }

                    Settings.Default._routeTable.MyNode = node;

                    break;

                case ConnectionType.Other:
                    node.Endpoint = new EndpointAddress(string.Format("net.tcp://{0}:{1}/FileShareService",
                        Settings.Default.OtherConnectionInformation.IPAddress, Settings.Default.OtherConnectionInformation.Port));

                    Settings.Default._routeTable.MyNode = node;

                    break;
            }

            LogWrite(string.Format("MyNode Uri: \"{0}\" ID: \"{1}\"", node.Endpoint.ToString(), 
                BinaryEditor.BytesToHexString(node.NodeID)));

            _serviceHostThread = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    Node serviceNode = new Node();

                    switch (Settings.Default.ConnectionType)
                    {
                        case ConnectionType.Direct:
                            serviceNode = Settings.Default._routeTable.MyNode;

                            break;

                        case ConnectionType.UPnP:
                            serviceNode.Endpoint = new EndpointAddress(string.Format("net.tcp://{0}:{1}/FileShareService",
                                Settings.Default.UpnpConnectionInformation.MachineIPAddress, Settings.Default.UpnpConnectionInformation.InternalPort));

                            break;

                        case ConnectionType.Other:
                            serviceNode.Endpoint = new EndpointAddress(string.Format("net.tcp://0.0.0.0:{0}/FileShareService",
                                Settings.Default.OtherConnectionInformation.Port));

                            break;
                    }

                    Settings.Default._serviceHost = new ServiceHost(typeof(FileShareService), serviceNode.Endpoint.Uri);
                    Settings.Default._serviceHost.Open();

                    LogWrite(string.Format("ServiceHost Uri: \"{0}\" ID: \"{1}\"", serviceNode.Endpoint.ToString(),
                        BinaryEditor.BytesToHexString(serviceNode.NodeID)));

                    StartEvent();
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message,
                        "Profes, Profes.P2P.FileShare.dll",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    try
                    {
                        Settings.Default._serviceHost.Abort();
                    }
                    catch{ }

                    this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        MessageBoxStatusBarItem.Content = "�g�p�ł��Ȃ�IP�A�h���X";
                    }));
                }
            }));

            _serviceHostThread.Start();

            Settings.Default.IsServiceStarted = true;

            this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            {
                MessageBoxStatusBarItem.Content = "�T�[�r�X�J�n";
            }));
        }

        /// <summary>
        /// �T�[�r�X���I������
        /// </summary>
        private void StopService()
        {
            StopEvent();

            if (Settings.Default._serviceHost != null)
            {
                try
                {
                    Settings.Default._serviceHost.Close();
                }
                catch
                {
                    try
                    {
                        Settings.Default._serviceHost.Abort();
                    }
                    catch { }
                }

                if (Settings.Default.ConnectionType == ConnectionType.UPnP)
                {
                    UPnPClient.CloseFirewallPort(Settings.Default.UpnpConnectionInformation.MachineIPAddress,
                            Settings.Default.UpnpConnectionInformation.GatewayIPAddress,
                            Settings.Default.UpnpConnectionInformation.ExternalPort);
                }

                Settings.Default.IsServiceStarted = false;

                this.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    MessageBoxStatusBarItem.Content = "�T�[�r�X�I��";
                }));

                LogWrite("�T�[�r�X�I��");
            }
        }

        void FileShareService_DebugWrite(object sender, string e)
        {
            LogWrite(e);
        }

        public void LogWrite(string ss)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (LogCheckBox != null && LogCheckBox.IsChecked == false)
                {
                    var ssl = LogTextBox.Text.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                    using (StringWriter sw = new StringWriter())
                    {
                        sw.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + ":\t" + ss);
                        for (int i = 0; i < 49 && ssl.Length > i; i++)
                        {
                            sw.WriteLine(ssl[i]);
                        }

                        LogTextBox.Text = sw.ToString();
                    }
                }
            }));
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Text = "";


            var pw = new ProgressWindow();
            pw.Topmost = true;
            pw.MessageLabel.Content = "IP�A�h���X����ǂݍ���ł��܂�...";
            pw.ProgressBar.IsIndeterminate = true;
            pw.ShowDialog();

        }

        bool settingsWindowShowFlag = false;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindowShowFlag == true)
            {
                MessageBox.Show("�T�[�r�X�̍X�V���ł��B���΂炭���҂���������");
                return;
            }

            try
            {
                using (SettingWindow settingWindow = new SettingWindow())
                {
                    settingWindow.Owner = System.Windows.Application.Current.Windows[0];
                    settingWindow.ShowDialog();
                    if (settingWindow.DialogResult != true) return;
                    if (settingWindow.DataFormatCheck() != "") return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("��肪�����������߃E�C���h�E��\���ł��܂���ł���\r\n" + ex.Message);
                return;
            }

            QueryGrid.Children.Clear();

            if (Settings.Default.TreeRadioButton == true)
                QueryGrid.Children.Add(new FileShareTreeQueryControl_2());

            if (Settings.Default.TabRadioButton == true)
                QueryGrid.Children.Add(new FileShareTabQueryControl());

            Thread stopThread = new Thread(new ThreadStart(delegate()
            {
                settingsWindowShowFlag = true;

                StopService();
            }));
            stopThread.Start();

            Thread startThread = new Thread(new ThreadStart(delegate()
            {
                stopThread.Join();
                StartService();

                settingsWindowShowFlag = false;
            }));
            startThread.Start();
        }

        private void NodeSettingButton_Click(object sender, RoutedEventArgs e)
        {
            using (NodeSettingWindow nodeSettingWindow = new NodeSettingWindow())
            {
                nodeSettingWindow.ShowDialog();
            }
        }

        #region IDisposable �����o

        /// <summary>
        /// �C���t���X�g���N�`���BFileShareControl �ɂ���Ďg�p����Ă��邷�ׂẴ��\�[�X��������܂��B
        /// </summary>
        public void Dispose()
        {
            EventEnd();

            if (Settings.Default._serviceHost != null)
                Settings.Default._serviceHost.Abort();

            Settings.Default.Save();

            if (Settings.Default.ConnectionType == ConnectionType.UPnP)
            {
                UPnPClient.CloseFirewallPort(Settings.Default.UpnpConnectionInformation.MachineIPAddress,
                        Settings.Default.UpnpConnectionInformation.GatewayIPAddress,
                        Settings.Default.UpnpConnectionInformation.ExternalPort);
            }
        }

        #endregion
    }
}