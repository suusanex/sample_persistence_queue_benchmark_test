using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace sample_persistence_queue_benchmark_test
{
    public class BinaryFile : IBenchMarkTarget
    {
        private string MainFileName = "MainFile.bin";
        private string SendingFileName = "SendingFile.bin";

        object MainFileLock = new object();
        object SendingFileLock = new object();

        private const byte Mark_BeforeSend = 0;
        private const byte Mark_AfterSend = 1;

        //1レコードのデータ構成
        //1byte：Mark
        //4byte：レコードデータのサイズ
        //可変：レコードデータ


        public void Initialize()
        {
            if (File.Exists(MainFileName))
            {
                File.Delete(MainFileName);
            }
            if (File.Exists(SendingFileName))
            {
                File.Delete(SendingFileName);
            }

        }

        public void PushRecord(string record)
        {
            var buf = new List<byte>();

            buf.Add(Mark_BeforeSend);

            var recordByte = Encoding.UTF8.GetBytes(record);

            buf.AddRange(BitConverter.GetBytes(recordByte.Length));

            buf.AddRange(recordByte);

            lock (MainFileLock)
            {
                using (var file = new FileStream(MainFileName, FileMode.Append, FileAccess.Write, FileShare.None))
                {
                    file.Write(buf.ToArray());
                }
            }
        }

        private long SendingFileCursor;

        List<long> LastPopRecordPositions = new List<long>();

        public IEnumerable<string> PopRecords(int count)
        {
            var returnBuf = new List<string>();

            LastPopRecordPositions.Clear();

            lock (SendingFileLock)
            {
                //送信用バッファファイルが既に末尾に到達している場合、削除（末尾に達したとしてもRevertされる可能性があるため、次のPopが発生した時点で削除する）
                {
                    var fileInfo = new FileInfo(SendingFileName);
                    if (fileInfo.Exists)
                    {
                        if (fileInfo.Length <= SendingFileCursor)
                        {
                            File.Delete(SendingFileName);
                            SendingFileCursor = 0;
                        }
                    }
                }

                //送信用バッファファイルが無い場合、現在のキューファイルを送信用バッファファイルへ移動。現在のキューファイルも無ければレコード無しを返す
                if (!File.Exists(SendingFileName))
                {
                    lock (MainFileLock)
                    {
                        if (!File.Exists(MainFileName))
                        {
                            return returnBuf;
                        }

                        File.Move(MainFileName, SendingFileName);
                        SendingFileCursor = 0;
                    }
                }

                //ファイル末尾に到達するか、もしくは指定された個数を読み終えるまで、ファイルを読み込む。送信済みマークがついているレコードは無視する。
                using (var file = new FileStream(SendingFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    if (SendingFileCursor != 0)
                    {
                        file.Seek(SendingFileCursor, SeekOrigin.Begin);                       
                    }

                    while (returnBuf.Count < count)
                    {
                        var recordPosition = file.Position;

                        var mark = file.ReadByte();
                        if (mark == -1)
                        {
                            break;
                        }

                        //未処理のレコードを見つけたら、Revert用にポジションを記録したうえで処理済みに書き換え、読み込む。処理済みのレコードはスキップする。
                        if (mark == Mark_BeforeSend)
                        {
                            file.Seek(-1 * sizeof(byte), SeekOrigin.Current);
                            file.WriteByte(Mark_AfterSend);
                        }
                        LastPopRecordPositions.Add(recordPosition);

                        var recordSizeBuf = new byte[4];
                        file.Read(recordSizeBuf, 0, recordSizeBuf.Length);
                        var recordSize = BitConverter.ToInt32(recordSizeBuf);

                        if (mark == Mark_AfterSend)
                        {
                            file.Seek(recordSize, SeekOrigin.Current);
                            continue;
                        }
                        var recordBuf = new byte[recordSize];
                        file.Read(recordBuf, 0, recordSize);
                        returnBuf.Add(Encoding.UTF8.GetString(recordBuf));

                    }

                    SendingFileCursor = file.Position;
                   
                }

            }

            return returnBuf;
        }

        public void RevertPopRecords()
        {
            //Revert用に記録したポジションのレコードを、送信前状態へ書き換える。また、ファイル読み込みのカーソルを、Revertしたポジションの先頭まで戻す。

            if (!LastPopRecordPositions.Any())
            {
                return;
            }

            lock (SendingFileLock)
            {

                using (var file = new FileStream(SendingFileName, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    SendingFileCursor = LastPopRecordPositions.First();

                    foreach (var lastPopRecordPosition in LastPopRecordPositions)
                    {
                        file.Seek(lastPopRecordPosition, SeekOrigin.Begin);

                        file.WriteByte(Mark_BeforeSend);
                    }
                }
            }

            LastPopRecordPositions.Clear();

        }

        public void CommitPopRecords()
        {
        }

        public long UseStorageSize
        {
            get
            {
                var fileInfos = new[] {new FileInfo(MainFileName), new FileInfo(SendingFileName)};
                var fileSizes = fileInfos.Select(d => d.Exists ? d.Length : 0);
                return fileSizes.Sum();
            }
        }

        public long UseMemorySize => Environment.WorkingSet;
        public long FinalStorageSize => UseStorageSize;
    }
}
