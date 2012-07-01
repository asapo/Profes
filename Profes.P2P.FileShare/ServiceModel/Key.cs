using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Profes.P2P.FileShare.FileShareControl;
using System.ComponentModel;
using System.Runtime.Serialization;
using Profes.BinaryEditorBase;
using Profes.Security.Cryptography;
using Profes.P2P.FileShare.Properties;
using System.Timers;
using System.Net;
using System.Windows;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// KeyController の AddedNew イベントを処理するメソッドを表します。
    /// </summary>
    public delegate void KeyControllerAddedNewEventHandler(object sender, Key e);

    /// <summary>
    /// Key制御のためのメソッドを提供します
    /// </summary>
    [Serializable]
    public class KeyController : ISerializable
    {
        Dictionary<string, Key> _keyDic = new Dictionary<string, Key>();
        Dictionary<string, CacheReview> _myReviewDic = new Dictionary<string, CacheReview>();
        Dictionary<string, List<CacheReview>> _reviewDic = new Dictionary<string, List<CacheReview>>();

        Timer timer;

        public event KeyControllerAddedNewEventHandler AddedNew;

        public KeyController()
        {
            TimerEvent();
        }

        protected KeyController(SerializationInfo info, StreamingContext context)
        {
            lock (_myReviewDic)
            {
                var _myReviewDic_KeyList = (string[])info.GetValue("_myReviewDic_Keys", typeof(string[]));
                var _myReviewDic_ValueList = (CacheReview[])info.GetValue("_myReviewDic_Values", typeof(CacheReview[]));

                for (int i = 0; i < _myReviewDic_KeyList.Length; i++)
                {
                    _myReviewDic.Add(_myReviewDic_KeyList[i], _myReviewDic_ValueList[i]);
                }
            }

            lock (_reviewDic)
            {
                var _reviewDic_KeyList = (string[])info.GetValue("_reviewDic_Keys", typeof(string[]));
                var _reviewDic_ValueList = (CacheReview[][])info.GetValue("_reviewDic_Values", typeof(CacheReview[][]));

                for (int i = 0; i < _reviewDic_KeyList.Length; i++)
                {
                    var list = new List<CacheReview>();
                    list.AddRange(_reviewDic_ValueList[i]);

                    _reviewDic.Add(_reviewDic_KeyList[i], list);
                }
            }

            TimerEvent();
        }

        new public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            lock (_myReviewDic)
            {
                var _myReviewDic_KeyList = new string[_myReviewDic.Count];
                var _myReviewDic_ValueList = new CacheReview[_myReviewDic.Count];

                int i = 0;
                foreach (string key in _myReviewDic.Keys)
                {
                    _myReviewDic_KeyList[i] = key;
                    _myReviewDic_ValueList[i] = _myReviewDic[key];
                    i++;
                }

                info.AddValue("_myReviewDic_Keys", _myReviewDic_KeyList.ToArray(), typeof(string[]));
                info.AddValue("_myReviewDic_Values", _myReviewDic_ValueList.ToArray(), typeof(CacheReview[]));
            }

            lock (_reviewDic)
            {
                var _reviewDic_KeyList = new string[_reviewDic.Count];
                var _reviewDic_ValueList = new CacheReview[_reviewDic.Count][];

                int i = 0;
                foreach (string key in _reviewDic.Keys)
                {
                    _reviewDic_KeyList[i] = key;
                    _reviewDic_ValueList[i] = _reviewDic[key].ToArray();
                    i++;
                }

                info.AddValue("_reviewDic_Keys", _reviewDic_KeyList.ToArray(), typeof(string[]));
                info.AddValue("_reviewDic_Values", _reviewDic_ValueList.ToArray(), typeof(CacheReview[]));
            }
        }

        void TimerEvent()
        {
            timer = new Timer();
            timer.Elapsed += new ElapsedEventHandler(timer_Elapsed);
            timer.Interval = 1000 * 60;
            timer.Start();
        }

        void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            lock (_keyDic)
            {
                if (_keyDic == null || _keyDic.Count == 0) return;

                var tempKeyDic = new Dictionary<string, Key>();
                int i = 0;

                foreach (Key k in _keyDic.Values.ToArray())
                {
                    if (!Verification.VerificationIPAddress(k.FileLocation.Endpoint)) continue;

                    var t = DateTime.Now - k.KeyCreateTime;
                    if (t.Hours < 1 && t.Ticks >= 0)
                    {
                        tempKeyDic[BinaryEditor.BytesToHexString(k.Hash)] = k;
                        i++;
                    }
                }

                _keyDic = tempKeyDic;
            }
        }

        /// <summary>
        /// Keyを追加します
        /// </summary>
        public void Add(Key item)
        {
            if (item == null) return;
            if (item.FileLocation == null) return;
            if (!Verification.VerificationIPAddress(item.FileLocation.Endpoint)) return;
            if (!item.VerifyDigitalSignature()) return;

            var t = DateTime.Now - item.KeyCreateTime;
            if (t.Hours < 1 && t.Ticks >= 0)
            {
                string stringHash = BinaryEditor.BytesToHexString(item.Hash);
                string stringSignatureHash = BinaryEditor.BytesToHexString(item.Cache_SignatureHash);

                AddedNew(this, item);

                lock (_keyDic)
                {
                    if (!_keyDic.ContainsKey(stringHash))
                    {
                        _keyDic.Add(stringHash, item);
                    }
                    else
                    {
                        if (_keyDic[stringHash].KeyCreateTime < item.KeyCreateTime)
                        {
                            _keyDic[stringHash] = item;
                        }
                    }
                }

                lock (_reviewDic)
                {
                    if (item.Review != null && item.Review.Review != Review.なし &&
                        item.Review.ReviewComments != null && item.Review.ReviewComments.Trim() != "")
                    {
                        if (!_reviewDic.ContainsKey(stringSignatureHash))
                        {
                            _reviewDic[stringSignatureHash] = new List<CacheReview>();
                        }
                        if (!_reviewDic[stringSignatureHash].Any(n => BinaryEditor.ArrayEquals(n.SignatureHash, item.Review.SignatureHash)))
                        {
                            _reviewDic[stringSignatureHash].RemoveAll(n => n.PublicKey == item.PublicKey);
                            _reviewDic[stringSignatureHash].Add(item.Review);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Cacheを追加します
        /// </summary>
        public void Add(Cache item, Node node)
        {
            if (item == null) return;
            if (item.Key == null) return;

            Key key = item.Key;
            key.FileLocation = node;
            key.KeyCreateTime = DateTime.Now;

            key.CacheBlockBitmap = new bool[item.CacheBlockHash.Length];

            string stringHash = BinaryEditor.BytesToHexString(item.Hash);
            for (int i = 0; i < item.CacheBlockHash.Length; i++)
            {
                if (Settings.Default._cacheController.ContainsKey(stringHash) && Settings.Default._cacheController[stringHash][i] != null)
                {
                    key.CacheBlockBitmap[i] = true;
                }
            }

            lock (_myReviewDic)
            {
                string stringSignatureHash = BinaryEditor.BytesToHexString(item.SignatureHash);
                if (_myReviewDic.ContainsKey(stringSignatureHash))
                    key.Review = _myReviewDic[stringSignatureHash];
            }

            this.Add(key);
        }

        /// <summary>
        /// Keyリストを追加します
        /// </summary>
        public void AddRange(IEnumerable<Key> collection)
        {
            foreach (Key item in collection)
            {
                this.Add(item);
            }
        }

        /// <summary>
        /// Cacheリストを追加します
        /// </summary>
        public void AddRange(IEnumerable<Cache> collection, Node node)
        {
            foreach (Cache item in collection)
            {
                this.Add(item, node);
            }
        }

        /// <summary>
        /// キャッシュ評価を追加します
        /// </summary>
        /// <param name="hash">キャッシュの電子署名のハッシュ</param>
        /// <param name="review"></param>
        public void AddReview(byte[] signatureHash, CacheReview review)
        {
            string stringSignatureHash = BinaryEditor.BytesToHexString(signatureHash);

            lock (_myReviewDic)
            {
                _myReviewDic[stringSignatureHash] = review;
            }

            lock (_reviewDic)
            {
                if (_reviewDic.ContainsKey(stringSignatureHash))
                    _reviewDic[stringSignatureHash].RemoveAll(n => n.PublicKey == review.PublicKey);
            }
        }

        /// <summary>
        /// キャッシュ評価を検索します
        /// </summary>
        /// <param name="signatureHash"></param>
        /// <returns></returns>
        public CacheReview[] SearchReview(byte[] signatureHash)
        {
            lock (_reviewDic)
            {
                string stringSignatureHash = BinaryEditor.BytesToHexString(signatureHash);

                if (_reviewDic.ContainsKey(stringSignatureHash))
                {
                    return _reviewDic[stringSignatureHash].ToArray();
                }
                else
                {
                    return new CacheReview[0];
                }
            }
        }

        /// <summary>
        /// 指定されたハッシュを持つKeyを検索します
        /// </summary>
        /// <param name="hash">ハッシュ</param>
        public Key[] Search(byte[] hash)
        {
            List<Key> keyList = new List<Key>();

            lock (_keyDic)
            {
                foreach (Key k in _keyDic.Values.ToArray())
                {
                    if (BinaryEditor.ArrayEquals(k.Cache_Hash, hash)) keyList.Add(k);
                }
            }
            return keyList.ToArray();
        }

        /// <summary>
        /// Keyリストを取得します
        /// </summary>
        public Key[] KeyList
        {
            get
            {
                List<Key> keyList = new List<Key>();

                lock (_keyDic)
                {
                    foreach (Key k in _keyDic.Values.ToArray())
                    {
                        keyList.Add(k);
                    }
                }

                return keyList.ToArray();
            }
        }

        /// <summary>
        /// CacheListViewItemリストを取得します
        /// </summary>
        public CacheListViewItem[] CacheListViewItemList
        {
            get
            {
                List<CacheListViewItem> list = new List<CacheListViewItem>();

                lock (_keyDic)
                {
                    foreach (Key k in this.KeyList.ToArray())
                    {
                        if (list.Any(n => BinaryEditor.ArrayEquals(n.SignatureHash, k.Cache_SignatureHash))) continue;
                        list.Add(new CacheListViewItem(k));
                    }
                }

                return list.ToArray();
            }
        }
    }

    /// <summary>
    /// Key情報を提供します
    /// </summary>
    [Serializable]
    [DataContract]
    public class Key
    {
        /// <summary>
        /// クラスの新しいインスタンスを初期化します
        /// </summary>
        public Key()
        {
            this.KeyCreateTime = DateTime.Now;
        }

        #region Cacheヘッダ情報部

        /// <summary>
        /// デジタル署名を作成する
        /// </summary>
        /// <param name="privateKey">署名に使用する秘密鍵</param>
        public void CreateDigitalSignature(string privateKey)
        {
            try
            {
                Signature = DigitalSignature.CreateDigitalSignature(GetHash(), privateKey);
            }
            catch { }
            try
            {
                Signature_SHA1 = DigitalSignature.CreateDigitalSignature_SHA1(GetHash(), privateKey);
            }
            catch { }
        }

        /// <summary>
        /// デジタル署名を検証する
        /// </summary>
        public bool VerifyDigitalSignature()
        {
            if (PublicKey == null) return false;
            if (Signature == null && Signature_SHA1 == null) return false;

            try
            {
                if (Signature != null)
                    return DigitalSignature.VerifyDigitalSignature(GetHash(), Signature, PublicKey);
            }
            catch { }
            try
            {
                if (Signature_SHA1 != null)
                    return DigitalSignature.VerifyDigitalSignature_SHA1(GetHash(), Signature_SHA1, PublicKey);
            }
            catch { }

            return false;
        }

        private byte[] GetHash()
        {
            List<byte> dest = new List<byte>();

            dest.AddRange(Encoding.Unicode.GetBytes(this.Cache_FileName));
            dest.AddRange(BitConverter.GetBytes(this.Cache_Size));
            dest.AddRange(BitConverter.GetBytes(this.Cache_CreationTime.Ticks));

            if (this.Cache_Sign != null)
                dest.AddRange(Encoding.Unicode.GetBytes(this.Cache_Sign));

            foreach (string ss in this.Cache_Category)
            {
                dest.AddRange(Encoding.Unicode.GetBytes(ss));
            }

            dest.AddRange(this.Cache_Hash);
            dest.AddRange(this.Cache_SignatureHash);

            dest.AddRange(this.Cache_Signature);
            dest.AddRange(this.Cache_Signature_SHA1);

            return HashFunction.HashCreate(dest.ToArray());
        }

        /// <summary>
        /// ハッシュを取得します
        /// </summary>
        /// <returns></returns>
        public byte[] Hash
        {
            get
            {
                List<byte> byteList = new List<byte>();

                byteList.AddRange(FileLocation.NodeID);
                byteList.AddRange(GetHash());

                byteList.AddRange(BitConverter.GetBytes(this.Review == null ? false : true));

                return HashFunction.HashCreate(byteList.ToArray());
            }
        }

        /// <summary>
        /// キャッシュのファイル名を取得または設定します
        /// </summary>
        [DataMember]
        public string Cache_FileName { get; set; }

        /// <summary>
        /// キャッシュのサイズを取得または設定します
        /// </summary>
        [DataMember]
        public long Cache_Size { get; set; }

        /// <summary>
        /// キャッシュの作成時間を取得または設定します
        /// </summary>
        [DataMember]
        public DateTime Cache_CreationTime { get; set; }

        /// <summary>
        /// キャッシュのサインを取得または設定します
        /// </summary>
        [DataMember]
        public string Cache_Sign { get; set; }

        private string[] _category;

        /// <summary>
        /// キャッシュのカテゴリを取得または設定します
        /// </summary>
        [DataMember]
        public string[] Cache_Category
        {
            get { return _category; }
            set
            {
                if (value.Length > 3) throw new ArgumentOutOfRangeException();

                _category = value;
                this.Cache_CategoryHash = new byte[3][];

                for (int i = 0; i < _category.Count(); i++)
                {
                    this.Cache_CategoryHash[i] = HashFunction.HashCreate(_category[i].ToLower());
                }
            }
        }

        /// <summary>
        /// カテゴリのハッシュリストを取得します
        /// </summary>
        public byte[][] Cache_CategoryHash { get; private set; }

        /// <summary>
        /// キャッシュのハッシュを取得または設定します
        /// </summary>
        [DataMember]
        public byte[] Cache_Hash { get; set; }

        /// <summary>
        /// キャッシュの電子署名のハッシュを取得または設定します
        /// </summary>
        [DataMember]
        public byte[] Cache_SignatureHash { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Cache_Signature_SHA1 { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Cache_Signature { get; set; }

        #endregion

        /// <summary>
        /// 公開鍵を取得または設定します
        /// </summary>
        [DataMember]
        public string PublicKey { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Signature { get; private set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Signature_SHA1 { get; private set; }

        /// <summary>
        /// ファイルの場所の情報を取得または設定します
        /// </summary>
        [DataMember]
        public Node FileLocation { get; set; }

        /// <summary>
        /// Keyの作成時間
        /// </summary>
        [DataMember]
        public DateTime KeyCreateTime { get; set; }

        /// <summary>
        /// キャッシュブロックのビットマップを取得または設定します
        /// </summary>
        [DataMember]
        public bool[] CacheBlockBitmap { get; set; }

        /// <summary>
        /// キャッシュ評価情報を取得または設定します
        /// </summary>
        [DataMember]
        public CacheReview Review { get; set; }
    }

    /// <summary>
    /// Review
    /// </summary>
    [Serializable]
    [DataContract]
    public enum Review
    {
        [EnumMember]
        なし = 0,
        [EnumMember]
        良い = 1,
        [EnumMember]
        悪い = 2
    }

    /// <summary>
    /// Cache評価情報を提供します
    /// </summary>
    [Serializable]
    [DataContract]
    public class CacheReview
    {
        /// <summary>
        /// クラスの新しいインスタンスを初期化します
        /// </summary>
        public CacheReview()
        {
            this.CreateTime = DateTime.Now;
        }

        /// <summary>
        /// キャッシュの評価を取得または設定します
        /// </summary>
        [DataMember]
        public Review Review { get; set; }

        [DataMember]
        private string _reviewComments;

        /// <summary>
        /// キャッシュのコメントを取得または設定します
        /// </summary>
        public string ReviewComments
        {
            get { return _reviewComments; }
            set
            {
                if (value.Split('\n').Length > 10)
                {
                    return;
                }

                if (value == null)
                {
                    _reviewComments = null;
                }
                else if (value.Length > 128)
                {
                    _reviewComments = value.Substring(0, 128);
                }
                else
                {
                    _reviewComments = value;
                }
            }
        }

        [DataMember]
        private string _sign;

        /// <summary>
        /// サインを取得または設定します
        /// </summary>
        public string Sign
        {
            get { return _sign; }
            set
            {
                if (value == null)
                {
                    _sign = null;
                }
                else if (value.Length > 10)
                {
                    _sign = value.Substring(0, 10);
                }
                else
                {
                    _sign = value;
                }
            }
        }

        /// <summary>
        /// キャッシュのコメント作成時間
        /// </summary>
        [DataMember]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// キャッシュの評価に対する公開鍵を取得または設定します
        /// </summary>
        [DataMember]
        public string PublicKey { get; set; }

        /// <summary>
        /// キャッシュ評価に対する電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Signature { get; private set; }

        /// <summary>
        /// キャッシュ評価に対する電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Signature_SHA1 { get; private set; }

        /// <summary>
        /// 電子署名のハッシュを取得します
        /// </summary>
        public byte[] SignatureHash
        {
            get
            {
                List<byte> byteList = new List<byte>();

                if (this.Signature != null)
                {
                    byteList.AddRange(this.Signature);
                }
                else if (this.Signature_SHA1 != null)
                {
                    byteList.AddRange(this.Signature_SHA1);
                }

                return HashFunction.HashCreate(byteList.ToArray());
            }
        }

        /// <summary>
        /// デジタル署名を作成する
        /// </summary>
        /// <param name="privateKey">署名に使用する秘密鍵</param>
        public void CreateDigitalSignature(string privateKey)
        {
            try
            {
                Signature = DigitalSignature.CreateDigitalSignature(GetHash(), privateKey);
            }
            catch { }
            try
            {
                Signature_SHA1 = DigitalSignature.CreateDigitalSignature_SHA1(GetHash(), privateKey);
            }
            catch { }
        }

        /// <summary>
        /// デジタル署名を検証する
        /// </summary>
        public bool VerifyDigitalSignature()
        {
            if (PublicKey == null) return false;
            if (Signature == null && Signature_SHA1 == null) return false;

            try
            {
                if (Signature != null)
                    return DigitalSignature.VerifyDigitalSignature(GetHash(), Signature, PublicKey);
            }
            catch { }
            try
            {
                if (Signature_SHA1 != null)
                    return DigitalSignature.VerifyDigitalSignature_SHA1(GetHash(), Signature_SHA1, PublicKey);
            }
            catch { }

            return false;
        }

        private byte[] GetHash()
        {
            List<byte> dest = new List<byte>();

            dest.AddRange(Encoding.Unicode.GetBytes(this.ReviewComments));
            dest.AddRange(BitConverter.GetBytes(this.CreateTime.Ticks));
            dest.AddRange(Encoding.Unicode.GetBytes(this.Sign));

            dest.AddRange(BitConverter.GetBytes((int)this.Review));

            return HashFunction.HashCreate(dest.ToArray());
        }
    }
}