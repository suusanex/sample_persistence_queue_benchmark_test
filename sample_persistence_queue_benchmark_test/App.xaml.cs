using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
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

        public static readonly string AppRunFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        static readonly AppConfig m_AppConfig = new AppConfig();

        public static IConfiguration Config => m_AppConfig.Config;

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
