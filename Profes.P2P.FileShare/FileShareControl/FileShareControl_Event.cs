using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.Windows.Threading;
using Profes.BinaryEditorBase;
using Profes.P2P.FileShare.Properties;
using Profes.P2P.FileShare.ServiceModel;
using Profes.Security.Cryptography;
using System.Windows;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.IO;
using Profes.P2P.FileShare.ServiceModel.Client;

namespace Profes.P2P.FileShare.FileShareControl
{
    public partial class FileShareControl
    {
        private Timer _BackupTimer;

        private StoreController _storeController;
        private QueryController _queryController;
        private DownloadController _downloadController;
        private UploadController _uploadController;
        private UploadDiffusionController _uploadDiffusionController;

        void EventInit()
        {
            LogWrite("EventInit開始");

            _storeController = new StoreController();
            _queryController = new QueryController();
            _downloadController = new DownloadController();
            _uploadController = new UploadController();
            _uploadDiffusionController = new UploadDiffusionController();

            StoreController.DebugWrite += new FileShareServiceDebugWriteEventHandler(DebugWrite);
            StoreController.NodeShowAdd += new FileShareClientNodeShowEventHandler(NodeShowAdd);
            StoreController.NodeShowRemove += new FileShareClientNodeShowEventHandler(NodeShowRemove);

            QueryController.DebugWrite += new FileShareServiceDebugWriteEventHandler(DebugWrite);
            QueryController.NodeShowAdd += new FileShareClientNodeShowEventHandler(NodeShowAdd);
            QueryController.NodeShowRemove += new FileShareClientNodeShowEventHandler(NodeShowRemove);

            DownloadController.DebugWrite += new FileShareServiceDebugWriteEventHandler(DebugWrite);
            DownloadController.NodeShowAdd += new FileShareClientNodeShowEventHandler(NodeShowAdd);
            DownloadController.NodeShowRemove += new FileShareClientNodeShowEventHandler(NodeShowRemove);

            UploadController.DebugWrite += new FileShareServiceDebugWriteEventHandler(DebugWrite);
            UploadController.NodeShowAdd += new FileShareClientNodeShowEventHandler(NodeShowAdd);
            UploadController.NodeShowRemove += new FileShareClientNodeShowEventHandler(NodeShowRemove);

            UploadDiffusionController.DebugWrite += new FileShareServiceDebugWriteEventHandler(DebugWrite);
            UploadDiffusionController.NodeShowAdd += new FileShareClientNodeShowEventHandler(NodeShowAdd);
            UploadDiffusionController.NodeShowRemove += new FileShareClientNodeShowEventHandler(NodeShowRemove);

            _BackupTimer = new Timer(new TimerCallback(_BackupTimer_Clock), null, Timeout.Infinite, 1000 * 60);

            Settings.Default._routeTable.Ping += new RouteTablePingEventHandler(_routeTable_Ping);

            Settings.Default._keyController.AddedNew += new KeyControllerAddedNewEventHandler(_keyController_AddedNew);
            Settings.Default._downloadList.ListChanged += new System.ComponentModel.ListChangedEventHandler(_downloadList_ListChanged);
        }

        private void EventEnd()
        {
            if (_storeController != null) _storeController.Dispose();
            if (_queryController != null) _queryController.Dispose();
            if (_downloadController != null) _downloadController.Dispose();
            if (_uploadController != null) _uploadController.Dispose();
            if (_uploadDiffusionController != null) _uploadDiffusionController.Dispose();
        }

        private void StartEvent()
        {
            LogWrite("Event開始");

            if (_storeController != null) _storeController.Start();
            if (_queryController != null) _queryController.Start();
            if (_downloadController != null) _downloadController.Start();
            if (_uploadController != null) _uploadController.Start();
            if (_uploadDiffusionController != null) _uploadDiffusionController.Start();
        }

        private void StopEvent()
        {
            LogWrite("Event停止");
            if (_storeController != null) _storeController.Stop();
            if (_queryController != null) _queryController.Stop();
            if (_downloadController != null) _downloadController.Stop();
            if (_uploadController != null) _uploadController.Stop();
            if (_uploadDiffusionController != null) _uploadDiffusionController.Stop();
        }

        void DebugWrite(object sender, string e)
        {
            LogWrite(e);
        }

        void NodeShowAdd(object sender, NodeListViewItem e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                Settings.Default._nodeShowList.Add(e);
            }));
        }

        void NodeShowRemove(object sender, NodeListViewItem e)
        {
            this.Dispatcher.Invoke(DispatcherPriority.Background, new Action(() =>
            {
                if (Settings.Default._nodeShowList.Any(n => n == e))
                    Settings.Default._nodeShowList.Remove(e);
            }));
        }

        private void _BackupTimer_Clock(object sender)
        {
            Settings.Default.Save();
        }

        void _downloadList_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            if (e.ListChangedType == System.ComponentModel.ListChangedType.ItemAdded)
            {
                var clvi = Settings.Default._downloadList[e.NewIndex];

                lock (Settings.Default.DownloadHistory)
                {
                    Settings.Default.DownloadHistory.Add(clvi);
                }
            }
        }


        bool _routeTable_Ping(object sender, Node e)
        {
            try
            {
                using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", e.Endpoint))
                {
                    IFileShareService proxy = channel.CreateChannel();
                    Settings.Default._routeTable.AddRange(proxy.GetRouteTable(new byte[] { 0 }));

                    LogWrite("_routeTable_Ping成功：ノードの生存を確認しました");
                    
                    return true;
                }
            }
            catch (EndpointNotFoundException ex)
            {
                LogWrite("_routeTable_Ping成功：ノードの死亡を確認しました");

                Debug.WriteLine("_routeTable_Ping" + ex.Message);
                return false;
            }
            catch (TimeoutException ex)
            {
                LogWrite("_routeTable_Ping成功：ノードの死亡を確認しました");

                Debug.WriteLine("_routeTable_Ping" + ex.Message);
                return false;
            }
            catch (FaultException ex)
            {
                LogWrite("_routeTable_Ping成功：ノードの死亡を確認しました");

                Debug.WriteLine("_routeTable_Ping" + ex.Message);
                return false;
            }
            catch (CommunicationException ex)
            {
                LogWrite("_routeTable_Ping成功：ノードの死亡を確認しました");

                Debug.WriteLine("_routeTable_Ping" + ex.Message);
                return false;
            }
            catch (NullReferenceException ex)
            {
                LogWrite("_routeTable_Ping成功：ノードの死亡を確認しました");

                Debug.WriteLine("_routeTable_Ping" + ex.Message);
                return false;
            }
        }

        #region 30%の確率でキーの書き換え

        Dictionary<string, bool> _rewriteDic = new Dictionary<string, bool>();

        void _keyController_AddedNew(object sender, Key e)
        {
            if (BinaryEditor.ArrayEquals(e.FileLocation.NodeID, Settings.Default._routeTable.MyNode.NodeID)) return;

            string stringHash = BinaryEditor.BytesToHexString(e.FileLocation.NodeID) + BinaryEditor.BytesToHexString(e.Cache_Hash);
            bool flag;

            if (_rewriteDic.ContainsKey(stringHash))
            {
                flag = _rewriteDic[stringHash];
            }
            else
            {
                try
                {
                    if (((double)_rewriteDic.Values.Count(n => n == true) / (double)_rewriteDic.Values.Count()) * 100 < 30)
                    {
                        _rewriteDic.Add(stringHash, true);
                        flag = true;
                    }
                    else
                    {
                        _rewriteDic.Add(stringHash, false);
                        flag = false;
                    }
                }
                catch (DivideByZeroException)
                {
                    _rewriteDic.Add(stringHash, true);
                    flag = true;
                }
            }

            if (flag == true)
            {
                Key CloneKey = Clone.DeepCopyClone<Key>(e);
                CloneKey.FileLocation = Settings.Default._routeTable.MyNode;

                Settings.Default._keyController.Add(CloneKey);
            }
        }

        #endregion
    }
}