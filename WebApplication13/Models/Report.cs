using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public enum RepColType
    {
        User = 10,
        userFullName = 13,
        Email = 11,
        Phone = 12,
        Image = 14,

        ServiceObject = 20,
        soTitle = 21,
        soCode = 22,
        soDescription = 23,
        soPosition = 24,

        Alert = 30,
        alertStatus = 31,

        Work = 40,
        workStatus = 41,

        File = 50,
        FilesV = 51,

        Id = 1,
        DataTime = 2,
        Text = 3,

        Table = 100
    }
    
    public class RepRow
    {

        public RepRow(List<dynamic> Values)
        {
            this.Values = Values.ToArray();
        }

        public RepRow(dynamic[] Values)
        {
            this.Values = Values;
        }

        public dynamic[] Values { get; set; }

    }

    public struct RepTableArrays
    {
        public int Layer;
        public string Title;
        public RepColType[] ColsType;
        public string[] ColsName;
        public RepRow[] Rows;
    }

    public class RepTable
    {
        public int Layer { get; set; }
        public string Title { get; set; }
        public List<RepColType> ColsType { get; set; }
        public List<string> ColsName { get; set; }
        public List<RepRow> Rows { get; set; }

        public RepTable(int Layer = 0)
        {
            this.Layer = Layer;
            ColsName = new List<string>();
            ColsType = new List<RepColType>();
            Rows = new List<RepRow>();
        }
        public RepTable(List<RepColType> ColsType, List<string> ColsName, int Layer = 0)
        {
            this.Layer = Layer;
            this.ColsName = ColsName;
            this.ColsType = ColsType;
            Rows = new List<RepRow>();
        }
        public RepTable(List<RepRow> Rows, int Layer = 0)
        {
            this.Layer = Layer;
            ColsName = new List<string>();
            ColsType = new List<RepColType>();
            this.Rows = Rows;
        }
        public RepTable(List<RepColType> ColsType, List<string> ColsName, List<RepRow> Rows, int Layer = 0)
        {
            this.Layer = Layer;
            this.ColsName = ColsName;
            this.ColsType = ColsType;
            this.Rows = Rows;
        }

        public RepTableArrays GetArrays()
        {
            return new RepTableArrays {
                Layer = Layer,
                Title = Title,
                ColsName = ColsName.ToArray(), 
                ColsType = ColsType.ToArray(),
                Rows = Rows.ToArray()};
        }
    }

}
