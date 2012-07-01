using System.Security.Cryptography;
using System.IO;

namespace Profes.Security.Cryptography
{
    /// <summary>
    /// デジタル署名のためのメソッドを提供します
    /// </summary>
    public static class DigitalSignature
    {
        /// <summary>
        /// デジタル署名を作成する
        /// </summary>
        /// <param name="message">署名を付けるメッセージ</param>
        /// <param name="privateKey">署名に使用する秘密鍵</param>
        /// <returns>作成された署名</returns>
        public static byte[] CreateDigitalSignature_SHA1(byte[] message, string privateKey)
        {
            byte[] hashData = SHA1.Create().ComputeHash(message);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);

                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                rsaFormatter.SetHashAlgorithm("SHA1");
                return rsaFormatter.CreateSignature(hashData);
            }
        }

        /// <summary>
        /// デジタル署名を検証する
        /// </summary>
        /// <param name="message">署名の付いたメッセージ</param>
        /// <param name="signature">署名</param>
        /// <param name="publicKey">送信者の公開鍵</param>
        /// <returns>認証に成功した時はTrue。失敗した時はFalse。</returns>
        public static bool VerifyDigitalSignature_SHA1(byte[] message, byte[] signature, string publicKey)
        {
            byte[] hashData = SHA1.Create().ComputeHash(message);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);

                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA1");
                return rsaDeformatter.VerifySignature(hashData, signature);
            }
        }

        /// <summary>
        /// デジタル署名を作成する
        /// </summary>
        /// <param name="message">署名を付けるメッセージ</param>
        /// <param name="privateKey">署名に使用する秘密鍵</param>
        /// <returns>作成された署名</returns>
        public static byte[] CreateDigitalSignature(byte[] message, string privateKey)
        {
            byte[] hashData = SHA256.Create().ComputeHash(message);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(privateKey);

                RSAPKCS1SignatureFormatter rsaFormatter = new RSAPKCS1SignatureFormatter(rsa);
                rsaFormatter.SetHashAlgorithm("SHA256");
                return rsaFormatter.CreateSignature(hashData);
            }
        }

        /// <summary>
        /// デジタル署名を検証する
        /// </summary>
        /// <param name="message">署名の付いたメッセージ</param>
        /// <param name="signature">署名</param>
        /// <param name="publicKey">送信者の公開鍵</param>
        /// <returns>認証に成功した時はTrue。失敗した時はFalse。</returns>
        public static bool VerifyDigitalSignature(byte[] message, byte[] signature, string publicKey)
        {
            byte[] hashData = SHA256.Create().ComputeHash(message);

            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                rsa.FromXmlString(publicKey);

                RSAPKCS1SignatureDeformatter rsaDeformatter = new RSAPKCS1SignatureDeformatter(rsa);
                rsaDeformatter.SetHashAlgorithm("SHA256");
                return rsaDeformatter.VerifySignature(hashData, signature);
            }
        }

        /// <summary>
        /// 公開鍵と秘密鍵を作成して返す
        /// </summary>
        /// <param name="publicKey">作成された公開鍵(XML形式)</param>
        /// <param name="privateKey">作成された秘密鍵(XML形式)</param>
        public static void CreateKeys(out string publicKey, out string privateKey)
        {
            CspParameters CSPParam = new CspParameters();
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(CSPParam))
            {
                CSPParam.Flags = CspProviderFlags.UseMachineKeyStore;
                
                publicKey = rsa.ToXmlString(false);
                privateKey = rsa.ToXmlString(true);
            }
        }
    }
}