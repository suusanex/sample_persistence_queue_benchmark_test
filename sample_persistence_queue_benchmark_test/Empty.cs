using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Concurrent;

namespace sample_persistence_queue_benchmark_test
{
    public class Empty : IBenchMarkTarget
    {
        public long UseStorageSize => 0;

        public long UseMemorySize => Environment.WorkingSet;

        public void Initialize()
        {
        }

        ConcurrentQueue<string> Queue = new ConcurrentQueue<string>();

        string[] LastPopRecords;

        public IEnumerable<string> PopRecords(int count)
        {
            var buf = new List<string>();

            for (int i = 0; i < count; i++)
            {
                if(Queue.TryDequeue(out var record))
                {
                    buf.Add(record);
                }
                else
                {
                    break;
                }
            }

            LastPopRecords = buf.ToArray();

            return buf;
            
        }

        public void PushRecord(string record)
        {
            Queue.Enqueue(record);
        }

        public void RevertPopRecords()
        {
            if(LastPopRecords == null)
            {
                return;
            }

            foreach (var item in LastPopRecords)
            {
                //TODO:push-front相当処理の実現方式を検討中
                Queue.Enqueue(item);
            }



        }
    }
}
