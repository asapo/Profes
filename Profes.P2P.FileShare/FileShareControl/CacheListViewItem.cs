using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Profes.P2P.FileShare.ServiceModel;
using System.Windows.Media;
using Profes.P2P.FileShare.Properties;
using Profes.Security.Cryptography;
using System.ComponentModel;
using System.Timers;
using System.Windows.Threading;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Profes.BinaryEditorBase;

namespace Profes.P2P.FileShare.FileShareControl
{
    [Serializable]
    public class CacheListViewItem : INotifyPropertyChanged
    {
        [field:NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public CacheListViewItem(Cache cache)
        {
            this.Category = cache.Category;
            this.CreationTime = cache.CreationTime;
            this.Hash = cache.Hash;
            this.Name = cache.Name;
            this.Sign = cache.Sign;
            this.Signature = cache.Signature;
            this.Signature_SHA1 = cache.Signature_SHA1;
            this.SignatureHash = cache.SignatureHash;
            this.Size = cache.Size;
            this.CacheBlockLength = cache.CacheBlockHash.Length;
            this.PublicKey = cache.PublicKey;

            this.UploadBlockBitmap = new bool[this.CacheBlockLength];

            this.Cache = cache;
        }

        public CacheListViewItem(Key key)
        {
            this.Category = key.Cache_Category;
            this.CreationTime = key.Cache_CreationTime;
            this.Hash = key.Cache_Hash;
            this.Name = key.Cache_FileName;
            this.Sign = key.Cache_Sign;
            this.Signature = key.Signature;
            this.Signature_SHA1 = key.Signature_SHA1;
            this.SignatureHash = key.Cache_SignatureHash;
            this.Size = key.Cache_Size;
            this.CacheBlockLength = key.CacheBlockBitmap.Length;
            this.PublicKey = key.PublicKey;

            this.UploadBlockBitmap = new bool[this.CacheBlockLength];

            this.Key = key;
        }

        /// <summary>
        /// キャッシュヘッダを取得します
        /// </summary>
        public Cache Cache { get; private set; }

        /// <summary>
        /// Key情報を取得します
        /// </summary>
        public Key Key { get; private set; }


        /// <summary>
        /// キャッシュのファイル名を取得または設定します
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// キャッシュのサイズを取得または設定します
        /// </summary>
        public long Size { get; set; }

        /// <summary>
        /// キャッシュの作成時間を取得または設定します
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// キャッシュのサインを取得または設定します
        /// </summary>
        public string Sign { get; set; }

        private string[] _category;

        /// <summary>
        /// キャッシュのカテゴリを取得または設定します
        /// </summary>
        public string[] Category
        {
            get { return _category; }
            set
            {
                if (value.Length > 3) throw new ArgumentOutOfRangeException();

                _category = value;
                this.CategoryHash = new byte[3][];

                for (int i = 0; i < _category.Count(); i++)
                {
                    this.CategoryHash[i] = HashFunction.HashCreate(_category[i].ToLower());
                }
            }
        }

        /// <summary>
        /// カテゴリのハッシュリストを取得します
        /// </summary>
        public byte[][] CategoryHash { get; private set; }

        /// <summary>
        /// キャッシュのハッシュを取得または設定します
        /// </summary>
        public byte[] Hash { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        public byte[] Signature { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        public byte[] Signature_SHA1 { get; set; }

        /// <summary>
        /// キャッシュの電子署名のハッシュを取得または設定します
        /// </summary>
        public byte[] SignatureHash { get; set; }

        /// <summary>
        /// 公開鍵を取得または設定します
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// IDを取得します
        /// </summary>
        public byte[] ID
        {
            get
            {
                return HashFunction.HashCreate(this.PublicKey);
            }
        }

        /// <summary>
        /// キャッシュブロックリストの長さを取得または設定します
        /// </summary>
        public long CacheBlockLength { get; set; }

        /// <summary>
        /// キャッシュ率を取得します
        /// </summary>
        public double Rate
        {
            get
            {
                string stringhash = BinaryEditor.BytesToHexString(this.Hash);

                if (Settings.Default._cacheController.ContainsKey(stringhash))
                {
                    return 100 * ((double)Settings.Default._cacheController[stringhash].Count(n => n != null) / (double)this.CacheBlockLength);
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// キャッシュ率をstringで取得します
        /// </summary>
        public string RateString
        {
            get
            {
                string stringhash = BinaryEditor.BytesToHexString(this.Hash);

                if (!Settings.Default._cacheController.ContainsKey(stringhash))
                {
                    return string.Format("0/{0}", this.CacheBlockLength);
                }
                else
                {
                    return string.Format("{0}/{1}", Settings.Default._cacheController[stringhash].Count(n => n != null), this.CacheBlockLength);
                }
            }
        }

        [OptionalField(VersionAdded = 2)]
        bool _isChecked;

        /// <summary>
        /// CacheListViewItem がチェックされているかどうかを取得または設定します
        /// </summary>
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                _isChecked = value;
                NotifyPropertyChanged("IsChecked");
                NotifyPropertyChanged("Foreground");
            }
        }

        /// <summary>
        /// Brush を取得または設定します。
        /// </summary>
        public Brush Foreground
        {
            get
            {
                if (IsChecked == true)
                {
                    return Brushes.Blue;
                }
                else
                {
                    return Brushes.Black;
                }
            }
        }

        /// <summary>
        /// ダウンロード可能なブロックのビットマップを取得または設定します
        /// </summary>
        public bool[] DownloadBlockBitmap
        {
            get
            {
                bool[] block = new bool[this.CacheBlockLength];

                foreach (Key key in Settings.Default._keyController.Search(this.Hash))
                {
                    for (int i = 0; i < key.CacheBlockBitmap.Length && i < block.Length; i++)
                    {
                        block[i] |= key.CacheBlockBitmap[i];
                    }
                }

                return block;
            }
        }

        /// <summary>
        /// ダウンロード可能なブロック率をStringで取得します
        /// </summary>
        public string DownloadRateString
        {
            get
            {
                return string.Format("{0}/{1}", DownloadBlockBitmap.Count(n => n == true), this.CacheBlockLength);
            }
        }

        /// <summary>
        /// ダウンロード可能なブロック率を取得します
        /// </summary>
        public double DownloadRate { get { return 100 * ((double)DownloadBlockBitmap.Count(n => n == true)) / ((double)this.CacheBlockLength); } }

        /// <summary>
        /// アップロードするキャッシュのビットマップを取得または設定します
        /// </summary>
        public bool[] UploadBlockBitmap { get; set; }

        /// <summary>
        /// アップロード率を取得します
        /// </summary>
        public double UploadRate { get { return 100 * ((double)UploadBlockBitmap.Count(n => n == true) / (double)UploadBlockBitmap.Length); } }

        /// <summary>
        /// アップロード率をStringで取得します
        /// </summary>
        public string UploadRateString { get { return string.Format("{0}/{1}", UploadBlockBitmap.Count(n => n == true), UploadBlockBitmap.Length); } }

        /// <summary>
        /// キャッシュの評価を取得または設定します
        /// </summary>
        public Review Review { get; set; }

        /// <summary>
        /// Review率を取得します
        /// </summary>
        public double ReviewRate
        {
            get
            {
                var reviewList = Settings.Default._keyController.SearchReview(this.SignatureHash);

                if (reviewList.Length == 0)
                {
                    return 50;
                }
                else
                {
                    return 100 * ((double)reviewList.Count(n => n.Review == Review.良い) / (double)reviewList.Count(n => n.Review != Review.なし));
                }
            }
        }

        /// <summary>
        /// Review率をStringで取得します
        /// </summary>
        public string ReviewRateString
        {
            get
            {
                var reviewList = Settings.Default._keyController.SearchReview(this.SignatureHash);
                return string.Format("{0}/{1}", reviewList.Count(n => n.Review == Review.良い), reviewList.Count(n => n.Review == Review.悪い));
            }
        }
    }
}