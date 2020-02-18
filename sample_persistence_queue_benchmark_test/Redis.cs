using System;
using System.Collections.Generic;
using System.Text;
using StackExchange.Redis;

namespace sample_persistence_queue_benchmark_test
{

    public class Redis : IDisposable
    {
        private ConnectionMultiplexer _m_Controller;

        public Redis()
        {
        }

        private ConnectionMultiplexer Controller => _m_Controller ?? (_m_Controller = ConnectionMultiplexer.Connect("localhost"));

        private string QueueName = "RedisQueue";

        
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

        public void RevertPopRecords()
        {
            var transactionStack = PopTransactionItems;
            var db = Controller.GetDatabase();

            while (transactionStack.TryPop(out var popItem))
            {
                db.ListLeftPush(QueueName, popItem);
            }
            
        }

        public void Dispose()
        {
            _m_Controller?.Dispose();
        }
    }
}
