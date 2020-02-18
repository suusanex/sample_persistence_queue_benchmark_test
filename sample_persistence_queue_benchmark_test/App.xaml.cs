using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using NLog;

namespace sample_persistence_queue_benchmark_test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            
        }

        private readonly Logger _trace = LogManager.GetCurrentClassLogger();

        private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            var message = e.Exception?.Message;
            _trace.Error(message);
            MessageBox.Show(message);
        }

        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var message = e.Exception?.Message;
            _trace.Error(message);
            MessageBox.Show(message);
            e.Handled = true;
        }
    }
}
