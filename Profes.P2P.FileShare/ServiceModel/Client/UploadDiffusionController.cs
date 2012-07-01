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
    class UploadDiffusionController : IDisposable
    {
        private Timer _uploadDiffusion;

        static public event FileShareServiceDebugWriteEventHandler DebugWrite;
        static public event FileShareClientNodeShowEventHandler NodeShowAdd;
        static public event FileShareClientNodeShowEventHandler NodeShowRemove;

        private int _uploadDiffusionTimer_Count;

        public UploadDiffusionController()
        {
            _uploadDiffusion = new Timer(new TimerCallback(_uploadDiffusionTimer_Clock), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _uploadDiffusion.Change(0, 2 * 1000);
        }

        public void Stop()
        {
            _uploadDiffusion.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #region 拡散アップロード

        private void _uploadDiffusionTimer_Clock(object sender)
        {
            if (Settings.Default.UploadDiffusionList.Count == 0) return;
            if (_uploadDiffusionTimer_Count > 0) return;
            _uploadDiffusionTimer_Count++;

            UploadDiffusion ud = Settings.Default.UploadDiffusionList[0];
            byte[] value = null;

            IFileShareService proxy = null;
            try
            {
                value = Settings.Default._cacheController[ud.Cache, ud.CacheBlockHash];

                int i = 3;

                while (i > 0 && (DateTime.Now - ud.CreateTime).Minutes < 30 && (DateTime.Now - ud.CreateTime).Minutes >= 0)
                {
                    NodeListViewItem nlvi = null;

                    try
                    {
                        Node node = Settings.Default._routeTable.Random();
                        nlvi = new NodeListViewItem()
                        {
                            CommunicationType = "uploadDiffusion",
                            Description = "キャッシュブロックを送信しています",
                            Node = node,
                        };
                        NodeShowAdd(this, nlvi);

                        using (ChannelFactory<IFileShareService> channel = new ChannelFactory<IFileShareService>("Tcp_Client", node.Endpoint))
                        {
                            proxy = channel.CreateChannel();

                            proxy.SetCacheBlock(ud.Cache, value, ud.CreateTime);
                            DebugWrite(this, "_uploadDiffusionTimer_Clock：拡散アップロードに成功しました");

                            i--;
                        }
                    }
                    catch (EndpointNotFoundException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (TimeoutException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (FaultException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (CommunicationException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (NullReferenceException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (ArgumentNullException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (ArgumentException ex)
                    {
                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    catch (ApplicationException ex)
                    {
                        DebugWrite(this, "_uploadDiffusionTimer_Clock：" + ex.Message);

                        Debug.WriteLine("_uploadDiffusionTimer_Clock" + ex.Message);
                    }
                    finally
                    {
                        if (nlvi != null)
                            NodeShowRemove(this, nlvi);
                    }
                }
            }
            catch (ApplicationException ex)
            {
                DebugWrite(this, "_uploadDiffusionTimer_Clock：" + ex.Message);
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

                Settings.Default.UploadDiffusionList.RemoveAt(0);
                _uploadDiffusionTimer_Count--;
            }
        }

        #endregion

        #region IDisposable メンバ

        /// <summary>
        /// インフラストラクチャ。UploadDiffusionController によって使用されているすべてのリソースを解放します。
        /// </summary>
        public void Dispose()
        {
            this._uploadDiffusion.Dispose();
        }

        #endregion
    }
}