﻿using System;
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


        private Thread m_PushTimer;
        private int m_PushRecentCount = 0;
        bool m_PushTimerIsContinue;


        private Timer m_PopTimer;


        //Insertを10msごとに10000回くらい実行
        private readonly TimeSpan PushInterval = TimeSpan.Parse(App.Config["PushInterval"]);
        private readonly int PushMaxCount = int.Parse(App.Config["PushMaxCount"]);

        //複数データのGet＆Deleteを10秒ごとに実行開始（「複数データ」の範囲は方式によって違う）
        readonly TimeSpan PopInterval = TimeSpan.Parse(App.Config["PopInterval"]);

        /// <summary>
        /// サーバー送信が成功するかどうかを決める値。変更すると、すぐに次の通信結果に反映される。
        /// </summary>
        public bool IsSuccessServerSend { get; set; } = bool.Parse(App.Config["IsSuccessServerSend"]);

        /// <summary>
        /// サーバー送信に要する時間をイメージした値
        /// </summary>
        readonly TimeSpan ServerSendTime = TimeSpan.Parse(App.Config["ServerSendTime"]);

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


            _trace.Warn($"{nameof(PushInterval)}={PushInterval}");
            _trace.Warn($"{nameof(PushMaxCount)}={PushMaxCount}");
            _trace.Warn($"{nameof(PopInterval)}={PopInterval}");
            _trace.Warn($"{nameof(IsSuccessServerSend)}={IsSuccessServerSend}");
            _trace.Warn($"{nameof(ServerSendTime)}={ServerSendTime}");

            TestPushAllTime = new Stopwatch();
            TestPushAllTime.Start();
            TestPopAllTime = new Stopwatch();
            TestPopAllTime.Start();

            m_PushTimerIsContinue = true;
            m_PushTimer = new Thread(()=>
            {
                while (m_PushTimerIsContinue)
                {
                    WaitWithoutSleep(PushInterval);
                    PushTimeout(null);
                                       
                }
            });
            m_PushTimer.Start();
            m_PopTimer = new Timer(PopTimeout, null, TimeSpan.Zero, PopInterval);

            m_CheckTimer_FileSize = new Timer(CheckTimeout_FileSize, null, TimeSpan.Zero, CheckInterval_FileSize);
            m_CheckTimer_MemorySize = new Timer(CheckTimeout_MemorySize, null, TimeSpan.Zero, CheckInterval_MemorySize);


            _trace.Trace("Push Start");
        }

        /// <summary>
        /// 1スレッドを占有して可能な限り正確なWaitを行う
        /// </summary>
        /// <param name="waitTime"></param>
        void WaitWithoutSleep(TimeSpan waitTime)
        {
            var watch = new Stopwatch();
            watch.Start();
            while(watch.ElapsedTicks < waitTime.Ticks)
            {
                ;
            }
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

        private void PopTimeout(object state)
        {
            int serverSendCount = 10;
            int allCount = (int)Math.Ceiling(ServerSendTime.TotalMilliseconds / PushInterval.TotalMilliseconds);
            var loopCount = Math.Ceiling((decimal) allCount / serverSendCount);

            for (int i = 0; i < loopCount; i++)
            {
                string[] records = m_Target.PopRecords(serverSendCount).ToArray();
                if (!records.Any())
                {
                    break;
                }

                _trace.Trace($"Poped, {string.Join(", ", records)}");
                Thread.Sleep(ServerSendTime);

                if (!IsSuccessServerSend)
                {
                    m_Target.RevertPopRecords();
                    _trace.Trace("Fail, Revert");
                    break;
                }

                m_Target.CommitPopRecords();
                _trace.Trace("Success, Commit");
            }

        }


        Random m_TestPathLengthRandom = new Random();

        private void PushTimeout(object state)
        {
            var testPath = new StringBuilder();
            {
                var testLoopCount = m_TestPathLengthRandom.Next(1, 8);
                for (int i = 0; i < testLoopCount; i++)
                {
                    testPath.Append(Guid.NewGuid().ToString("N"));
                }
            }

            var item = new QueueItem
            {
                ItemTime = DateTimeOffset.Now,
                Path = testPath.ToString()
            };

            var str = JsonConvert.SerializeObject(item);

            m_Target.PushRecord(str);
            

            m_PushRecentCount++;
            _trace.Trace($"Pushed, Count={m_PushRecentCount}");

            if (PushMaxCount == m_PushRecentCount)
            {
                _trace.Trace("Push End");

                m_PushTimerIsContinue = false;
                m_PopTimer.Change(-1, -1);

                Task.Run(() =>
                {
                    _trace.Trace("Complete Thread Start");
                    TestPushAllTime.Stop();
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
