using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Profes.Security.Cryptography
{
    /// <summary>
    /// ハッシュ生成クラス
    /// </summary>
    public static class HashFunction
    {
        /// <summary>
        /// SHA-256アルゴリズムによりハッシュを生成する
        /// </summary>
        /// <param name="value">ハッシュを生成するbyte配列</param>
        public static byte[] HashCreate(byte[] value)
        {
            return SHA256.Create().ComputeHash(value);
        }

        /// <summary>
        /// SHA-256アルゴリズムによりハッシュを生成する
        /// </summary>
        /// <param name="value">ハッシュを生成する文字列</param>
        public static byte[] HashCreate(string value)
        {
            return HashCreate(Encoding.Unicode.GetBytes(value));
        }

        /// <summary>
        /// SHA-256アルゴリズムによりハッシュを生成する
        /// </summary>
        /// <param name="value">ハッシュを生成するIPアドレス</param>
        public static byte[] HashCreate(IPEndPoint value)
        {
            return HashCreate(value.ToString());
        }
    }
}