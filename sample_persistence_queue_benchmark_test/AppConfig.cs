using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;
using sample_persistence_queue_benchmark_test.MongoDBAccess;

namespace sample_persistence_queue_benchmark_test
{
    public class AppConfig
    {
        public IConfiguration Config
        {
            get
            {
                if (m_Config == null)
                {
                    var config = new ConfigurationBuilder();
                    config.SetBasePath(App.AppRunFolderPath);
                    config.AddJsonFile("AppConfig.json");
                    m_Config = config.Build();
                }
                return m_Config;
            }

        }

        private IConfiguration m_Config;

    }
}
