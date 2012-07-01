using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using Profes.BinaryEditorBase;
using System.Net;

namespace Profes.P2P.FileShare.ServiceModel
{
    static class Verification
    {
        public static bool VerificationIPAddress(EndpointAddress end)
        {
            try
            {
                switch (end.Uri.HostNameType)
                {
                    case UriHostNameType.IPv4:
                        return VerificationIPAddress(end.Uri.DnsSafeHost);
                    case UriHostNameType.IPv6:
                        return VerificationIPAddress(end.Uri.DnsSafeHost);
                    case UriHostNameType.Dns:
                        return true;
                    default:
                        return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool VerificationIPAddress(string ip)
        {
            try
            {
                if (ip == null || ip.Trim().Length == 0) return false;
                return VerificationIPAddress(IPAddress.Parse(ip));
            }
            catch
            {
                return false;
            }
        }

        public static bool VerificationIPAddress(IPAddress ip)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return VerificationIPAddressV4(ip.ToString());
            }
            else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (ip.IsIPv6LinkLocal == true || ip.IsIPv6Multicast == true || ip.IsIPv6SiteLocal == true)
                    return false;
                else
                    return VerificationIPAddressV6(ip.ToString());
            }
          
            return false;
        }
        
        private static bool VerificationIPAddressV6(string ip)
        {
            if (ip == "0000:0000:0000:0000:0000:0000:0000:0000")
            {
                return false;
            }
            if (ip == "0000:0000:0000:0000:0000:0000:0000:0001")
            {
                return false;
            }

            return true;
        }

        private static bool VerificationIPAddressV4(string ip)
        {
            if (ip == "0.0.0.0") return false;

            byte[] privateAddress_10_0_0_0 = IPtoByteArray("10.0.0.0");
            byte[] privateAddress_10_255_255_255 = IPtoByteArray("10.255.255.255");
            byte[] privateAddress_172_16_0_0 = IPtoByteArray("172.16.0.0");
            byte[] privateAddress_172_31_255_255 = IPtoByteArray("172.31.255.255");
            byte[] privateAddress_192_168_0_0 = IPtoByteArray("192.168.0.0");
            byte[] privateAddress_192_168_255_255 = IPtoByteArray("192.168.255.255");
            byte[] privateAddress_127_0_0_0 = IPtoByteArray("127.0.0.0");
            byte[] privateAddress_127_255_255_255 = IPtoByteArray("127.255.255.255");

            byte[] orig = IPtoByteArray(ip);

            if (BinaryEditor.ByteArraryCompare(privateAddress_10_0_0_0, orig) != 1 &&
                BinaryEditor.ByteArraryCompare(privateAddress_10_255_255_255, orig) != -1)
            {
                return false;
            }
            if (BinaryEditor.ByteArraryCompare(privateAddress_172_16_0_0, orig) != 1 &&
                BinaryEditor.ByteArraryCompare(privateAddress_172_31_255_255, orig) != -1)
            {
                return false;
            }
            if (BinaryEditor.ByteArraryCompare(privateAddress_192_168_0_0, orig) != 1 &&
                BinaryEditor.ByteArraryCompare(privateAddress_192_168_255_255, orig) != -1)
            {
                return false;
            }
            if (BinaryEditor.ByteArraryCompare(privateAddress_127_0_0_0, orig) != 1 &&
                BinaryEditor.ByteArraryCompare(privateAddress_127_255_255_255, orig) != -1)
            {
                return false;
            }

            return true;
        }

        private static byte[] IPtoByteArray(string ip)
        {
            List<byte> blist = new List<byte>();

            foreach (string ss in ip.Split(new string[] { "." }, StringSplitOptions.RemoveEmptyEntries))
            {
                blist.Add(byte.Parse(ss));
            }

            return blist.ToArray();
        }
    }
}