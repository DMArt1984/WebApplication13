using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public class IndexStat
    {
        public DateTime Today { get; set; }
        public List<Statistic> Statistics { get; set; }
    }
    

    public class Statistic
    {
        public int Id { get; set; } // ID записи
        
    }



}
