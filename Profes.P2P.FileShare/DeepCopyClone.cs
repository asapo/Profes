using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Profes.P2P.FileShare
{
    public static class Clone
    {
        /// <summary>
        /// 対象オブジェクトのディープコピークローンを取得します。
        /// </summary>
        /// <typeparam name="T">対象オブジェクトの型</typeparam>
        /// <param name="target">コピー対象オブジェクトを指定します。</param>
        /// <returns>ディープコピーのクローンを返します。</returns>
        /// <remarks>このメソッでディープコピーするには、対象クラスがシリアライズ可能である必要があります。</remarks>
        public static T DeepCopyClone<T>(T target)
        {
            object clone = null;
            using (MemoryStream stream = new MemoryStream())
            {
                //対象オブジェクトをシリアライズ
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, target);
                stream.Position = 0;

                //シリアライズデータをデシリアライズ
                clone = formatter.Deserialize(stream);
            }
            return (T)clone;
        }
    }
}