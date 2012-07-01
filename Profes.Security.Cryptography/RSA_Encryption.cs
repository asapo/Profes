using System;
using System.Security.Cryptography;
using System.Diagnostics;

namespace Profes.Security.Cryptography
{
    /// <summary>
    /// RSA暗号化クラス
    /// </summary>
    public static class RSA_Encryption
    {
        /// <summary>
        /// 公開鍵と秘密鍵を作成して返す
        /// </summary>
        /// <param name="publicKey">作成された公開鍵(XML形式)</param>
        /// <param name="privateKey">作成された秘密鍵(XML形式)</param>
        public static void CreateKeys(out string publicKey, out string privateKey)
        {
            CspParameters CSPParam = new CspParameters();
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048, CSPParam))
            {
                CSPParam.Flags = CspProviderFlags.UseMachineKeyStore;

                publicKey = rsa.ToXmlString(false);
                privateKey = rsa.ToXmlString(true);
            }
        }

        /// <summary>
        /// 公開鍵を使って文字列を暗号化する
        /// </summary>
        /// <param name="value">暗号化する文字列</param>
        /// <param name="publicKey">暗号化に使用する公開鍵(XML形式)</param>
        public static byte[] Encrypt(byte[] value, string publicKey)
        {
            // エラーチェック
            if (value == null || value.Length <= 0) throw new ArgumentNullException("valueが不正");
            if (publicKey == null || publicKey.Length <= 0) throw new ArgumentNullException("publicKeyが不正");

            CspParameters CSPParam = new CspParameters();
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(CSPParam))
            {
                CSPParam.Flags = CspProviderFlags.UseMachineKeyStore;
                rsa.FromXmlString(publicKey);

                return rsa.Encrypt(value, true);
            }
        }

        /// <summary>
        /// 秘密鍵を使って文字列を復号化する
        /// </summary>
        /// <param name="value">Encryptメソッドにより暗号化された文字列</param>
        /// <param name="privateKey">復号化に必要な秘密鍵(XML形式)</param>
        public static byte[] Decrypt(byte[] value, string privateKey)
        {
            // エラーチェック
            if (value == null || value.Length <= 0) throw new ArgumentNullException("valueが不正");
            if (privateKey == null || privateKey.Length <= 0) throw new ArgumentNullException("privateKeyが不正");

            CspParameters CSPParam = new CspParameters();
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(CSPParam))
            {
                CSPParam.Flags = CspProviderFlags.UseMachineKeyStore;
                rsa.FromXmlString(privateKey);

                return rsa.Decrypt(value, true);
            }
        }
    }
}