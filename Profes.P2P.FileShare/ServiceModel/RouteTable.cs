using System.Collections.Generic;
using System;
using System.Text;
using Profes.Security.Cryptography;
using System.Collections;
using System.Runtime.Serialization;
using System.Linq;
using System.Diagnostics;
using Profes.BinaryEditorBase;
using System.Windows;
using System.Threading;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// RouteTable の Ping イベントを処理するメソッドを表します。
    /// </summary>
    public delegate bool RouteTablePingEventHandler(object sender, Node e);

    /// <summary>
    /// ノード検索のためのメソッドを提供します
    /// </summary>
    [Serializable]
    class RouteTable : ISerializable
    {
        private const int MAX_NODELIST_LENGTH = 32;

        private Node[][] _nodeList = new Node[256 + 1][];
        private Node _myNode;

        /// <summary>
        /// ノードの生存チェック時に発生します
        /// </summary>
        public event RouteTablePingEventHandler Ping;

        #region メソッド

        /// <summary>
        /// RouteTableクラスの新しいインスタンスを初期化します
        /// </summary>
        public RouteTable()
        {
            TimerEvent();
        }

        /// <summary>
        /// RouteTableクラスの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="myNode">自分自身のノード情報を指定する</param>
        public RouteTable(Node myNode)
        {
            this.MyNode = myNode;

            TimerEvent();
        }

        protected RouteTable(SerializationInfo info, StreamingContext context)
        {
            _nodeList = (Node[][])info.GetValue("_nodeList", typeof(Node[][]));
            _myNode = (Node)info.GetValue("MyNode", typeof(Node));

            TimerEvent();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("_nodeList", _nodeList, typeof(Node[][]));
            info.AddValue("MyNode", _myNode, typeof(Node));
        }

        Timer timer;

        void TimerEvent()
        {
            timer = new Timer(new TimerCallback(_pingTimer_Clock), null, 0, 60 * 1000);
        }

        List<Node> _pingNodeList = new List<Node>();
        private Dictionary<string, DateTime> _deadNodeDic = new Dictionary<string, DateTime>();

        private void _pingTimer_Clock(object sender)
        {
            try
            {
                Node[] list;

                lock (this)
                {
                    list = _pingNodeList.ToArray();
                    _pingNodeList.Clear();
                }

                foreach (Node node in list)
                {
                    int i = Xor(MyNode.NodeID, node.NodeID);

                    if (Ping != null && false == Ping(this, node))
                    {
                        lock (this)
                        {
                            _deadNodeDic[BinaryEditor.BytesToHexString(node.NodeID)] = DateTime.Now;

                            if (_nodeList[i] != null)
                            {
                                int index = 0;
                                while (_nodeList[i].Count(n => n != null) > index &&
                                    BinaryEditor.ArrayEquals(_nodeList[i][index].NodeID, node.NodeID)) index++;

                                if (_nodeList[i].Count(n => n != null) > index)
                                {
                                    _nodeList[i][index] = null;
                                    _nodeList[i] = _nodeList[i].OrderBy(n => n == null).ToArray();
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private bool DeadNodeCheck(Node node)
        {
            string nodeIdString = BinaryEditor.BytesToHexString(node.NodeID);

            if (_deadNodeDic.ContainsKey(nodeIdString))
            {
                var t = DateTime.Now - _deadNodeDic[nodeIdString];

                if (t.Hours < 1)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 死亡ノードを追加する
        /// </summary>
        /// <param name="node"></param>
        public void AddDeadNode(Node node)
        {
            _deadNodeDic[BinaryEditor.BytesToHexString(node.NodeID)] = DateTime.Now;
        }

        /// <summary>
        /// ノードを追加する
        /// </summary>
        /// <param name="node">追加するノード</param>
        public void Add(Node node)
        {
            if (node == null) return;
            if (!Verification.VerificationIPAddress(node.Endpoint)) return;
            if (DeadNodeCheck(node)) return;
            if (_pingNodeList.Any(n => BinaryEditor.ArrayEquals(n.NodeID, node.NodeID))) return;

            int i = Xor(MyNode.NodeID, node.NodeID);

            if (i == 0) return;

            lock (this)
            {
                // 追加するnodeがNodeListに入っている場合、そのノードをNodeListの末尾に移す
                if (_nodeList[i] != null && true == _nodeList[i].Any(n => n != null && BinaryEditor.ArrayEquals(n.NodeID, node.NodeID)))
                {
                    _nodeList[i] = _nodeList[i].OrderBy(n => n == null || BinaryEditor.ArrayEquals(n.NodeID, node.NodeID)).ToArray();
                }
                // 追加するnodeがNodeListの中に存在しない場合
                else if (_nodeList[i] != null)
                {
                    if (_nodeList[i].Count(n => n == null) == 0)
                    {
                        Node checkNode = _nodeList[i].FirstOrDefault(n => n != null && !_pingNodeList.Any(m => BinaryEditor.ArrayEquals(n.NodeID, m.NodeID)));
                        if (checkNode != null) _pingNodeList.Add(checkNode);
                    }

                    if (_nodeList[i].Count(n => n != null) < MAX_NODELIST_LENGTH)
                    {
                        _nodeList[i][_nodeList[i].Count(n => n != null)] = node;
                        _nodeList[i] = _nodeList[i].OrderBy(n => n == null || BinaryEditor.ArrayEquals(n.NodeID, node.NodeID)).ToArray();
                    }
                }
                else
                {
                    _nodeList[i] = new Node[MAX_NODELIST_LENGTH];
                    _nodeList[i][0] = node;
                }
            }
        }

        /// <summary>
        /// ノードリストを追加する
        /// </summary>
        /// <param name="Nodes">追加するノードリスト</param>
        public void AddRange(Node[] Nodes)
        {
            foreach (Node node in Nodes)
            {
                this.Add(node);
            }
        }

        /// <summary>
        /// ノードを検索する
        /// </summary>
        /// <param name="key">検索するノードID</param>
        /// <returns>距離の近いノードを返す</returns>
        public Node[] Search(byte[] key)
        {
            Node[][] tempNodeList = new Node[256 + 1][];
            List<Node> tempOneList = new List<Node>();

            lock (this)
            {
                for (int i = 0; i < _nodeList.Length; i++)
                {
                    for (int j = 0; _nodeList[i] != null && j < _nodeList[i].Length && _nodeList[i][j] != null; j++)
                    {
                        int index = Xor(key, _nodeList[i][j].NodeID);

                        if (tempNodeList[index] == null)
                        {
                            tempNodeList[index] = new Node[MAX_NODELIST_LENGTH];
                        }
                        if (tempNodeList[index].Count(n => n != null) < MAX_NODELIST_LENGTH)
                        {
                            tempNodeList[index][tempNodeList[index].Count(n => n != null)] = _nodeList[i][j];
                        }
                    }
                }
            }

            for (int i = 0; i < tempNodeList.Length; i++)
            {
                for (int j = 0; tempNodeList[i] != null && j < tempNodeList[i].Length && tempNodeList[i][j] != null; j++)
                {
                    if (tempOneList.Count <= 100) tempOneList.Add(tempNodeList[i][j]);
                }
            }

            return tempOneList.ToArray();
        }

        /// <summary>
        /// ノードを検索する
        /// </summary>
        /// <param name="key">検索するカテゴリ</param>
        /// <returns>距離の近いノードを返す</returns>
        public Node[] Search(string key)
        {
            return Search(HashFunction.HashCreate(key.ToLower()));
        }

        /// <summary>
        /// ランダムにノードを取得する
        /// </summary>
        /// <returns>ノードを返す</returns>
        public Node Random()
        {
            List<Node> tempNodeList = new List<Node>();

            lock (this)
            {
                for (int i = 0; i < _nodeList.Length; i++)
                {
                    for (int j = 0; _nodeList[i] != null && j < _nodeList[i].Length && _nodeList[i][j] != null; j++)
                    {
                        tempNodeList.Add(_nodeList[i][j]);
                    }
                }
            }

            if (tempNodeList.Count == 0) return null;
            return tempNodeList[(new Random()).Next(tempNodeList.Count)];
        }

        /// <summary>
        /// ノードを削除する
        /// </summary>
        /// <param name="node"></param>
        public void Remove(Node node)
        {
            lock (this)
            {
                int i = Xor(this.MyNode.NodeID, node.NodeID);

                if (_nodeList[i] != null)
                {
                    int index = 0;
                    while (_nodeList[i].Count(n => n != null) > index &&
                        BinaryEditor.ArrayEquals(_nodeList[i][index].NodeID, node.NodeID)) index++;

                    if (_nodeList[i].Count(n => n != null) > index)
                    {
                        _nodeList[i][index] = null;
                        _nodeList[i] = _nodeList[i].OrderBy(n => n == null).ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// 距離を算出する
        /// </summary>
        private int Xor(byte[] x, byte[] y)
        {
            StringBuilder str = new StringBuilder();
            int len = Math.Min(x.Length, y.Length);

            for (int i = 0; i < len; i++) str.Append(Convert.ToString(x[i] ^ y[i], 2).PadLeft(8, '0'));

            return str.ToString().TrimStart('0').Length;
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// 自分のノード情報を取得または設定する
        /// </summary>
        public Node MyNode
        {
            get { return _myNode; }
            set
            {
                if (_myNode == value) return;
                _myNode = value;

                Node[][] tempNodeList = new Node[256 + 1][];

                lock (this)
                {
                    for (int i = 0; i < _nodeList.Length; i++)
                    {
                        for (int j = 0; _nodeList[i] != null && j < _nodeList[i].Length && _nodeList[i][j] != null; j++)
                        {
                            if (!Verification.VerificationIPAddress(_nodeList[i][j].Endpoint)) continue;

                            int index = Xor(_myNode.NodeID, _nodeList[i][j].NodeID);

                            if (index == 0) continue;

                            if (tempNodeList[index] == null)
                            {
                                tempNodeList[index] = new Node[MAX_NODELIST_LENGTH];
                            }
                            if (tempNodeList[index].Count(n => n != null) < MAX_NODELIST_LENGTH)
                            {
                                tempNodeList[index][tempNodeList[index].Count(n => n != null)] = _nodeList[i][j];
                            }
                        }
                    }
                }

                _nodeList = tempNodeList;
            }
        }

        /// <summary>
        /// ノード数を取得します
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;

                lock (this)
                {
                    for (int i = 0; i < _nodeList.Length; i++)
                    {
                        for (int j = 0; _nodeList[i] != null && j < _nodeList[i].Length && _nodeList[i][j] != null; j++)
                        {
                            count++;
                        }
                    }
                }

                return count;
            }
        }
        #endregion
    }
}