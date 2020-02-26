using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace sample_persistence_queue_benchmark_test
{
    public class BinaryFileType2 : IBenchMarkTarget
    {
        private string FileNamePrefix = "LogFile_";
        private string FileNameExt = ".bin";
        private string FileDirName = "BinaryFileType2";

        const int OneFileRecordCount = 10;
        int TailFileRecordIndex;

        /// <summary>
        /// キューのHeadにあたるファイルのインデックス。送信済みでRevertもしくはCommitを待機しているファイルは、キューに含まない（Headは送信前データを指す）
        /// </summary>
        int FileIndexHead;
        /// <summary>
        /// キューのTailにあたるファイルのインデックス。
        /// </summary>
        int FileIndexTail;
        /// <summary>
        /// 送信済みで、RevertもしくはCommitを待機しているファイルのインデックス。無い場合は-1。
        /// </summary>
        private int SendingFileIndex = -1;

        object FileAndIndexLock = new object();


        //1レコードのデータ構成
        //4byte：レコードデータのサイズ
        //可変：レコードデータ


        public void Initialize()
        {
            if (Directory.Exists(FileDirName))
            {
                Directory.Delete(FileDirName, true);
            }

            Directory.CreateDirectory(FileDirName);

        }

        public void PushRecord(string record)
        {
            var buf = new List<byte>();

            var recordByte = Encoding.UTF8.GetBytes(record);

            buf.AddRange(BitConverter.GetBytes(recordByte.Length));

            buf.AddRange(recordByte);

            lock (FileAndIndexLock)
            {
                string targetFileName = $@"{FileDirName}\{FileNamePrefix}{FileIndexTail:X8}{FileNameExt}";

                using (var file = new FileStream(targetFileName, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    file.Write(buf.ToArray());
                }
                
                if(OneFileRecordCount <= TailFileRecordIndex + 1){
                    
                    FileIndexTail++;

                    TailFileRecordIndex = 0;
                }
                else{    
                    TailFileRecordIndex++;
                }
            }

        }


        public IEnumerable<string> PopRecords(int count)
        {
            if (count != OneFileRecordCount)
            {
                throw new NotSupportedException("本クラスの方式は、特定個数（1ファイルのレコード数に対応する）の取り出し以外はサポートしない");
            }

            var returnBuf = new List<string>();

            lock (FileAndIndexLock)
            {

                string targetFileName = $@"{FileDirName}\{FileNamePrefix}{FileIndexHead:X8}{FileNameExt}";

                //Headに対応するファイルが無い場合、空なので終了
                if (!File.Exists(targetFileName))
                {
                    return new string[0];
                }

                //HeadとTailが同一の場合、Tailを進める（最大値に満たなくても送信対象とする）
                if(FileIndexHead == FileIndexTail){
                    FileIndexTail++;
                    TailFileRecordIndex = 0;
                }


                //ファイル末尾に到達するまで、ファイルを読み込む。
                using (var file = new FileStream(targetFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    while (returnBuf.Count < count && file.Position < file.Length)
                    {
                        var recordSizeBuf = new byte[4];
                        file.Read(recordSizeBuf, 0, recordSizeBuf.Length);
                        var recordSize = BitConverter.ToInt32(recordSizeBuf);

                        var recordBuf = new byte[recordSize];
                        file.Read(recordBuf, 0, recordSize);
                        returnBuf.Add(Encoding.UTF8.GetString(recordBuf));
                    }
                }

                SendingFileIndex = FileIndexHead;
                FileIndexHead++;

            }

            return returnBuf;
        }

        public void RevertPopRecords()
        {

            lock (FileAndIndexLock)
            {
                FileIndexHead = SendingFileIndex;
            }

        }

        public void CommitPopRecords()
        {
            lock (FileAndIndexLock)
            {
                if (SendingFileIndex == -1)
                {
                    return;
                }

                string targetFileName = $@"{FileDirName}\{FileNamePrefix}{SendingFileIndex:X8}{FileNameExt}";

                //送信済みの待機ファイルがあれば削除
                if (File.Exists(targetFileName))
                {
                    SendingFileIndex = -1;
                    File.Delete(targetFileName);
                }

            }
        }

        public long UseStorageSize => Utility.GetDirectorySize(new DirectoryInfo(FileDirName));

        public long UseMemorySize => Environment.WorkingSet;
        public long FinalStorageSize => UseStorageSize;
    }
}
