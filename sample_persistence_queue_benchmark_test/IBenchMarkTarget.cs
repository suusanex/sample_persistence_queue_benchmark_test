using System.Collections.Generic;

namespace sample_persistence_queue_benchmark_test
{
    public interface IBenchMarkTarget
    {
        /// <summary>
        /// 初期化する。前回に残ったデータの削除などを行う。
        /// </summary>
        void Initialize();

        /// <summary>
        /// 指定したレコードをキューにプッシュ
        /// </summary>
        void PushRecord(string record);
        /// <summary>
        /// 指定した個数のレコードをキューから取得
        /// </summary>
        IEnumerable<string> PopRecords(int count);
        /// <summary>
        /// 最後にPopRecordsで取得したレコードを、キューに戻す
        /// </summary>
        void RevertPopRecords();

        /// <summary>
        /// 現在使用中のストレージサイズを返す。
        /// </summary>
        long UseStorageSize { get; }

        /// <summary>
        /// 現在使用中のメモリサイズを返す。
        /// </summary>
        long UseMemorySize { get; }
    }
}