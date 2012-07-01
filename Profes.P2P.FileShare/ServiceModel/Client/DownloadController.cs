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

namespace Profes.P2P.FileShare.ServiceModel.Client
{
    /// <summary>
    /// FileShareClient の NodeShow イベントを処理するメソッドを表します。
    /// </summary>
    public delegate void FileShareClientNodeShowEventHandler(object sender, NodeListViewItem e);

    class DownloadController : IDisposable
    {
        private Timer _downloadTimer;

        static public event FileShareServiceDebugWriteEventHandler DebugWrite;
        static public event FileShareClientNodeShowEventHandler NodeShowAdd;
        static public event FileShareClientNodeShowEventHandler NodeShowRemove;

        private List<byte[]> _downloadCacheHashList = new List<byte[]>();
        private static List<byte[]> _downloadNodeHash = new List<byte[]>();
        private static Dictionary<string, List<int>> _downloadBlockDic = new Dictionary<string, List<int>>();

        public DownloadController()
        {
            _downloadTimer = new Timer(new TimerCallback(_downloadTimer_Clock), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _downloadTimer.Change(0, 2 * 1000);
        }

        public void Stop()
        {
            _downloadTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region ダウンロード

        private void _downloadTimer_Clock(object sender)
        {
            if (_downloadCacheHashList.Count >= Settings.Default.DownloadTimerMaxCount) return;

            CacheListViewItem item = Settings.Default._downloadList.
                FirstOrDefault(n => !_downloadCacheHashList.Any(m => BinaryEditor.ArrayEquals(n.Hash, m)));

            if (item == null) return;

            _downloadCacheHashList.Add(item.Hash);

            try
            {
                lock (Settings.Default._keyController)
                {
                    foreach (Key key in Settings.Default._keyController.Search(item.Hash))
                    {
                        if (BinaryEditor.ArrayEquals(Settings.Default._routeTable.MyNode.NodeID, key.FileLocation.NodeID)) continue;

                        lock (_downloadNodeHash)
                        {
                            if (_downloadNodeHash.Any(n => BinaryEditor.ArrayEquals(n, key.FileLocation.NodeID))) continue;
                        }

                        DownloadCommunication dc = new DownloadCommunication(key, item);
                        Thread thread = new Thread(new ThreadStart(dc.Communication));

                        thread.Start();
                    }
                }
            }
            finally
            {
                lock (_downloadCacheHashList)
                {
                    _downloadCacheHashList.RemoveAll(n => BinaryEditor.ArrayEquals(n, item.Hash));
                }
            }

            Cache cache;

            lock (Settings.Default._cacheController.CacheList)
            {
                cache = Settings.Default._cacheController.CacheList.
                    FirstOrDefault(n => BinaryEditor.ArrayEquals(n.SignatureHash, item.SignatureHash));
            }

            if (cache == null)
            {
                try
                {
                    lock (Settings.Default._cacheController.CacheList)
                    {
                        cache = Clone.DeepCopyClone<Cache>(Settings.Default._cacheController.CacheList
                            .First(n => BinaryEditor.ArrayEquals(n.Hash, item.Hash)));
                    }

                    Key key = item.Key != null ? item.Key : item.Cache.Key;

                    if (cache != null && key != null)
                    {
                        cache.Key = Clone.DeepCopyClone<Key>(key);
                        cache.Key.FileLocation = new Node();
                        cache.Key.Review = null;

                        cache.Category = item.Key.Cache_Category;
                        cache.CreationTime = item.Key.Cache_CreationTime;
                        cache.Name = item.Key.Cache_FileName;
                        cache.Sign = item.Key.Cache_Sign;
                        cache.Size = item.Key.Cache_Size;
                        cache.Signature = item.Key.Cache_Signature;
                        cache.Signature_SHA1 = item.Key.Cache_Signature_SHA1;
                        cache.PublicKey = item.Key.PublicKey;

                        if (!cache.VerifyDigitalSignature()) throw new ApplicationException("電子署名が不正です");

                        lock (Settings.Default._cacheController.CacheList)
                        {
                            Settings.Default._cacheController.CacheList.Add(cache);
                        }
                    }
                }
                catch (ApplicationException ex)
                {
                    DebugWrite(this, ex.Message);
                }
                catch (InvalidOperationException) { }
                catch (ArgumentNullException) { }
            }

            if (cache != null && Settings.Default._cacheController.Rate(cache) == 100)
            {
                bool convertFlag = false;

                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                {
                    if (Settings.Default._downloadList.Any(n => BinaryEditor.ArrayEquals(n.SignatureHash, item.SignatureHash)))
                    {
                        Settings.Default._downloadList.Remove(item);
                        convertFlag = true;
                    }
                }));

                try
                {
                    if (convertFlag)
                        Settings.Default._cacheController.CacheToFile(cache, Settings.Default.DownloadDirectoryPath);
                }
                catch (ApplicationException ex)
                {
                    DebugWrite(this, ex.Message);
                }
            }
        }

        private class DownloadCommunication
        {
            Key key;
            CacheListViewItem item;

            public DownloadCommunication(Key key, CacheListViewItem item)
            {
                this.key = key;
                this.item = item;
            }

            public void Communication()
            {
                lock (_downloadNodeHash)
                {
                    _downloadNodeHash.Add(key.FileLocation.NodeID);
                }

                NodeListViewItem nlvi = null;
                string stringHash = "";
                int i = -1;

                IFileShareService proxy = null;
                try
                {
                    stringHash = BinaryEditor.BytesToHexString(item.Hash);
                    Cache cache;

                    lock (Settings.Default._cacheController.CacheList)
                    {
                        cache = Settings.Default._cacheController.CacheList.
                            FirstOrDefault(n => BinaryEditor.ArrayEquals(n.SignatureHash, item.SignatureHash));
                    }

                    lock (_downloadBlockDic)
                    {
                        if (!_downloadBlockDic.ContainsKey(stringHash))
                            _downloadBlockDic.Add(stringHash, new List<int>());
                    }

                    nlvi = new NodeListViewItem()
                    {
                        CommunicationType = "download",
                        Description = "キャッシュブロックを受信しています",
                        Node = key.FileLocation,
                    };
                    NodeShowAdd(this, nlvi);

                    using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", key.FileLocation.Endpoint))
                    {
                        proxy = channel.CreateChannel();

                        if (cache == null)
                        {
                            cache = new Cache();
                            cache.Key = Clone.DeepCopyClone<Key>(key);
                            cache.Key.FileLocation = new Node();
                            cache.Key.Review = null;

                            cache.Category = key.Cache_Category;
                            cache.CreationTime = key.Cache_CreationTime;
                            cache.Name = key.Cache_FileName;
                            cache.Sign = key.Cache_Sign;
                            cache.Size = key.Cache_Size;
                            cache.Signature = key.Cache_Signature;
                            cache.Signature_SHA1 = key.Cache_Signature_SHA1;
                            cache.PublicKey = key.PublicKey;
                            cache.CacheBlockHash = proxy.GetCacheBlockHashList(key.Cache_Hash);

                            if (!cache.VerifyDigitalSignature()) throw new ApplicationException("電子署名が不正です");

                            lock (Settings.Default._cacheController.CacheList)
                            {
                                Settings.Default._cacheController.CacheList.Add(cache);
                            }
                        }

                        for (; ; )
                        {
                            lock (_downloadBlockDic)
                            {
                                List<int> bmp = new List<int>();

                                for (int j = 0; j < item.CacheBlockLength; j++)
                                {
                                    if ((!Settings.Default._cacheController.ContainsKey(stringHash) ||
                                            Settings.Default._cacheController[stringHash][j] == null) && key.CacheBlockBitmap[j] == true &&
                                            !_downloadBlockDic[stringHash].Any(n => n == j))
                                    {
                                        bmp.Add(j);
                                    }
                                }

                                if (bmp.Count == 0) return;

                                i = bmp[new Random().Next(bmp.Count)];
                                _downloadBlockDic[stringHash].Add(i);
                            }

                            byte[] block = proxy.GetCacheBlock(item.Hash, i);
                            DebugWrite(this, "download成功：キャッシュブロックのダウンロードに成功しました");

                            if (block != null)
                                Settings.Default._cacheController[cache, i] = block;

                            lock (_downloadBlockDic)
                            {
                                _downloadBlockDic[stringHash].Remove(i);
                            }
                        }
                    }
                }
                catch (EndpointNotFoundException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (TimeoutException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (FaultException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (CommunicationException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (NullReferenceException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (ArgumentNullException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (ArgumentException ex)
                {
                    Debug.WriteLine("DownloadCommunication" + ex.Message);
                }
                catch (ApplicationException ex)
                {
                    DebugWrite(this, "DownloadCommunication：" + ex.Message);

                    Debug.WriteLine("DownloadCommunication" + ex.Message);
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

                    lock (_downloadBlockDic)
                    {
                        if (i != -1)
                            _downloadBlockDic[stringHash].Remove(i);
                    }

                    if (nlvi != null)
                        NodeShowRemove(this, nlvi);

                    lock (_downloadNodeHash)
                    {
                        _downloadNodeHash.RemoveAll(n => BinaryEditor.ArrayEquals(n, key.FileLocation.NodeID));
                    }
                }
            }
        }

        #endregion

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。DownloadController によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this._downloadTimer.Dispose();
        }

        #endregion
    }
}