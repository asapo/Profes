using System.Runtime.Serialization;
using System.ServiceModel.Description;
using System.Text;
using Profes.Security.Cryptography;
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Net;
using System.Security.Cryptography;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// ノードに関する情報を表します
    /// </summary>
    [Serializable]
    [DataContract]
    public class Node
    {
        public Node() { }

        [DataMember]
        private Uri _endpoint;

        /// <summary>
        /// Uriを取得または設定します
        /// </summary>
        public EndpointAddress Endpoint
        {
            get
            {
                if (_endpoint == null) return new EndpointAddress("net.tcp://127.0.0.1:80/FileShareService");
                else return new EndpointAddress(_endpoint);
            }
            set
            {
                if (value.Uri.DnsSafeHost != "0.0.0.0")
                {
                    _endpoint = value.Uri;
                    NatNodeID = null;
                }
                else
                {
                    _endpoint = value.Uri;

                    byte[] random = new byte[32];
                    RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                    rng.GetBytes(random);
                    NatNodeID = random;
                }
            }
        }

        /// <summary>
        /// NAT用のノードIDを取得または設定します
        /// </summary>
        [field:OptionalField(VersionAdded = 2)]
        private byte[] NatNodeID { get; set; }

        /// <summary>
        /// ノードIDを取得します
        /// </summary>
        public byte[] NodeID
        {
            get
            {
                if (NatNodeID == null)
                {
                    return HashFunction.HashCreate(Encoding.Unicode.GetBytes(Endpoint.Uri.ToString()));
                }
                else
                {
                    return NatNodeID;
                }
            }
        }
    }
}