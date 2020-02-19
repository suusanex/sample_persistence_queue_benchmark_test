using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace sample_persistence_queue_benchmark_test.EF
{
    public class EFDBContext : DbContext
    {

        public DbSet<DBItem> DBItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=SQLite.db");
    }
}
