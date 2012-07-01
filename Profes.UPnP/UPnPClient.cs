using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;

namespace Profes.UPnP
{
    public class UPnPClient
    {
        #region GetExternalIPAddress

        /// <summary>
        /// ルータ越しのグローバルIPを取得します
        /// </summary>
        /// <returns></returns>
        public static string GetExternalIPAddress()
        {
            foreach (var nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var firewallIP in nic.GetIPProperties().GatewayAddresses
                    .Select(n => n.Address)
                    .Where(n => n.AddressFamily == AddressFamily.InterNetwork))
                {
                    string value;
                    if (null != (value = GetExternalIPAddress(firewallIP.ToString())))
                        return value;
                }
            }

            return null;
        }

        /// <summary>
        /// ルータ越しのグローバルIPを取得します
        /// </summary>
        /// <param name="machineIP">ユニキャストアドレス</param>
        /// <param name="firewallIP">ゲドウェイアドレス</param>
        /// <returns></returns>
        public static string GetExternalIPAddress(string firewallIP)
        {
            int port = -1;
            string svc = GetServicesFromDeviceOrCache(firewallIP, out port);
            if (port < 0) return null;

            string value = null;

            Thread startThread = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    using (System.Net.WebClient webClient = new WebClient())
                    {
                        if (null != (value = GetExternalIPAddressFromService(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", firewallIP, port)))
                            return;
                        if (null != (value = GetExternalIPAddressFromService(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", firewallIP, port)))
                            return;
                    }
                }
                catch { }
            }));
            startThread.Start();
            startThread.Join(new TimeSpan(0, 0, 30));
            startThread.Abort();

            return value;
        }

        private static string GetExternalIPAddressFromService(string services, string serviceType, string firewallIP, int gatewayPort)
        {
            if (services.Length == 0)
                return null;

            if (!services.Contains(serviceType))
                return null;

            services = services.Substring(services.IndexOf(serviceType));

            string controlUrl = Regex.Match(services, "<controlURL>(.*)</controlURL>").Groups[1].Value;

            string soapBody =
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" +
                " <s:Body>" +
                "  <u:GetExternalIPAddress xmlns:u=\"" + serviceType + "\">" + "</u:GetExternalIPAddress>" +
                " </s:Body>" +
                "</s:Envelope>";

            byte[] body = System.Text.UTF8Encoding.ASCII.GetBytes(soapBody);

            string url = "http://" + firewallIP + ":" + gatewayPort.ToString() + controlUrl;

            try
            {
                System.Net.WebRequest wr = System.Net.WebRequest.Create(url);

                wr.Method = "POST";
                wr.Headers.Add("SOAPAction", "\"" + serviceType + "#GetExternalIPAddress\"");
                wr.ContentType = "text/xml;charset=\"utf-8\"";
                wr.ContentLength = body.Length;

                System.IO.Stream stream = wr.GetRequestStream();

                stream.Write(body, 0, body.Length);
                stream.Flush();
                stream.Close();

                string externalIPAddress = null;

                using (WebResponse wres = wr.GetResponse())
                {
                    if (((HttpWebResponse)wres).StatusCode == HttpStatusCode.OK)
                    {
                        using (StreamReader sr = new StreamReader(wres.GetResponseStream()))
                        {
                            externalIPAddress = Regex.Match(sr.ReadToEnd(), "<NewExternalIPAddress>(.*)</NewExternalIPAddress>").Groups[1].Value;
                        }
                    }
                }

                return externalIPAddress;
            }
            catch { }

            return null;
        }

        #endregion

        #region OpenFirewallPort

        /// <summary>
        /// ルータのポートを開放します
        /// </summary>
        /// <param name="externalPort">外部ポート</param>
        /// <param name="internalPort">内部ポート</param>
        /// <returns>ポート開放に成功した場合、開放したユニキャストアドレスを返します</returns>
        public static IPAddress OpenFirewallPort(int externalPort, int internalPort)
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
                        if (OpenFirewallPort(machineIP.ToString(), firewallIP.ToString(), externalPort, internalPort))
                            return machineIP;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// ルータのポートを開放します
        /// </summary>
        /// <param name="machineIP">ユニキャストアドレス</param>
        /// <param name="firewallIP">ゲドウェイアドレス</param>
        /// <param name="externalPort">外部ポート</param>
        /// <param name="internalPort">内部ポート</param>
        /// <returns></returns>
        public static bool OpenFirewallPort(string machineIP, string firewallIP, int externalPort, int internalPort)
        {
            int port = -1;
            string svc = GetServicesFromDeviceOrCache(firewallIP, out port);
            if (port < 0) return false;

            bool flag = false;

            Thread startThread = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    if (flag = OpenPortFromService(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", machineIP, firewallIP, port, externalPort, internalPort))
                        return;
                    if (flag = OpenPortFromService(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", machineIP, firewallIP, port, externalPort, internalPort))
                        return;
                    //urn:schemas-upnp-org:device:InternetGatewayDevice:1

                }
                catch { }
            }));
            startThread.Start();
            startThread.Join(new TimeSpan(0, 0, 30));
            startThread.Abort();

            return flag;
        }

        private static bool OpenPortFromService(string services, string serviceType, string machineIP, string firewallIP, int gatewayPort, int externalPort, int internalPort)
        {
            if (services.Length == 0)
                return false;

            if (!services.Contains(serviceType))
                return false;

            services = services.Substring(services.IndexOf(serviceType));

            string controlUrl = Regex.Match(services, "<controlURL>(.*)</controlURL>").Groups[1].Value;

            string soapBody =
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ \" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/ \">" +
                " <s:Body>" +
                "  <u:AddPortMapping xmlns:u=\"" + serviceType + "\">" +
                "   <NewRemoteHost></NewRemoteHost>" +
                "   <NewExternalPort>" + externalPort + "</NewExternalPort>" +
                "   <NewProtocol>TCP</NewProtocol>" +
                "   <NewInternalPort>" + internalPort + "</NewInternalPort>" +
                "   <NewInternalClient>" + machineIP + "</NewInternalClient>" +
                "   <NewEnabled>1</NewEnabled>" +
                "   <NewPortMappingDescription>Profes</NewPortMappingDescription>" +
                "   <NewLeaseDuration>0</NewLeaseDuration>" +
                "  </u:AddPortMapping>" +
                " </s:Body>" +
                "</s:Envelope>";

            byte[] body = System.Text.UTF8Encoding.ASCII.GetBytes(soapBody);

            string url = "http://" + firewallIP + ":" + gatewayPort.ToString() + controlUrl;

            try
            {
                System.Net.WebRequest wr = System.Net.WebRequest.Create(url);

                wr.Method = "POST";
                wr.Headers.Add("SOAPAction", "\"" + serviceType + "#AddPortMapping\"");
                wr.ContentType = "text/xml;charset=\"utf-8\"";
                wr.ContentLength = body.Length;

                System.IO.Stream stream = wr.GetRequestStream();

                stream.Write(body, 0, body.Length);
                stream.Flush();
                stream.Close();

                using (WebResponse wres = wr.GetResponse())
                {
                    if (((HttpWebResponse)wres).StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        #endregion

        #region CloseFirewallPort

        /// <summary>
        /// ルータのポートを封鎖します
        /// </summary>
        /// <param name="externalPort">外部ポート</param>
        /// <returns>ポート封鎖に成功した場合、封鎖したユニキャストアドレスを返します</returns>
        public static IPAddress CloseFirewallPort(int externalPort)
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
                        if (CloseFirewallPort(machineIP.ToString(), firewallIP.ToString(), externalPort))
                            return machineIP;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// ルータのポートを封鎖します
        /// </summary>
        /// <param name="machineIP">ユニキャストアドレス</param>
        /// <param name="firewallIP">ゲドウェイアドレス</param>
        /// <param name="externalPort">外部ポート</param>
        /// <returns></returns>
        public static bool CloseFirewallPort(string machineIP, string firewallIP, int externalPort)
        {
            int port = -1;
            string svc = GetServicesFromDeviceOrCache(firewallIP, out port);
            if (port < 0) return false;

            bool flag = false;

            Thread startThread = new Thread(new ThreadStart(delegate()
            {
                try
                {
                    if (flag = ClosePortFromService(svc, "urn:schemas-upnp-org:service:WANIPConnection:1", firewallIP, port, externalPort))
                        return;
                    if (flag = ClosePortFromService(svc, "urn:schemas-upnp-org:service:WANPPPConnection:1", firewallIP, port, externalPort))
                        return;
                }
                catch { }
            }));
            startThread.Start();
            startThread.Join(new TimeSpan(0, 0, 30));
            startThread.Abort();

            return flag;
        }

        private static bool ClosePortFromService(string services, string serviceType, string firewallIP, int gatewayPort, int externalPort)
        {
            if (services.Length == 0)
                return false;

            if (!services.Contains(serviceType))
                return false;

            services = services.Substring(services.IndexOf(serviceType));

            string controlUrl = Regex.Match(services, "<controlURL>(.*)</controlURL>").Groups[1].Value;

            string soapBody =
                "<s:Envelope xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/ \" s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/ \">" +
                "<s:Body>" +
                "<u:DeletePortMapping xmlns:u=\"" + serviceType + "\">" +
                "<NewRemoteHost></NewRemoteHost>" +
                "<NewExternalPort>" + externalPort + "</NewExternalPort>" +
                "<NewProtocol>TCP</NewProtocol>" +
                "</u:DeletePortMapping>" +
                "</s:Body>" +
                "</s:Envelope>";

            byte[] body = System.Text.UTF8Encoding.ASCII.GetBytes(soapBody);

            string url = "http://" + firewallIP + ":" + gatewayPort.ToString() + controlUrl;

            try
            {
                System.Net.WebRequest wr = System.Net.WebRequest.Create(url);

                wr.Method = "POST";
                wr.Headers.Add("SOAPAction", "\"" + serviceType + "#DeletePortMapping\"");
                wr.ContentType = "text/xml;charset=\"utf-8\"";
                wr.ContentLength = body.Length;

                System.IO.Stream stream = wr.GetRequestStream();

                stream.Write(body, 0, body.Length);
                stream.Flush();
                stream.Close();

                using (WebResponse wres = wr.GetResponse())
                {
                    if (((HttpWebResponse)wres).StatusCode == HttpStatusCode.OK)
                    {
                        return true;
                    }
                }
            }
            catch { }

            return false;
        }

        #endregion

        private static string _services = "";
        private static int _resPort;

        private static string GetServicesFromDeviceOrCache(string firewallIP, out int resPort)
        {
            if (_services == "")
            {
                _services = GetServicesFromDevice("239.255.255.250", out _resPort);

                if (_services == "")
                {
                    // 239.255.255.250ではなくfirewallIPだとつながる場合がまれにある
                    // 繋がっていないネットワークアダプタが原因？初期起動時は問題ない？
                    _services = GetServicesFromDevice(firewallIP, out _resPort);
                }
            }

            resPort = _resPort;
            return _services;
        }

        private static string GetServicesFromDevice(string firewallIP, out int resPort)
        {
            string queryResponse = "";
            resPort = -1;

            try
            {
                string query = "M-SEARCH * HTTP/1.1\r\n" +
                    "Host:" + firewallIP + ":1900\r\n" +
                    "ST:upnp:rootdevice\r\n" +
                    "Man:\"ssdp:discover\"\r\n" +
                    "MX:3\r\n" +
                    "\r\n" +
                    "\r\n";

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(firewallIP), 1900);

                client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 30000);
                byte[] q = Encoding.ASCII.GetBytes(query);

                client.SendTo(q, q.Length, SocketFlags.None, endPoint);

                IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
                EndPoint senderEP = (EndPoint)sender;
                byte[] data = new byte[1024];
                int recv = client.ReceiveFrom(data, ref senderEP);

                queryResponse = Encoding.ASCII.GetString(data);
            }
            catch { }

            if (queryResponse.Length == 0)
                return "";

            string location = Regex.Match(queryResponse.ToLower(), "^location.*?:(.*)", RegexOptions.Multiline).Groups[1].Value;

            if (location.Length == 0)
                return "";

            Uri locationPort = new Uri(location);
            resPort = locationPort.Port;

            string downloadString = "";

            try
            {
                using (var webClient = new WebClient())
                {
                    Thread startThread = new Thread(new ThreadStart(delegate()
                    {
                        try
                        {
                            downloadString = webClient.DownloadString(location);
                        }
                        catch { }
                    }));

                    startThread.Start();
                    startThread.Join(new TimeSpan(0, 0, 30));
                    startThread.Abort();
                }
            }
            catch
            {
                return "";
            }

            return downloadString;
        }

        /// <summary>
        /// サービスを初期化する
        /// </summary>
        static public void Clear()
        {
            _services = "";
            _resPort = -1;
        }
    }
}