using System;
using System.Collections.Generic;
using System.IO;
using Profes.Security.Cryptography;
using Profes.P2P.FileShare.FileShareControl;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Reflection;
using System.Windows.Forms;
using Profes.BinaryEditorBase;
using System.IO.Compression;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// キャッシュ制御のためのメソッドを提供します
    /// </summary>
    [Serializable]
    public class CacheController : Dictionary<string, List<CacheBlock>>, ISerializable
    {
        private const long FILESIZE = 1048576; // 分割サイズ（2進数100000000000000000000）

        #region メソッド

        /// <summary>
        /// CacheControllerクラスの新しいインスタンスを初期化します
        /// </summary>
        public CacheController()
        {
            this.CacheList = new List<Cache>();
        }

        /// <summary>
        /// CacheControllerクラスの新しいインスタンスを初期化します
        /// </summary>
        /// <param name="cacheDirectoryPath">キャッシュディレクトリへのパス</param>
        public CacheController(string cacheDirectoryPath)
        {
            this.CacheDirectoryPath = cacheDirectoryPath;
        }

        protected CacheController(SerializationInfo info, StreamingContext context)
            : base()
        {
            var KeyList = (string[])info.GetValue("Dictionary_Keys", typeof(string[]));
            var ValueList = (CacheBlock[][])info.GetValue("Dictionary_Values", typeof(CacheBlock[][]));

            for (int i = 0; i < KeyList.Length; i++)
            {
                this.Add(KeyList[i], ValueList[i].ToList<CacheBlock>());
            }

            CacheList = ((Cache[])info.GetValue("CacheList", typeof(Cache[]))).ToList<Cache>();
            CacheDirectoryPath = info.GetString("CacheDirectoryPath");
            PublicKey = info.GetString("PublicKey");
            PrivateKey = info.GetString("PrivateKey");
        }

        new public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            string[] KeyList = new string[base.Count];
            CacheBlock[][] ValueList = new CacheBlock[base.Count][];

            int i = 0;
            foreach (string key in this.Keys)
            {
                KeyList[i] = key;
                ValueList[i] = base[key].ToArray();
                i++;
            }

            info.AddValue("Dictionary_Keys", KeyList.ToArray(), typeof(string[]));
            info.AddValue("Dictionary_Values", ValueList.ToArray(), typeof(CacheBlock[][]));

            lock (this.CacheList)
            {
                if (this.CacheList != null)
                {
                    info.AddValue("CacheList", this.CacheList.ToArray(), typeof(Cache[]));
                }
                else
                {
                    info.AddValue("CacheList", new Cache[0], typeof(Cache[]));
                }
            }

            info.AddValue("CacheDirectoryPath", this.CacheDirectoryPath);
            info.AddValue("PublicKey", this.PublicKey);
            info.AddValue("PrivateKey", this.PrivateKey);
        }

        /// <summary>
        /// ファイルをキャッシュへ変換する
        /// </summary>
        /// <param name="filePath">ファイルへのパス</param>
        /// <param name="category">キャッシュのカテゴリ</param>
        public Cache FileToCache(string filePath, string[] category)
        {
            if (PrivateKey == null) throw new ApplicationException("PrivateKeyが不正です");
            if (PublicKey == null) throw new ApplicationException("PublicKeyが不正です");

            Cache cache = new Cache() { Category = category };
            List<CacheBlock> cacheBlockList = new List<CacheBlock>();
            List<byte[]> cacheBlockHashList = new List<byte[]>();

            using (FileStream src = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                for (long remain = src.Length, num = 0; remain > 0; remain -= FILESIZE, num++)
                {
                    int length = (int)Math.Min(FILESIZE, remain);
                    byte[] dest = new byte[length];
                    src.Seek(num * FILESIZE, SeekOrigin.Begin);
                    src.Read(dest, 0, length);

                    cacheBlockHashList.Add(HashFunction.HashCreate(dest));
                    cache.Size += length;
                    cacheBlockList.Add(new CacheBlock() { Offset = num * FILESIZE, Count = length });
                }
            }
            cache.CacheBlockHash = cacheBlockHashList.ToArray();
            cache.Name = Path.GetFileName(filePath);
            cache.Sign = this.Sign;
            cache.PublicKey = PublicKey;

            // 電子署名の作成
            cache.CreateDigitalSignature(PrivateKey);

            // キーの作成
            cache.CreateKey(PrivateKey);

            // キャッシュの本体が存在する場合、上書きする
            string cacheFilePath = CacheFilePathFind(cache.Hash);
            if (File.Exists(cacheFilePath)) File.Delete(cacheFilePath);
            File.Copy(filePath, cacheFilePath);

            // キャッシュヘッダが存在する場合、上書きする
            if (!this.ContainsKey(BinaryEditor.BytesToHexString(cache.Hash)))
            {
                this.Add(BinaryEditor.BytesToHexString(cache.Hash), cacheBlockList);
            }
            else
            {
                this[BinaryEditor.BytesToHexString(cache.Hash)] = cacheBlockList;
            }

            // this.CacheListにキャッシュヘッダが存在する場合、上書きする
            lock (this.CacheList)
            {
                int index = 0;

                while (this.CacheList.Count > index && !BinaryEditor.ArrayEquals(this.CacheList[index].SignatureHash, cache.SignatureHash)) index++;

                if (this.CacheList.Count > index)
                {
                    this.CacheList.RemoveAt(index);
                }

                this.CacheList.Add(cache);
            }

            return cache;
        }

        /// <summary>
        /// キャッシュをファイルへ変換する
        /// </summary>
        /// <param name="cache">変換するキャッシュ</param>
        /// <param name="downloadDirectoryPath">ダウンロードディレクトリへのパス</param>
        public void CacheToFile(Cache cache, string downloadDirectoryPath)
        {
            string downloadFilePath = FileNameWithoutRepetition(Path.Combine(downloadDirectoryPath, cache.Name));
            string cacheFilePath = CacheFilePathFind(cache.Hash);

            if ("" == cacheFilePath) throw new ApplicationException("キャッシュが存在しません");
            if (false == cache.VerifyDigitalSignature()) throw new ApplicationException("電子署名が不正です");

            try
            {
                using (FileStream writer = new FileStream(downloadFilePath, FileMode.Create, FileAccess.Write))
                {
                    for (int i = 0; i < this[BinaryEditor.BytesToHexString(cache.Hash)].Count; i++)
                    {
                        byte[] dest = this[cache, i];
                        writer.Write(dest, 0, dest.Length);
                    }
                }
            }
            catch (ApplicationException)
            {
                throw new ApplicationException("ファイル変換エラー");
            }
        }

        /// <summary>
        /// キャッシュカテゴリに一致するキャッシュを取得します
        /// </summary>
        /// <param name="key">検索するカテゴリ</param>
        public Cache[] GetCategoryFind(byte[] key)
        {
            List<Cache> cacheList = new List<Cache>();

            lock (this.CacheList)
            {
                foreach (Cache cache in this.CacheList)
                {
                    if (cache.CategoryHash.Any(item => BinaryEditor.ArrayEquals(item, key)))
                    {
                        cacheList.Add(cache);
                    }
                }
            }

            return cacheList.ToArray();
        }

        /// <summary>
        /// キャッシュカテゴリに一致するキャッシュを取得します
        /// </summary>
        /// <param name="key">検索するカテゴリ</param>
        public Cache[] GetCategoryFind(string key)
        {
            return this.GetCategoryFind(HashFunction.HashCreate(key));
        }

        /// <summary>
        /// キャッシュ率を取得します
        /// </summary>
        public double Rate(Cache cache)
        {
            string stringhash = BinaryEditor.BytesToHexString(cache.Hash);

            if (!this.ContainsKey(stringhash)) return 0;
            else return 100 * ((double)this[stringhash].Count(n => n != null) / (double)cache.CacheBlockHash.Length);
        }

        /// <summary>
        /// キャッシュファイルの場所を検索します
        /// </summary>
        /// <param name="hash">キャッシュのハッシュ</param>
        /// <returns>キャッシュファイルへのパス</returns>
        private string CacheFilePathFind(byte[] hash)
        {
            return Path.Combine(CacheDirectoryPath, Convert.ToBase64String(hash).Replace("/", "-"));
        }

        /// <summary>
        /// 重複のないファイル名を生成する
        /// </summary>
        private string FileNameWithoutRepetition(string path)
        {
            if (File.Exists(path) == false) return path;
            
            for (int index = 1; ; index++)
            {
                string text = string.Format(@"{0}\{1}({2}){3}",
                    Path.GetDirectoryName(path),
                    Path.GetFileNameWithoutExtension(path),
                    index,
                    Path.GetExtension(path));

                if (File.Exists(text) == false) return text;
            }
        }

        /// <summary>
        /// キャッシュブロックが存在するかどうかを示す値を返します
        /// </summary>
        /// <param name="cache"></param>
        /// <param name="cacheBlockHash"></param>
        /// <returns></returns>
        public bool Contains(Cache cache, byte[] cacheBlockHash)
        {
            try
            {
                byte[] value = this[cache, cacheBlockHash];
                return BinaryEditor.ArrayEquals(HashFunction.HashCreate(value), cacheBlockHash);
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region プロパティ

        /// <summary>
        /// 指定したハッシュのキャッシュブロックを取得または設定します
        /// </summary>
        /// <param name="cache">キャッシュのハッシュ</param>
        /// <param name="cacheBlockIndex">キャッシュブロックインデックス</param>
        /// <returns>キャッシュブロック</returns>
        public byte[] this[Cache cache, int cacheBlockIndex]
        {
            get
            {
                if (cache == null)
                {
                    throw new ApplicationException("cacheが不正です");
                }
                if (cacheBlockIndex >= cache.CacheBlockHash.Length)
                {
                    throw new ApplicationException("cacheBlockIndexが不正です");
                }

                string stringCacheHash = "";
                byte[] dest = null;

                try
                {
                    stringCacheHash = BinaryEditor.BytesToHexString(cache.Hash);

                    CacheBlock cacheBlock = this[stringCacheHash][cacheBlockIndex];
                    dest = new byte[cacheBlock.Count];

                    using (FileStream reader = new FileStream(CacheFilePathFind(cache.Hash), FileMode.Open, FileAccess.Read))
                    {
                        reader.Seek(cacheBlock.Offset, SeekOrigin.Begin);
                        reader.Read(dest, 0, cacheBlock.Count);
                    }

                    if (!BinaryEditor.ArrayEquals(cache.CacheBlockHash[cacheBlockIndex], HashFunction.HashCreate(dest)))
                    {
                        this[stringCacheHash][cacheBlockIndex] = null;

                        throw new ApplicationException("キャッシュブロックが壊れています");
                    }
                }
                catch (FileNotFoundException)
                {
                    lock (this.CacheList)
                    {
                        this.CacheList.Remove(cache);
                    }

                    this.Remove(stringCacheHash);

                    throw new ApplicationException("キャッシュが削除されていました");
                }
                catch (KeyNotFoundException)
                {
                    lock (this.CacheList)
                    {
                        this.CacheList.Remove(cache);
                    }

                    throw new ApplicationException("キャッシュが存在しません");
                }
                catch (NullReferenceException)
                {
                    throw new ApplicationException("キャッシュが存在しません");
                }
                catch (IOException ex)
                {
                    throw new ApplicationException(ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    throw new ApplicationException("キャッシュが存在しません");
                }
                catch (ApplicationException ex)
                {
                    throw new ApplicationException(ex.Message);
                }

                return dest;
            }
            set
            {
                if (cache == null)
                {
                    throw new ApplicationException("cacheが不正です");
                }
                if (cacheBlockIndex >= cache.CacheBlockHash.Length)
                {
                    throw new ApplicationException("cacheBlockIndexが不正です");
                }
                if (!BinaryEditor.ArrayEquals(cache.CacheBlockHash[cacheBlockIndex], HashFunction.HashCreate(value)))
                {
                    throw new ApplicationException("キャッシュブロックが壊れています");
                }
                if (false == cache.VerifyDigitalSignature())
                {
                    throw new ApplicationException("不正な電子署名です");
                }

                string stringCacheHash = BinaryEditor.BytesToHexString(cache.Hash);

                lock (this.CacheList)
                {
                    if (!CacheList.Any(n => BinaryEditor.ArrayEquals(n.SignatureHash, cache.SignatureHash)))
                    {
                        CacheList.Add(cache);
                    }
                }
                if (!base.ContainsKey(stringCacheHash))
                {
                    base.Add(stringCacheHash, new List<CacheBlock>(new CacheBlock[cache.CacheBlockHash.Length]));
                }

                CacheBlock cacheBlock = this[stringCacheHash][cacheBlockIndex];

                try
                {
                    if (cacheBlock != null)
                    {
                        using (FileStream writer = new FileStream(CacheFilePathFind(cache.Hash), FileMode.OpenOrCreate, FileAccess.Write))
                        {
                            writer.Seek(cacheBlock.Offset, SeekOrigin.Begin);
                            writer.Write(value, 0, cacheBlock.Count);
                        }
                    }
                    else
                    {
                        cacheBlock = new CacheBlock();
                        using (FileStream writer = new FileStream(CacheFilePathFind(cache.Hash), FileMode.Append, FileAccess.Write))
                        {
                            cacheBlock.Offset = writer.Position;
                            writer.Write(value, 0, (cacheBlock.Count = value.Length));
                        }

                        this[stringCacheHash][cacheBlockIndex] = cacheBlock;
                    }
                }
                catch (IOException ex)
                {
                    throw new ApplicationException(ex.Message);
                }
            }
        }

        /// <summary>
        /// 指定したハッシュのキャッシュブロックを取得または設定します
        /// </summary>
        /// <param name="cache">キャッシュのヘッダ</param>
        /// <param name="cacheBlockHash">キャッシュブロックハッシュ</param>
        /// <returns>キャッシュブロック</returns>
        public byte[] this[Cache cache, byte[] cacheBlockHash]
        {
            get
            {
                if (cache == null)
                {
                    throw new ApplicationException("cacheが不正です");
                }
                if (cacheBlockHash == null || cache.CacheBlockHash.Any(n => BinaryEditor.ArrayEquals(n, cacheBlockHash)) == false)
                {
                    throw new ApplicationException("cacheBlockHashが不正です");
                }

                int index = 0;
                while (index < cache.CacheBlockHash.Length
                    && !BinaryEditor.ArrayEquals(cache.CacheBlockHash[index], cacheBlockHash)) index++;

                if (index == cache.CacheBlockHash.Length) return null;
                else return this[cache, index];
            }
            set
            {
                if (cache == null)
                {
                    throw new ApplicationException("cacheが不正です");
                }
                if (cacheBlockHash == null || cache.CacheBlockHash.Any(n => BinaryEditor.ArrayEquals(n, cacheBlockHash)) == false)
                {
                    throw new ApplicationException("cacheBlockHashが不正です");
                }

                for (int i = 0; i < cache.CacheBlockHash.Length; i++)
                {
                    if (BinaryEditor.ArrayEquals(cache.CacheBlockHash[i], cacheBlockHash))
                    {
                        this[cache, i] = value;
                    }
                }
            }
        }

        /// <summary>
        /// キャッシュリスト
        /// </summary>
        public IList<Cache> CacheList { get; private set; }

        /// <summary>
        /// キャッシュディレクトリへのパス
        /// </summary>
        public string CacheDirectoryPath { get; set; }

        /// <summary>
        /// 公開鍵を取得または設定します
        /// </summary>
        public string PublicKey { get; set; }

        /// <summary>
        /// 秘密鍵を取得または設定します
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// サインを取得または設定します
        /// </summary>
        public string Sign { get; set; }

        #endregion
    }

    /// <summary>
    /// キャッシュブロック情報を提供します
    /// </summary>
    [Serializable]
    public class CacheBlock
    {
        #region プロパティ

        /// <summary>
        /// キャッシュの先頭からのバイト オフセット
        /// </summary>
        public long Offset { get; set; }

        /// <summary>
        /// キャッシュのバイト数
        /// </summary>
        public int Count { get; set; }

        #endregion
    }
}