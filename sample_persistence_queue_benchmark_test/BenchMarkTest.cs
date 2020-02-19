using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace sample_persistence_queue_benchmark_test
{
    public class BenchMarkTest
    {
        public BenchMarkTest(IBenchMarkTarget target)
        {
            m_Target = target;
        }


        public event Action OnTestEnd;


        Stopwatch TestPushAllTime;
        Stopwatch TestPopAllTime;

        //TODO:使用するメモリ量
        //TODO:使用するディスクサイズ


        private Timer m_PushTimer;
        private int m_PushRecentCount = 0;


        private Timer m_PopTimer;


        //Insertを10msごとに10000回くらい実行
        readonly TimeSpan PushInterval = new TimeSpan(0, 0, 0, 0, 10);
        private readonly int PushMaxCount = 10000;

        //複数データのGet＆Deleteを10秒ごとに実行開始（「複数データ」の範囲は方式によって違う）
        readonly TimeSpan PopInterval = new TimeSpan(0, 0, 10);

        /// <summary>
        /// サーバー送信が成功するかどうかを決める値。変更すると、すぐに次の通信結果に反映される。
        /// </summary>
        public bool IsSuccessServerSend { get; set; } = false;

        /// <summary>
        /// サーバー送信に要する時間をイメージした値
        /// </summary>
        readonly TimeSpan ServerSendTime = new TimeSpan(0, 0, 0, 0, 100);

        private readonly Logger _trace = LogManager.GetCurrentClassLogger();
        private IBenchMarkTarget m_Target;

        readonly TimeSpan CheckInterval_FileSize = new TimeSpan(0, 0, 3);
        private Timer m_CheckTimer_FileSize;
        readonly TimeSpan CheckInterval_MemorySize = new TimeSpan(0, 0, 3);
        private Timer m_CheckTimer_MemorySize;

        public void TestRunAsync()
        {
            if (m_PushTimer != null)
            {
                return;
            }

            m_Target.Initialize();
            _trace.Trace("Initialized");

            TestPushAllTime = new Stopwatch();
            TestPushAllTime.Start();
            TestPopAllTime = new Stopwatch();
            TestPopAllTime.Start();

            m_PushTimer = new Timer(PushTimeout, null, TimeSpan.Zero, PushInterval);
            m_PopTimer = new Timer(PopTimeout, null, TimeSpan.Zero, PopInterval);

            m_CheckTimer_FileSize = new Timer(CheckTimeout_FileSize, null, TimeSpan.Zero, CheckInterval_FileSize);
            m_CheckTimer_MemorySize = new Timer(CheckTimeout_MemorySize, null, TimeSpan.Zero, CheckInterval_MemorySize);


            _trace.Trace("Push Start");
        }

        private long MaxMemorySize;

        private void CheckTimeout_MemorySize(object state)
        {
            var nowSize = m_Target.UseMemorySize;
            if (MaxMemorySize < nowSize)
            {
                MaxMemorySize = nowSize;
            }
        }


        private long MaxFileSize;

        private void CheckTimeout_FileSize(object state)
        {
            var nowSize = m_Target.UseStorageSize;
            if (MaxFileSize < nowSize)
            {
                MaxFileSize = nowSize;
            }
        }

        private void PopTimeout(object isLast)
        {
            int count = (int)Math.Ceiling(ServerSendTime.TotalMilliseconds / PushInterval.TotalMilliseconds);
            var records = m_Target.PopRecords(count).ToArray();

            _trace.Trace($"Poped, {string.Join(", ", records)}");
            Thread.Sleep(ServerSendTime);

            if (!IsSuccessServerSend)
            {
                m_Target.RevertPopRecords();
            }
            

        }



        private void PushTimeout(object state)
        {
            var item = new QueueItem
            {
                ItemTime = DateTimeOffset.Now,
                Path =
                    "TestPath0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789"
            };

            var str = JsonConvert.SerializeObject(item);

            m_Target.PushRecord(str);
            

            m_PushRecentCount++;
            _trace.Trace($"Pushed, Count={m_PushRecentCount}");

            if (PushMaxCount == m_PushRecentCount)
            {
                _trace.Trace("Push End");

                m_PushTimer.Change(-1, -1);
                m_PopTimer.Change(-1, -1);

                Task.Run(() =>
                {
                    _trace.Trace("Complete Thread Start");
                    TestPushAllTime.Stop();
                    m_PushTimer.Dispose();
                    m_PushTimer = null;
                    m_PopTimer.Dispose();
                    m_PopTimer = null;
                    PopTimeout(null);
                    TestPopAllTime.Stop();
                    m_CheckTimer_FileSize.Dispose();
                    m_CheckTimer_FileSize = null;
                    m_CheckTimer_MemorySize.Dispose();
                    m_CheckTimer_MemorySize = null;

                    _trace.Warn($"{nameof(TestPushAllTime)}={TestPushAllTime.Elapsed}");
                    _trace.Warn($"{nameof(TestPopAllTime)}={TestPopAllTime.Elapsed}");
                    _trace.Warn($"{nameof(MaxFileSize)}={MaxFileSize}");
                    _trace.Warn($"{nameof(MaxMemorySize)}={MaxMemorySize}");

                    OnTestEnd?.Invoke();

                });
            }
        }


    }
}
