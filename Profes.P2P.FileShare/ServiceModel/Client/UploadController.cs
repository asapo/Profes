using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Profes.P2P.FileShare.FileShareControl;
using Profes.P2P.FileShare.Properties;
using Profes.BinaryEditorBase;
using System.ServiceModel;
using System.Diagnostics;
using System.Windows.Threading;
using System.Windows;

namespace Profes.P2P.FileShare.ServiceModel.Client
{
    class UploadController : IDisposable
    {
        private Timer _uploadTimer;

        static public event FileShareServiceDebugWriteEventHandler DebugWrite;
        static public event FileShareClientNodeShowEventHandler NodeShowAdd;
        static public event FileShareClientNodeShowEventHandler NodeShowRemove;

        private List<int> _uploadBlockIndex = new List<int>();
        private int _uploadCount = 0;
        private List<byte[]> _uploadNodeHash = new List<byte[]>();

        public UploadController()
        {
            _uploadTimer = new Timer(new TimerCallback(_uploadTimer_Clock), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _uploadTimer.Change(0, 2 * 1000);
        }

        public void Stop()
        {
            _uploadTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region アップロード

        private void _uploadTimer_Clock(object sender)
        {
            if (Settings.Default._uploadList.Count == 0) return;
            if (_uploadCount >= Settings.Default.UploadTimerMaxCount) return;

            _uploadCount++;
            CacheListViewItem item = null;

            try
            {
                item = Settings.Default._uploadList[0];

                int i = 0;
                while (i < item.UploadBlockBitmap.Length && (_uploadBlockIndex.Contains(i) || item.UploadBlockBitmap[i] == true)) i++;
                if (i >= item.UploadBlockBitmap.Length) return;

                Cache cache;

                lock (Settings.Default._cacheController.CacheList)
                {
                    cache = Settings.Default._cacheController.CacheList.
                        FirstOrDefault(n => BinaryEditor.ArrayEquals(n.SignatureHash, item.SignatureHash));
                }

                if (cache == null) return;

                _uploadBlockIndex.Add(i);

                Node node;
                lock (_uploadNodeHash)
                {
                    for (; ; )
                    {
                        node = Clone.DeepCopyClone<Node>(Settings.Default._routeTable.Random());
                        if (!_uploadNodeHash.Any(n => BinaryEditor.ArrayEquals(n, node.NodeID))) break;
                    }

                    _uploadNodeHash.Add(node.NodeID);
                }

                item.UploadBlockBitmap[i] = UploadCommunication(
                    node,
                    cache,
                    Settings.Default._cacheController[cache, i]);

                if (item.UploadBlockBitmap.Count(n => n == false) == 0)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Settings.Default._uploadList.Remove(item);
                    }));
                }

                lock (_uploadBlockIndex)
                {
                    _uploadBlockIndex.Remove(i);
                }
                lock (_uploadNodeHash)
                {
                    _uploadNodeHash.RemoveAll(n => BinaryEditor.ArrayEquals(n, node.NodeID));
                }
            }
            catch (ArgumentNullException)
            {
            }
            catch (ApplicationException)
            {
                if (item != null)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                    {
                        Settings.Default._uploadList.Remove(item);
                    }));
                }
            }
            finally
            {
                _uploadCount--;
            }
        }

        private bool UploadCommunication(Node node, Cache cache, byte[] value)
        {
            NodeListViewItem nlvi = null;

            IFileShareService proxy = null;
            try
            {
                nlvi = new NodeListViewItem()
                {
                    CommunicationType = "Upload",
                    Description = "キャッシュブロックを送信しています",
                    Node = node,
                };
                NodeShowAdd(this, nlvi);

                using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", node.Endpoint))
                {
                    proxy = channel.CreateChannel();
                    proxy.SetCacheBlock(cache, value, DateTime.Now);

                    DebugWrite(this, "UploadCommunication成功：キャッシュブロックのアップロードに成功しました");
                    return true;
                }
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (FaultException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (ArgumentNullException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (ApplicationException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("UploadCommunication" + ex.Message);
                return false;
            }
            finally
            {
                if (proxy != null)
                {
                    try
                    {
                        ((IClientChannel)proxy).Close();
                    }
                    catch
                    {
                        ((IClientChannel)proxy).Abort();
                    }
                }

                if (nlvi != null)
                    NodeShowRemove(this, nlvi);
            }
        }

        #endregion

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。UploadController によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this._uploadTimer.Dispose();
        }

        #endregion
    }
}