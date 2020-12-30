using System;
using System.Collections.Generic;
using System.Text;

namespace BacakupAndRestore
{
    public class Procces
    {
        public int fid { get; set; }
        public int spid { get; set; }
        public string status { get; set; }
        public string loginame { get; set; }
        public string origname { get; set; }
        public string hostname { get; set; }
        public int blk_spid { get; set; }

        public string dbname { get; set; }
        public string cmd { get; set; }
        public int block_xloid { get; set; }
    }
}
