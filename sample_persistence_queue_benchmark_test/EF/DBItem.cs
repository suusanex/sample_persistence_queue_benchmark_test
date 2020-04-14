using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace sample_persistence_queue_benchmark_test.EF
{
    public class DBItem
    {
        [Key]
        [Required]
        
        public int id { get; set; }

        [Required]
        public string data { get; set; }
    }
}
