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
using Profes.Security.Cryptography;

namespace Profes.P2P.FileShare.ServiceModel.Client
{
    class QueryController : IDisposable
    {
        private Timer _queryTimer;

        static public event FileShareServiceDebugWriteEventHandler DebugWrite;
        static public event FileShareClientNodeShowEventHandler NodeShowAdd;
        static public event FileShareClientNodeShowEventHandler NodeShowRemove;

        private int _queryTimerCount = 0;
        private List<byte[]> _queryNodeHash = new List<byte[]>();

        public QueryController()
        {
            _queryTimer = new Timer(new TimerCallback(_queryTimer_Clock), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _queryTimer.Change(0, 2 * 1000);
        }

        public void Stop()
        {
            _queryTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region クエリ

        private void _queryTimer_Clock(object sender)
        {
            Settings.Default._keyController.AddRange(Settings.Default._cacheController.CacheList.ToArray(), Settings.Default._routeTable.MyNode);

            if (_queryTimerCount >= Settings.Default.QueryTimerMaxCount) return;
            _queryTimerCount++;

            try
            {
                foreach (string ss in Settings.Default.QueryList)
                {
                    foreach (Node node in Settings.Default._routeTable.Search(ss))
                    {
                        if (_queryTimerCount > Settings.Default.QueryTimerMaxCount) break;

                        lock (_queryNodeHash)
                        {
                            if (_queryNodeHash.Any(n => BinaryEditor.ArrayEquals(n, node.NodeID))) continue;
                            _queryNodeHash.Add(node.NodeID);
                        }

                        Settings.Default._keyController.AddRange(QueryCommunication(node, ss));

                        lock (_queryNodeHash)
                        {
                            _queryNodeHash.RemoveAll(n => BinaryEditor.ArrayEquals(n, node.NodeID));
                        }
                    }
                }
            }
            catch (System.NullReferenceException) { }
            finally
            {
                _queryTimerCount--;
            }
        }

        private Key[] QueryCommunication(Node node, string query)
        {
            List<Key> queryList = new List<Key>();

            NodeListViewItem nlvi = null;

            IFileShareService proxy = null;
            try
            {
                nlvi = new NodeListViewItem()
                {
                    CommunicationType = "Query",
                    Description = "キャッシュカテゴリリストとノードリストを受信しています",
                    Node = node,
                };
                NodeShowAdd(this, nlvi);

                using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", node.Endpoint))
                {
                    proxy = channel.CreateChannel();

                    queryList.AddRange(proxy.GetCategoryKey(query));
                    DebugWrite(this, "QueryCommunication成功：クエリの受信に成功しました");

                    Settings.Default._routeTable.AddRange(proxy.GetRouteTable(HashFunction.HashCreate(query)));
                    DebugWrite(this, "QueryCommunication成功：ノードリストの受信に成功しました");
                }
            }
            catch (EndpointNotFoundException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
            }
            catch (TimeoutException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
            }
            catch (FaultException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
            }
            catch (CommunicationException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
            }
            catch (ArgumentNullException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
            }
            catch (NullReferenceException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
            }
            catch (ArgumentException ex)
            {
                Debug.WriteLine("QueryCommunication" + ex.Message);
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

            return queryList.ToArray();
        }

        #endregion

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。QueryController によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this._queryTimer.Dispose();
        }

        #endregion
    }
}