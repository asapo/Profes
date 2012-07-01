using System;
using System.IO;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Profes.Security.Cryptography
{
    /// <summary>
    /// AES暗号化クラス
    /// </summary>
    public static class AES_Encryption
    {
        /// <summary>
        /// 共通キーと初期化ベクトルを返す
        /// </summary>
        /// <param name="key">作成された共通キー</param>
        /// <param name="IV">作成された初期化ベクトル</param>
        public static void CreateKeys(out byte[] key, out byte[] IV)
        {
            using (RijndaelManaged aes = new RijndaelManaged())
            {
                aes.KeySize = 256;

                aes.GenerateKey();
                aes.GenerateIV();

                key = aes.Key;
                IV = aes.IV;
            }
        }

        /// <summary>
        /// AES暗号化する
        /// </summary>
        /// <param name="key">パスワード</param>
        /// <param name="IV">初期化ベクトル</param>
        /// <param name="value">暗号化する文字列</param>
        public static byte[] Encrypt(byte[] key, byte[] IV, byte[] value)
        {
            // エラーチェック
            if (key == null || key.Length <= 0) throw new ArgumentNullException("Keyが不正");
            if (IV == null || IV.Length <= 0) throw new ArgumentNullException("IVが不正");
            if (value == null || value.Length <= 0) throw new ArgumentNullException("valueが不正");

            using (RijndaelManaged aes = new RijndaelManaged())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, IV), CryptoStreamMode.Write))
            {
                cs.Write(value, 0, value.Length);
                cs.FlushFinalBlock();

                return ms.ToArray();
            }
        }

        /// <summary>
        /// AES暗号文を復号化する
        /// </summary>
        /// <param name="key">パスワード</param>
        /// <param name="IV">初期化ベクトル</param>
        /// <param name="value">復号化する文字列</param>
        public static byte[] Decrypt(byte[] key, byte[] IV, byte[] value)
        {
            // エラーチェック
            if (key == null || key.Length <= 0) throw new ArgumentNullException("Keyが不正");
            if (IV == null || IV.Length <= 0) throw new ArgumentNullException("IVが不正");
            if (value == null || value.Length <= 0) throw new ArgumentNullException("valueが不正");

            using (RijndaelManaged aes = new RijndaelManaged())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(key, IV), CryptoStreamMode.Write))
            {
                cs.Write(value, 0, value.Length);
                cs.FlushFinalBlock();
                
                return ms.ToArray();
            }
        }
    }
}