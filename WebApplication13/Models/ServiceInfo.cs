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
    public enum WorkStatus
    {
        none = 0, // нет статуса
        waiting = 1, // ожидание...
        editing = 2, // редактирование...
        production = 5, // в работе...
        partially = 8, // выполнено частично
        completed = 9 // выполнено
    }
    public class ServiceList
    {
        public List<ServiceObjectShort> SObjects { get; set; }
        //public Dictionary<int, string> Positions { get; set; }
        public Dictionary<string, string> Tables { get; set; }
        //public int maxid { get; internal set; }
        //public System.Collections.Generic.IEnumerable<Level> Level { get; set; }
    }

    public class Statistic
    {
        public int Id { get; set; } // ID записи
        public int ObjectId { get; set; } // ID объекта обслуживания
        public string ObjectName { get; set; } // Наименование объекта обслуживания
        public string UserName { get; set; } // Персонал: Имя

        public string UserEmail { get; set; } // Персонал: Почта
        public int Status { get; set; } // Статус
        public string Description { get; set; } // Описание
        public string DT { get; set; } // Дата и время
        public bool UserActive { get; set; }
        public bool ObjectActive { get; set; }

        public string StatusRus()
        {
            switch (Status)
            {
                case 1:
                    return "ожидание";
                case 2:
                    return "редактирование";
                case 5:
                    return "работа";
                case 8:
                    return "частично";
                case 9:
                    return "выполнено";
                default:
                    return "-";
            }
        }
        public string StatusColor()
        {
            switch (Status)
            {
                case 1:
                    return "#FF0000";
                case 2:
                    return "#FFA500";
                case 5:
                    return "#FF8C00";
                case 8:
                    return "#008B8B";
                case 9:
                    return "#9ACD32";
                default:
                    return "#E0FFFF";
            }
        }
        public string Statusfull()
        {
            switch (Status)
            {
                case 1:
                    return "10";
                case 2:
                    return "20";
                case 5:
                    return "50";
                case 8:
                    return "80";
                case 9:
                    return "100";
                default:
                    return "0";
            }
        }
    }


    public class Alert_
    {
        public int Id { get; set; }
        public int ServiceObjectId { get; set; } // Объект обслуживания
        public string myUserId { get; set; } // Персонал
        public int myFilesId { get; set; } // Файл
        public string DT { get; set; } // Дата и время
        public string Message { get; set; } // Сообщение
        public int Status { get; set; } // Статус
        public string? ObjectName { get; set; }
        public string? UserName { get; set; }
        public string? UserEmail { get; set; }
        public bool UserActive { get; set; }
        public bool ObjectActive { get; set; }

        public string StatusRus()
        {
            switch (Status)
            {
                case 0:
                    return "ожидание";
                case 10:
                    return "редактирование";
                case 5:
                    return "работа";
                case 99:
                    return "частично";
                case 9:
                    return "выполнено";
                default:
                    return "-";
            }
        }

    }
    public class Steps_
    {
        public int Id { get; set; }
        public int ServiceObjectId { get; set; }
        public int Index { get; set; }
        public string? Description { get; set; }
        public string? groupFilesId { get; set; }
        public string? ObjectName { get; set; }
        public bool IsActive { get; set; }

    }


}
