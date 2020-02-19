using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using sample_persistence_queue_benchmark_test.EF;

namespace sample_persistence_queue_benchmark_test
{
    public class SQLite : IBenchMarkTarget
    {
        public void Initialize()
        {
            using (var db = new EFDBContext())
            {
                db.DBItems.RemoveRange(db.DBItems);

                db.SaveChanges();
            }


        }


        public void PushRecord(string record)
        {
            using (var db = new EFDBContext())
            {
                db.DBItems.Add(new DBItem {data = record});

                db.SaveChanges();
            }
        }

        private DBItem[] m_LastPopRecords;

        public IEnumerable<string> PopRecords(int count)
        {
            using (var db = new EFDBContext())
            {
                var records = db.DBItems.OrderBy(d => d.id).Take(count);

                var returnBuf = records.Select(d => d.data).ToArray();

                db.DBItems.RemoveRange(records);

                m_LastPopRecords = records.ToArray();

                db.SaveChanges();

                return returnBuf;
            }


        }

        public void RevertPopRecords()
        {
            if (m_LastPopRecords == null)
            {
                return;
            }

            using (var db = new EFDBContext())
            {
                //TODO:push-front相当処理の実現方式を検討中
                db.DBItems.AddRange(m_LastPopRecords);
                m_LastPopRecords = null;

                db.SaveChanges();
            }
        }

        public long UseStorageSize { get; }
        public long UseMemorySize { get; }

    }
}
