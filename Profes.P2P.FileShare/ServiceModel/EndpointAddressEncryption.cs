using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Profes.P2P.FileShare.ServiceModel
{
    public static class EndpointAddressEncryption
    {
        /// <summary>
        /// EndpointAddressを暗号化する
        /// </summary>
        /// <param name="address">暗号化する文字列</param>
        public static string Encrypt(string address)
        {
            byte[] Key = { 36, 16, 27, 156, 54, 132, 44, 123, 237, 18, 111, 83, 99, 213, 253, 252, 180, 174, 118, 215, 204, 61, 130, 23, 114, 110, 32, 140, 120, 90, 213, 78 };
            byte[] IV = { 237, 239, 230, 189, 97, 59, 154, 215, 133, 218, 156, 130, 199, 97, 120, 233 };
            byte[] value = Encoding.Unicode.GetBytes(address);

            using (RijndaelManaged aes = new RijndaelManaged())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(Key, IV), CryptoStreamMode.Write))
            {
                cs.Write(value, 0, value.Length);
                cs.FlushFinalBlock();

                return "☆" + Convert.ToBase64String(ms.ToArray());
            }
        }

        /// <summary>
        /// 暗号化されたEndpointAddressを復号化する
        /// </summary>
        /// <param name="address">復号化する文字列</param>
        public static string Decrypt(string address)
        {
            byte[] Key = null;
            byte[] IV = null;
            byte[] value = null;

            if (address.StartsWith("☆"))
            {
                Key = new byte[] { 36, 16, 27, 156, 54, 132, 44, 123, 237, 18, 111, 83, 99, 213, 253, 252, 180, 174, 118, 215, 204, 61, 130, 23, 114, 110, 32, 140, 120, 90, 213, 78 };
                IV = new byte[] { 237, 239, 230, 189, 97, 59, 154, 215, 133, 218, 156, 130, 199, 97, 120, 233 };
                value = Convert.FromBase64String(address.TrimStart('☆'));
            }

            using (RijndaelManaged aes = new RijndaelManaged())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(Key, IV), CryptoStreamMode.Write))
            {
                cs.Write(value, 0, value.Length);
                cs.FlushFinalBlock();

                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }
    }
}