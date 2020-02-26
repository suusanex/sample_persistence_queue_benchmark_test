using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using sample_persistence_queue_benchmark_test.MongoDBAccess;

namespace sample_persistence_queue_benchmark_test
{
    public class MongoDB : IBenchMarkTarget
    {
        private string DBFolderPath => m_Config.Config.GetSection("DBItemstoreDatabaseSettings")
            .GetValue<string>("FolderPath");


        public long UseStorageSize => Utility.GetDirectorySize(new DirectoryInfo(DBFolderPath));


        public long UseMemorySize => Environment.WorkingSet + m_MongoDBServerProcess.WorkingSet64;
        public long FinalStorageSize
        {
            get
            {
                //ジャーナル反映の最大間隔60秒までを待機
                Thread.Sleep(new TimeSpan(0,1,0));
                return UseStorageSize;
            }
        }

        private Process m_MongoDBServerProcess = Process.GetProcessesByName("mongod").FirstOrDefault();

        AppConfig m_Config = new AppConfig();
        private MongoClient m_Client;
        private IMongoDatabase m_DB;
        private IMongoCollection<DBItem> m_Queue;
        private IMongoCollection<DBItem> m_BackupQueue;

        public void Initialize()
        {
            m_Client = new MongoClient(m_Config.Config.GetSection("DBItemstoreDatabaseSettings").GetValue<string>("ConnectionString"));
            m_DB = m_Client.GetDatabase(m_Config.Config.GetSection("DBItemstoreDatabaseSettings").GetValue<string>("DatabaseName"));
            var collectionName = m_Config.Config.GetSection("DBItemstoreDatabaseSettings").GetValue<string>("DBItemsCollectionName");

            CollectionDrop(collectionName);
            m_Queue = m_DB.GetCollection<DBItem>(collectionName);

            m_BackupCollectionName = collectionName + "_Backup";
            CollectionDrop(m_BackupCollectionName);
            m_BackupQueue = m_DB.GetCollection<DBItem>(m_BackupCollectionName);
        }

        private void CollectionDrop(string collectionName)
        {
            var queue = m_DB.GetCollection<DBItem>(collectionName);
            if (queue.CountDocuments(FilterDefinition<DBItem>.Empty) != 0)
            {
                m_DB.DropCollection(collectionName);
            }
        }

        public IEnumerable<string> PopRecords(int count)
        {
            //まずバックアップキューから取り出し、尽きたら通常のキューから取り出す。
            //取り出したものを全てバックアップキューへ入れる。
            var returnBuf = new List<string>();
            var popItemBuf = new List<DBItem>();
            var targetQueue = m_BackupQueue;
            for (int i = 0; i < count; i++)
            {
                var popItem =
                    targetQueue.FindOneAndDelete(FilterDefinition<DBItem>.Empty,
                        new FindOneAndDeleteOptions<DBItem, DBItem>() { Sort = Builders<DBItem>.Sort.Ascending(d => d.Id), MaxTime = new TimeSpan(0,0,0,10)});
                if (popItem == null)
                {
                    if (targetQueue == m_Queue)
                    {
                        break;
                    }

                    targetQueue = m_Queue;
                    popItem =
                        targetQueue.FindOneAndDelete(FilterDefinition<DBItem>.Empty,
                            new FindOneAndDeleteOptions<DBItem, DBItem>() { Sort = Builders<DBItem>.Sort.Ascending(d => d.Id), MaxTime = new TimeSpan(0, 0, 0, 10) });
                    if (popItem == null)
                    {
                        break;
                    }
                }
                
                returnBuf.Add(popItem.Data);
                popItemBuf.Add(popItem);
            }

            if (popItemBuf.Any())
            {
                m_BackupQueue.InsertMany(popItemBuf);
            }

            return returnBuf;
        }

        private string m_BackupCollectionName;

        public void PushRecord(string record)
        {
            m_Queue.InsertOne(new DBItem {Data = record});
        }

        public void CommitPopRecords()
        {
            while (m_BackupQueue.FindOneAndDelete(FilterDefinition<DBItem>.Empty) != null)
            {

            }
        }

        public void RevertPopRecords()
        {
            //次回取り出し時にBackupのキューから取り出す事で実現できているので、何もする必要がない
        }


    }
}
