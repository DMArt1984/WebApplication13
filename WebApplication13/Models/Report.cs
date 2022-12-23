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

        Dynamic = 0,
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

    public struct RepTable
    {
        public string Title;
        public RepColType[] ColsType;
        public string[] ColsName;
        public RepRow[] Rows;
    }

    public class RepTableList
    {
        public string Title { get; set; }
        public List<RepColType> ColsType { get; set; }
        public List<string> ColsName { get; set; }
        public List<RepRow> Rows { get; set; }

        public RepTableList()
        {
            ColsName = new List<string>();
            ColsType = new List<RepColType>();
            Rows = new List<RepRow>();
        }
        public RepTableList(List<RepColType> ColsType, List<string> ColsName)
        {
            this.ColsName = ColsName;
            this.ColsType = ColsType;
            Rows = new List<RepRow>();
        }
        public RepTableList(List<RepRow> Rows)
        {
            ColsName = new List<string>();
            ColsType = new List<RepColType>();
            this.Rows = Rows;
        }
        public RepTableList(List<RepColType> ColsType, List<string> ColsName, List<RepRow> Rows)
        {
            this.ColsName = ColsName;
            this.ColsType = ColsType;
            this.Rows = Rows;
        }

        public RepTable GetArrays()
        {
            return new RepTable {
                Title = Title,
                ColsName = ColsName.ToArray(), 
                ColsType = ColsType.ToArray(),
                Rows = Rows.ToArray()};
        }
    }

    public class CardCircle
    {
        public string icon { get; set; }
        public string color { get; set; }
        public string percent { get; set; }
        public string alpha { get; set; }
        public string description { get; set; }
        public string title { get; set; }
        public string titleNext { get; set; }
        public string value { get; set; }
    }

    public class cellCircle
    {
        public string color { get; set; }
        public string percent { get; set; }
    }

    public class cellProgressBar
    {
        public string color { get; set; }
        public float now { get; set; }
        public float min { get; set; }
        public float max { get; set; }
    }

    public class cellTextAccordion
    {
        public string[][] rows { get; set; }
    }

    public class RepAccordionItem
    {
        public string Title { get; set; }
        public dynamic Content { get; set; }
    }

    public struct RepAccordion
    {
        public RepAccordionItem[] rows;
    }

    public class RepCollapsItem
    {
        public dynamic TitleContent { get; set; }
        public dynamic OpenContent { get; set; }
    }

    public struct RepCollaps
    {
        public RepCollapsItem[] rows;
    }

    public struct RepAll
    {
        public dynamic item;
    }

}
