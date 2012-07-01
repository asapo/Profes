using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.ServiceModel;

namespace Profes.BinaryEditorBase
{
    /// <summary>
    /// バイナリエディターサービスを提供します
    /// </summary>
    public static class BinaryEditor
    {
        /// <summary>
        /// バイト列を16進数表記の文字列に変換
        /// </summary>
        /// <param name="bytes">文字列に変換するバイト配列</param>
        /// <returns>変換された文字列</returns>
        public static string BytesToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// 16進数表記の文字列をバイト列に変換
        /// </summary>
        /// <param name="byteString">バイト配列に変換する文字列</param>
        /// <returns>変換されたバイト配列</returns>
        public static byte[] HexStringToBytes(string byteString)
        {
            // 文字列の文字数(半角)が奇数の場合、頭に「0」を付ける
            int length = byteString.Length;
            if (length % 2 == 1)
            {
                byteString = "0" + byteString;
                length++;
            }

            List<byte> data = new List<byte>();

            for (int i = 0; i < length - 1; i = i + 2)
            {
                // 16進数表記の文字列かどうかをチェック
                string buf = byteString.Substring(i, 2);
                if (Regex.IsMatch(buf, @"^[0-9a-fA-F]{2}$"))
                {
                    data.Add(Convert.ToByte(buf, 16));
                }
                // 16進数表記で無ければ「00」とする
                else
                {
                    data.Add(Convert.ToByte("00", 16));
                }
            }

            return data.ToArray();
        }

        /// <summary>
        /// 配列を比較する
        /// </summary>
        public static bool ArrayEquals(Array b1, Array b2)
        {
            if (b1.Length != b2.Length)
                return false;
            for (int i = 0; i < b1.Length; i++)
            {
                if (!b1.GetValue(i).Equals(b2.GetValue(i)))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 指定したbyte配列を比較し、これらの相対値を示す値を返します。 
        /// </summary>
        /// <param name="b1"></param>
        /// <param name="b2"></param>
        /// <returns>b1が小さい場合は0以下、b1とb2が等価の場合0、b1が大きい場合0以上</returns>
        public static int ByteArraryCompare(byte[] b1, byte[] b2)
        {
            if (b1.Length < b2.Length) return -1;
            else if (b1.Length > b2.Length) return 1;

            for (int i = 0; i < b1.Length; i++)
            {
                if (b1[i] < b2[i]) return -1;
                else if (b1[i] > b2[i]) return 1;
            }

            return 0;
        }
    }
}