using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using StackExchange.Redis;

namespace sample_persistence_queue_benchmark_test
{

    public class Redis : IDisposable, IBenchMarkTarget
    {
        private ConnectionMultiplexer _m_Controller;

        public Redis()
        {
        }

        private ConnectionMultiplexer Controller => _m_Controller ?? (_m_Controller = ConnectionMultiplexer.Connect(_ServerUrl));

        private string QueueName = "RedisQueue";


        public void Initialize()
        {
            var db = Controller.GetDatabase();
            db.KeyDelete(QueueName);
        }

        public void PushRecord(string record)
        {
            var db = Controller.GetDatabase();

            db.ListRightPush(QueueName, record);

        }

        public IEnumerable<string> PopRecords(int count)
        {
            var buf = new List<string>();
            var transactionStack = new Stack<RedisValue>();
            
            var db = Controller.GetDatabase();

            for (int i = 0; i < count; i++)
            {
                var popItem = db.ListLeftPop(QueueName);
                if (!popItem.HasValue)
                {
                    break;
                }

                buf.Add(popItem);
                transactionStack.Push(popItem);                
            }

            PopTransactionItems = transactionStack;

            return buf;
        }

        Stack<RedisValue> PopTransactionItems = new Stack<RedisValue>();
        private string _ServerUrl = "localhost";

        public void RevertPopRecords()
        {
            var transactionStack = PopTransactionItems;
            var db = Controller.GetDatabase();

            while (transactionStack.TryPop(out var popItem))
            {
                db.ListLeftPush(QueueName, popItem);
            }
            
        }

        private static readonly string RedisInstallFolderPath = App.Config["RedisInstallFolderPath"];
        private string AOFFilePath => Path.Combine(RedisInstallFolderPath, "appendonly.aof");
        private string RDBFilePath => Path.Combine(RedisInstallFolderPath, "dump.rdb");

        public long UseStorageSize
        {
            get
            {
                var fileInfos = new[] { new FileInfo(AOFFilePath), new FileInfo(RDBFilePath) };
                var fileSizes = fileInfos.Select(d => d.Exists ? d.Length : 0);
                return fileSizes.Sum();
            }
        }

        public long UseMemorySize => Environment.WorkingSet + m_RedisServerProcess.WorkingSet64;
        public long FinalStorageSize => UseStorageSize;

        private Process m_RedisServerProcess = Process.GetProcessesByName("redis-server").FirstOrDefault();

        public void Dispose()
        {
            m_RedisServerProcess?.Dispose();
            m_RedisServerProcess = null;
            _m_Controller?.Dispose();
            _m_Controller = null;
        }

        public void CommitPopRecords()
        {
        }
    }
}
