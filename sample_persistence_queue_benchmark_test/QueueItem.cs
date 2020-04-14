using System;
using System.Collections.Generic;
using System.Text;

namespace sample_persistence_queue_benchmark_test
{
    public class QueueItem
    {
        public DateTimeOffset ItemTime { get; set; }
        public string Path { get; set; }

    }
}
