using System.ServiceModel;
using System;

namespace Profes.P2P.FileShare.ServiceModel
{
    /// <summary>
    /// ファイル共有サービスを提供します
    /// </summary>
    [ServiceContract]
    public interface IFileShareService
    {
        /// <summary>
        /// Key情報リストを設定します
        /// </summary>
        /// <param name="item">Key情報リスト</param>
        [OperationContract(IsOneWay = true)]
        void Store(Key[] item);

        /// <summary>
        /// keyにXOR距離が近いノードを取得します
        /// </summary>
        /// <param name="hash">取得したいキャッシュのハッシュ</param>
        /// <returns>ノードリストを返す</returns>
        [OperationContract]
        Node[] GetRouteTable(byte[] hash);

        /// <summary>
        /// カテゴリに一致するキャッシュヘッダリストを取得する
        /// </summary>
        /// <param name="category">カテゴリ</param>
        /// <returns>キャッシュヘッダリスト情報を返す</returns>
        [OperationContract]
        Key[] GetCategoryKey(string category);

        /// <summary>
        /// キャッシュヘッダを取得します
        /// </summary>
        /// <param name="hash">キャッシュのハッシュ</param>
        /// <returns>キャッシュブロックのビットマップを返す</returns>
        [OperationContract]
        byte[][] GetCacheBlockHashList(byte[] Hash);

        /// <summary>
        /// キャッシュブロックを取得します
        /// </summary>
        /// <param name="hash">キャッシュのハッシュ</param>
        /// <param name="index">インデックス</param>
        /// <returns>キャッシュブロックを返す</returns>
        [OperationContract]
        byte[] GetCacheBlock(byte[] hash, int index);

        /// <summary>
        /// キャッシュブロックを設定します
        /// </summary>
        /// <param name="cache">キャッシュヘッダ</param>
        /// <param name="value">キャッシュブロック</param>
        /// <param name="count">拡散回数</param>
        [OperationContract(IsOneWay = true)]
        void SetCacheBlock(Cache cache, byte[] value, DateTime CreateTime);
    }
}