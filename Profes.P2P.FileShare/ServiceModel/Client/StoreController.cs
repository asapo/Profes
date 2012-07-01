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
    class StoreController : IDisposable
    {
        private Timer _storeTimer;

        static public event FileShareServiceDebugWriteEventHandler DebugWrite;
        static public event FileShareClientNodeShowEventHandler NodeShowAdd;
        static public event FileShareClientNodeShowEventHandler NodeShowRemove;

        private int _storeTimerCount = 0;
        private List<byte[]> _storeNodeHash = new List<byte[]>();

        public StoreController()
        {
            _storeTimer = new Timer(new TimerCallback(_storeTimer_Clock), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _storeTimer.Change(0, 2 * 1000);
        }

        public void Stop()
        {
            _storeTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region ストア

        private void _storeTimer_Clock(object sender)
        {
            if (_storeTimerCount >= Settings.Default.StoreTimerMaxCount) return;
            _storeTimerCount++;

            Dictionary<string, List<Key>> byCategory = new Dictionary<string, List<Key>>();

            foreach (Key item in Settings.Default._keyController.KeyList)
            {
                foreach (string category in item.Cache_Category.Where(n => Settings.Default.QueryList.Any(m => m == Cache.CategoryRegularization(n))).Select(n => Cache.CategoryRegularization(n)))
                {
                    if (category.Trim() == "") continue;
                    if (!byCategory.ContainsKey(category)) byCategory[category] = new List<Key>();
                    byCategory[category].Add(item);
                }
            }

            foreach (string category in byCategory.Keys)
            {
                var nodeList = Settings.Default._routeTable.Search(category);
                int storeConut = 0;

                for (int i = 0; i < nodeList.Length && storeConut < 2; i++)
                {
                    if (_storeTimerCount > Settings.Default.StoreTimerMaxCount) break;

                    lock (_storeNodeHash)
                    {
                        if (_storeNodeHash.Any(n => BinaryEditor.ArrayEquals(n, nodeList[i].NodeID))) continue;
                        _storeNodeHash.Add(nodeList[i].NodeID);
                    }

                    if (StoreCommunication(nodeList[i], byCategory[category].ToArray())) storeConut++;

                    lock (_storeNodeHash)
                    {
                        _storeNodeHash.RemoveAll(n => BinaryEditor.ArrayEquals(n, nodeList[i].NodeID));
                    }
                }
            }

            _storeTimerCount--;
        }

        private bool StoreCommunication(Node node, Key[] keys)
        {
            NodeListViewItem nlvi = null;

            IFileShareService proxy = null;
            try
            {
                nlvi = new NodeListViewItem()
                {
                    CommunicationType = "Store",
                    Description = "キー情報を送信しています",
                    Node = node,
                };

                NodeShowAdd(this, nlvi);

                using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", node.Endpoint))
                {
                    proxy = channel.CreateChannel();

                    proxy.Store(keys);

                    DebugWrite(this, "StoreCommunication成功：Keyリストの送信に成功しました");
                    return true;
                }
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine("StoreCommunication" + ex.Message);
                return false;
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine("StoreCommunication" + ex.Message);
                return false;
            }
            catch (FaultException ex)
            {
                Debug.WriteLine("StoreCommunication" + ex.Message);
                return false;
            }
            catch (CommunicationException ex)
            {
                if (Settings.Default._routeTable.Count > 30)
                {
                    Settings.Default._routeTable.AddDeadNode(node);
                    Settings.Default._routeTable.Remove(node);
                }

                Debug.WriteLine("StoreCommunication" + ex.Message);
                return false;
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("StoreCommunication" + ex.Message);
                return false;
            }
            catch (ArgumentNullException ex)
            {
                Debug.WriteLine("StoreCommunication" + ex.Message);
                return false;
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("StoreCommunication" + ex.Message);
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
        /// インフラストラクチャ。StoreController によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this._storeTimer.Dispose();
        }

        #endregion
    }
}