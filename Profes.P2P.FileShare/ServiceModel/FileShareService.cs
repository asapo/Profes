using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Profes.P2P.FileShare.Properties;
using Profes.BinaryEditorBase;
using System.Diagnostics;
using Profes.Security.Cryptography;
using System.ServiceModel;
using Profes.P2P.FileShare.FileShareControl;
using System.Timers;
using System.ServiceModel.Channels;
using System.Windows;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// FileShareService の DebugWrite イベントを処理するメソッドを表します。
    /// </summary>
    public delegate void FileShareServiceDebugWriteEventHandler(object sender, string e);

    [ServiceBehaviorAttribute(InstanceContextMode = InstanceContextMode.PerSession, UseSynchronizationContext = false, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FileShareService : IFileShareService
    {
        static public event FileShareServiceDebugWriteEventHandler DebugWrite;

        public FileShareService() { }

        private string GetIP()
        {
            return ((RemoteEndpointMessageProperty)OperationContext.Current.IncomingMessageProperties[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name]).Address;
        }

        static Dictionary<string, DateTime> _ipRegulatoryDic = new Dictionary<string, DateTime>();

        private bool IpRegulatory(string type)
        {
            string ip = string.Format("[{0}] {1}", type, this.GetIP());

            if (_ipRegulatoryDic.ContainsKey(ip))
            {
                var t = DateTime.Now - _ipRegulatoryDic[ip];

                if (t.Minutes < 1)
                {
                    //DebugWrite(this, ip + "：遮断しました");

                    return false;
                }
                else
                {
                    _ipRegulatoryDic[ip] = DateTime.Now;
                }
            }
            else
            {
                _ipRegulatoryDic[ip] = DateTime.Now;
            }

            return true;
        }

        public void Store(Key[] item)
        {
            DebugWrite(this, "FileShareService.Store：キー情報を受信しました");

            if (item == null) return;

            Settings.Default._keyController.AddRange(item);
            Settings.Default._routeTable.AddRange(item.Select(n => n.FileLocation).ToArray());
        }

        static Dictionary<string, DateTime> _getRouteTableIpDic = new Dictionary<string, DateTime>();

        public Node[] GetRouteTable(byte[] hash)
        {
            if (!IpRegulatory("GetRouteTable")) return new Node[0];

            DebugWrite(this, "FileShareService.GetRouteTable：ノードリスト検索依頼を受けました");

            if (hash == null) hash = new byte[] { 0 };

            List<Node> nodeList = new List<Node>();
            nodeList.AddRange(Settings.Default._routeTable.Search(hash));
            nodeList.Add(Settings.Default._routeTable.MyNode);

            return nodeList.ToArray();
        }

        public Key[] GetCategoryKey(string category)
        {
            if (!IpRegulatory("GetCategoryKey")) return new Key[0];

#if DEBUG
            DebugWrite(this, "FileShareService.GetCategoryKey：" + category + " カテゴリー検索依頼を受けました");
#else
            DebugWrite(this, "FileShareService.GetCategoryKey：カテゴリー検索依頼を受けました");
#endif

            if (category == null) return null;

            List<Key> list = new List<Key>();

            list.AddRange(Settings.Default._keyController.KeyList.Where(n => n.Cache_Category.Any(m => m.ToLower() == category.ToLower())));

            if (list.Count < 1000)
            {
                foreach (Key k in Settings.Default._keyController.KeyList.Where(n => !n.Cache_Category.Any(m => m.ToLower() == category.ToLower())))
                {
                    if (list.Count >= 1000) break;
                    list.Add(k);
                }
            }

            return list.ToArray();
        }

        public byte[][] GetCacheBlockHashList(byte[] hash)
        {
            Cache cache ;

            lock (Settings.Default._cacheController.CacheList)
            {
                cache = Settings.Default._cacheController.CacheList.
                   FirstOrDefault(n => BinaryEditor.ArrayEquals(n.Hash, hash));
            }

            if (cache != null && cache.CacheBlockHash != null)
            {
                DebugWrite(this, "FileShareService.GetCacheBlockHashList：キャッシュブロックのハッシュリストの検索依頼を受けました");
                return cache.CacheBlockHash;
            }
            // キャッシュブロックハッシュリストが存在しない場合、中継する
            else
            {
                foreach (Key key in Settings.Default._keyController.Search(hash))
                {
                    if (BinaryEditor.ArrayEquals(Settings.Default._routeTable.MyNode.NodeID, key.FileLocation.NodeID)) continue;

                    IFileShareService proxy = null;
                    try
                    {
                        using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", key.FileLocation.Endpoint))
                        {
                            proxy = channel.CreateChannel();

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

                            DebugWrite(this, "FileShareService.GetCacheBlockHashList：キャッシュブロックのハッシュリストの中継依頼を受けました");
                            return cache.CacheBlockHash;
                        }
                    }
                    catch (EndpointNotFoundException ex)
                    {
                        continue;
                    }
                    catch (TimeoutException ex)
                    {
                        continue;
                    }
                    catch (FaultException ex)
                    {
                        continue;
                    }
                    catch (CommunicationException ex)
                    {
                        continue;
                    }
                    catch (NullReferenceException ex)
                    {
                        continue;
                    }
                    catch (ArgumentNullException ex)
                    {
                        continue;
                    }
                    catch (ApplicationException ex)
                    {
                        continue;
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
                    }
                }
            }

            return null;
        }

        public byte[] GetCacheBlock(byte[] hash, int index)
        {
            if (hash == null) return null;

            DebugWrite(this, "FileShareService.GetCacheBlock：キャッシュブロック検索依頼を受けました");

            try
            {
                Cache cache;

                lock (Settings.Default._cacheController.CacheList)
                {
                    cache = Settings.Default._cacheController.CacheList.First(n => BinaryEditor.ArrayEquals(n.Hash, hash));
                }

                return Settings.Default._cacheController[cache, index];
            }
            catch (InvalidOperationException ex)
            {
                DebugWrite(this, "FileShareService.GetCacheBlock：" + ex.Message);
                Debug.WriteLine(ex.Message);
            }
            catch (ApplicationException ex)
            {
                DebugWrite(this, "FileShareService.GetCacheBlock：" + ex.Message);
                Debug.WriteLine(ex.Message);
            }

            DebugWrite(this, "FileShareService.GetCacheBlock：キャッシュブロックの中継依頼を受けました");

            foreach (Key key in Settings.Default._keyController.Search(hash))
            {
                if (BinaryEditor.ArrayEquals(key.FileLocation.NodeID, Settings.Default._routeTable.MyNode.NodeID)) continue;
                if (key.CacheBlockBitmap == null || key.CacheBlockBitmap[index] == false) continue;

                IFileShareService proxy = null;
                try
                {
                    using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", key.FileLocation.Endpoint))
                    {
                        proxy = channel.CreateChannel();

                        Cache cache;

                        lock (Settings.Default._cacheController.CacheList)
                        {
                            cache = Settings.Default._cacheController.CacheList.
                                FirstOrDefault(n => BinaryEditor.ArrayEquals(n.Hash, key.Hash));
                        }

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

                        byte[] block = proxy.GetCacheBlock(hash, index);
                        if (block == null) continue;

                        Settings.Default._cacheController[cache, index] = block;

                        return block;
                    }
                }
                catch (EndpointNotFoundException ex)
                {
                    continue;
                }
                catch (TimeoutException ex)
                {
                    continue;
                }
                catch (FaultException ex)
                {
                    continue;
                }
                catch (CommunicationException ex)
                {
                    continue;
                }
                catch (NullReferenceException ex)
                {
                    continue;
                }
                catch (ArgumentNullException ex)
                {
                    continue;
                }
                catch (ApplicationException ex)
                {
                    continue;
                }
                catch (ArgumentException ex)
                {
                    continue;
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
                }
            }

            return null;
        }

        public void SetCacheBlock(Cache cache, byte[] value, DateTime createTime)
        {
            if (cache == null || value == null || createTime == null) return;

            DebugWrite(this, "FileShareService.SetCacheBlock：キャッシュブロックを受信しました");

            byte[] valueHash = HashFunction.HashCreate(value);

            try
            {
                if (Settings.Default._cacheController.Contains(cache, valueHash)) return;

                Settings.Default._cacheController[cache, valueHash] = value;

                Settings.Default.UploadDiffusionList.Add(new UploadDiffusion()
                {
                    Cache = cache,
                    CacheBlockHash = valueHash,
                    CreateTime = createTime
                });
            }
            catch (ApplicationException ex)
            {
                DebugWrite(this, "FileShareService.SetCacheBlock：" + ex.Message);

                Debug.WriteLine(ex.Message);
            }
        }
    }

    [Serializable]
    public class UploadDiffusion
    {
        public Cache Cache { get; set; }
        public byte[] CacheBlockHash { get; set; }
        public DateTime CreateTime { get; set; }
    }
}