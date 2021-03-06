﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using NLog;

namespace sample_persistence_queue_benchmark_test
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        internal class BindingSource : INotifyPropertyChanged
        {
            #region INotifyPropertyChanged実装 
            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
            #endregion

            internal BindingSource()
            {
            }

            Visibility _ProgressVisible;
            public Visibility ProgressVisible
            {
                get => _ProgressVisible;
                set { _ProgressVisible = value; OnPropertyChanged(nameof(ProgressVisible)); }
            }

        }

        internal BindingSource m_Bind;

        public MainWindow()
        {
            InitializeComponent();


            m_Bind = new BindingSource
            {
                ProgressVisible = Visibility.Collapsed
            };

            DataContext = m_Bind;
        }

        private readonly Redis m_Redis = new Redis();

        /// <summary>
        /// ガベージコレクト回避のための参照保持
        /// </summary>
        private ConcurrentDictionary<BenchMarkTest, BenchMarkTest> m_Tests = new ConcurrentDictionary<BenchMarkTest, BenchMarkTest>();

        private void OnBtnRedis(object sender, RoutedEventArgs e)
        {

            var test = new BenchMarkTest(m_Redis);

            test.OnTestEnd += () =>
            {                
                m_Tests.TryRemove(test, out _);
                _trace.Warn("Redis Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("Redis Test End");
                }));
            };

            _trace.Warn("Redis Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;

        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            m_Redis?.Dispose();
        }

        private readonly Logger _trace = LogManager.GetCurrentClassLogger();

        private BinaryFile m_BinaryFile = new BinaryFile();

        private void OnBtnBinary(object sender, RoutedEventArgs e)
        {

            var test = new BenchMarkTest(m_BinaryFile);

            test.OnTestEnd += () =>
            {
                m_Tests.TryRemove(test, out _);
                _trace.Warn("Binary Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("Binary Test End");
                }));
            };

            _trace.Warn("Binary Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;

        }

        SQLite m_SQLite = new SQLite();

        private void OnBtnSQLite(object sender, RoutedEventArgs e)
        {

            var test = new BenchMarkTest(m_SQLite);

            test.OnTestEnd += () =>
            {
                m_Tests.TryRemove(test, out _);
                _trace.Warn("SQLite Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("SQLite Test End");
                }));
            };

            _trace.Warn("SQLite Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;

        }

        Empty m_Empty = new Empty();

        private void OnBtnEmpty(object sender, RoutedEventArgs e)
        {
            var test = new BenchMarkTest(m_Empty);

            test.OnTestEnd += () =>
            {
                m_Tests.TryRemove(test, out _);
                _trace.Warn("Empty Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("Empty Test End");
                }));
            };

            _trace.Warn("Empty Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;

        }

        Kafka m_Kafka = new Kafka();

        private void OnBtnKafka(object sender, RoutedEventArgs e)
        {

            var test = new BenchMarkTest(m_Kafka);

            test.OnTestEnd += () =>
            {
                m_Tests.TryRemove(test, out _);
                _trace.Warn("Kafka Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("Kafka Test End");
                }));
            };

            _trace.Warn("Kafka Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;

        }


        MongoDB m_MongoDB = new MongoDB();

        private void OnBtnMongoDB(object sender, RoutedEventArgs e)
        {
            var test = new BenchMarkTest(m_MongoDB);

            test.OnTestEnd += () =>
            {
                m_Tests.TryRemove(test, out _);
                _trace.Warn("MongoDB Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("MongoDB Test End");
                }));
            };

            _trace.Warn("MongoDB Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;

        }

        private BinaryFileType2 m_BinaryType2File = new BinaryFileType2();

        private void OnBtnBinaryType2(object sender, RoutedEventArgs e)
        {

            var test = new BenchMarkTest(m_BinaryType2File);

            test.OnTestEnd += () =>
            {
                m_Tests.TryRemove(test, out _);
                _trace.Warn("BinaryType2 Test End");

                Dispatcher?.BeginInvoke(new Action(() =>
                {
                    m_Bind.ProgressVisible = Visibility.Hidden;
                    MessageBox.Show("BinaryType2 Test End");
                }));
            };

            _trace.Warn("BinaryType2 Test Start");
            test.TestRunAsync();

            m_Tests.TryAdd(test, test);

            m_Bind.ProgressVisible = Visibility.Visible;
        }
    }
}
