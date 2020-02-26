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
        /// 指定した個数のレコードをキューから取得（この時点でのデータの状態は実装依存。このあとにRevertとCommitのどちらも呼び出し可能で、それらを呼び出した後の状態は保証される。）
        /// </summary>
        IEnumerable<string> PopRecords(int count);
        /// <summary>
        /// 最後にPopRecordsで取得したレコードを、キューに戻す
        /// </summary>
        void RevertPopRecords();
    
        /// <summary>
        /// 最後にPopRecordsで取得したレコードを、確定する（戻せなくする）
        /// </summary>
        void CommitPopRecords();

        /// <summary>
        /// 現在使用中のストレージサイズを返す。
        /// </summary>
        long UseStorageSize { get; }

        /// <summary>
        /// 現在使用中のメモリサイズを返す。
        /// </summary>
        long UseMemorySize { get; }

        /// <summary>
        /// 最終的に使用しているストレージサイズを返す。（キャッシュの反映などを行ってから取得する）
        /// </summary>
        long FinalStorageSize { get; }

    }
}