using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using NLog;

namespace sample_persistence_queue_benchmark_test
{
    public class Kafka : IBenchMarkTarget
    {
        public void Initialize()
        {
            using (var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = m_ProducerConfig.BootstrapServers }).Build())
            {
                adminClient.DeleteTopicsAsync(new [] { TopicName }, null);
            }

        }

        private ProducerConfig m_ProducerConfig = new ProducerConfig()
        {
            BootstrapServers = "localhost:9092"
        };

        private readonly string TopicName = "QueueTopic";

        private readonly Logger _trace = LogManager.GetCurrentClassLogger();

        public void PushRecord(string record)
        {
            using (var producer = new ProducerBuilder<Null, string>(m_ProducerConfig).Build())
            {
                producer.Produce(TopicName, new Message<Null, string>{ Value = record}, ProducerDeliveryHandler);
            }
        }

        private void ProducerDeliveryHandler(DeliveryReport<Null, string> obj)
        {
            if (obj.Error?.IsError ?? false)
            {
                _trace.Warn($"Produce Error, {obj.Error.Code},{obj.Error.Reason},{obj.Error}");
            }
        }


        private ConsumerConfig m_ConsumerConfig = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092",
            GroupId = "QueueConsumer",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };


        public IEnumerable<string> PopRecords(int count)
        {
            var returnBuf = new List<string>();

            using (var consumer = new ConsumerBuilder<Ignore, string>(m_ConsumerConfig).Build())
            {
                try
                {
                    consumer.Subscribe(new[] {TopicName});
                    for (int i = 0; i < count; i++)
                    {
                        var consumeResult = consumer.Consume(new TimeSpan(0, 0, 0, 0, 100));
                        if (consumeResult == null)
                        {
                            break;
                        }
                        returnBuf.Add(consumeResult.Value);

                    }

                }
                catch (ConsumeException e)
                {
                    _trace.Warn($"{e.Message}");
                }
                finally
                {
                    consumer.Close();
                }
            }

            return returnBuf;
        }

        public void RevertPopRecords()
        {
            throw new NotImplementedException();
        }

        public long UseStorageSize { get; } = 0;
        public long UseMemorySize { get; } = 0;
    }
}
