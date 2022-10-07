using System.Collections.Generic;

namespace FactPortal.Models
{
    public class SOEdit
    {
        public ServiceList SList { get; set; }
        public int id { get; set; }
        public int value { get; set; }
        public System.Collections.Generic.IEnumerable<Level> Level { get; set; }
        public string Name_of_pos { get; set; }
        public List<Step> Steps { get; set; }
        public List<Alert_> Alerts { get; set; }
    }
}