using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Profes.Security.Cryptography;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Linq;
using System.Globalization;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// キャッシュ情報を提供します
    /// </summary>
    [Serializable]
    [DataContract]
    public class Cache
    {
        /// <summary>
        /// Keyを取得または設定します
        /// </summary>
        [DataMember]
        public Key Key { get; set; }

        #region メソッド

        /// <summary>
        /// クラスの新しいインスタンスを初期化します
        /// </summary>
        public Cache()
        {
            this.CreationTime = DateTime.Now;
        }

        /// <summary>
        /// キャッシュのキーを作成します
        /// </summary>
        /// <returns>キーを返す</returns>
        public void CreateKey(string privateKey)
        {
            this.Key = new Key
            {
                PublicKey = this.PublicKey,
                Cache_Category = this.Category,
                Cache_CreationTime = this.CreationTime,
                Cache_FileName = this.Name,
                Cache_Hash = this.Hash,
                Cache_Sign = this.Sign,
                Cache_SignatureHash = this.SignatureHash,
                Cache_Size = this.Size,
                Cache_Signature = this.Signature,
                Cache_Signature_SHA1 = this.Signature_SHA1,
            };

            Key.CreateDigitalSignature(privateKey);
        }

        /// <summary>
        /// デジタル署名を作成する
        /// </summary>
        /// <param name="privateKey">署名に使用する秘密鍵</param>
        public void CreateDigitalSignature(string privateKey)
        {
            try
            {
                Signature = null;
                Signature = DigitalSignature.CreateDigitalSignature(GetHash(), privateKey);
            }
            catch { }
            try
            {
                Signature_SHA1 = null;
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

            // -----キャッシュブロックのハッシュを書き込む-----
            foreach (byte[] key in this.CacheBlockHash)
            {
                dest.AddRange(key);
            }

            // -----サインを書き込む-----
            if (this.Sign != null && this.Sign.Trim().Length != 0)
                dest.AddRange(Encoding.Unicode.GetBytes(this.Sign));

            // -----ファイル名を書き込む-----
            if (this.Name != null)
                dest.AddRange(Encoding.Unicode.GetBytes(this.Name));

            // -----ファイルサイズを書き込む-----
            dest.AddRange(BitConverter.GetBytes(this.Size));

            // ----- 作成日を書き込む -----
            if (this.CreationTime != null)
                dest.AddRange(BitConverter.GetBytes(CreationTime.Ticks));

            return HashFunction.HashCreate(dest.ToArray());
        }

        /// <summary>
        /// カテゴリの正規化
        /// </summary>
        public static string CategoryRegularization(string item)
        {
            item = item.ToLower();
            item.ToLower(CultureInfo.GetCultureInfo("ja-JP"));
            item.ToLower(CultureInfo.GetCultureInfo("en-US"));

            return item;
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// キャッシュブロックハッシュリストを取得または設定します
        /// </summary>
        [DataMember]
        public byte[][] CacheBlockHash { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Signature { get; set; }

        /// <summary>
        /// 電子署名を取得します
        /// </summary>
        [DataMember]
        public byte[] Signature_SHA1 { get; set; }

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
        /// 公開鍵を取得または設定します
        /// </summary>
        [DataMember]
        public string PublicKey { get; set; }

        /// <summary>
        /// ファイル名を取得または設定します
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// ファイルサイズを取得します
        /// </summary>
        [DataMember]
        public long Size { get; set; }

        private string[] _category = new string[3];

        /// <summary>
        /// カテゴリリストを取得または設定します
        /// </summary>
        [DataMember]
        public string[] Category
        {
            get { return _category; }
            set
            {
                _category = value;
                this.CategoryHash = new byte[3][];

                for (int i = 0; i < _category.Count(); i++)
                {
                    this.CategoryHash[i] = HashFunction.HashCreate(CategoryRegularization(_category[i]));
                }
            }
        }

        /// <summary>
        /// カテゴリのハッシュリストを取得します
        /// </summary>
        public byte[][] CategoryHash { get; private set; }

        /// <summary>
        /// 作成時間を取得します
        /// </summary>
        [DataMember]
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// ハッシュを取得します
        /// </summary>
        public byte[] Hash
        {
            get
            {
                List<byte> dest = new List<byte>();

                // -----キャッシュブロックのハッシュを書き込む-----
                foreach (byte[] key in this.CacheBlockHash)
                {
                    dest.AddRange(key);
                }

                return HashFunction.HashCreate(dest.ToArray());
            }
        }

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

        #endregion
    }
}