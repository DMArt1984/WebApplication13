using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public class Level
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int LinkId { get; set; }
    }

    public class EditLevel
    {
        public Level IT { get; set; }
        public string PathString { get; set; }
        public string PathId { get; set; }
        public int Objects { get; set; }
    }
}
