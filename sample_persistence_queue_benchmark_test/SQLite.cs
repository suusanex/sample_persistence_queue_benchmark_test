using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.EntityFrameworkCore;
using sample_persistence_queue_benchmark_test.EF;
using NLog;

namespace sample_persistence_queue_benchmark_test
{
    public class SQLite : IBenchMarkTarget
    {
        private readonly Logger _trace = LogManager.GetCurrentClassLogger();
        public void Initialize()
        {
            while (true)
            {
                try
                {
                    using (var db = new EFDBContext())
                    {
                        db.DBItems.RemoveRange(db.DBItems);

                        db.SaveChanges();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    _trace.Warn("DbUpdateConcurrencyException");
                    continue;
                }

                break;
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
            while (true)
            {
                try
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
                catch (DbUpdateConcurrencyException)
                {
                    _trace.Warn("DbUpdateConcurrencyException");
                    continue;
                }
            }


        }

        public void RevertPopRecords()
        {
            if (m_LastPopRecords == null)
            {
                return;
            }

            while (true)
            {
                try
                {
                    using (var db = new EFDBContext())
                    {
                        //TODO:push-front相当処理の実現方式を検討中
                        db.DBItems.AddRange(m_LastPopRecords);
                        m_LastPopRecords = null;

                        db.SaveChanges();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    _trace.Warn("DbUpdateConcurrencyException");
                    continue;
                }

                break;
            }
        }

        public long UseStorageSize
        {
            get
            {
                var fileInfos = new[] { new FileInfo("SQLite.db") };
                var fileSizes = fileInfos.Select(d => d.Exists ? d.Length : 0);
                return fileSizes.Sum();
            }
        }
        public long UseMemorySize => Environment.WorkingSet;

    }
}
