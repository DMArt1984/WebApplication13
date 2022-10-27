using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public enum eWorkStep
    {
        Wait = 1, // ожидание
        Edit = 2, // редактирование
        Skip = 3, // пропущен
        Work = 5, // работа
        Part = 8, // частично
        Ready = 9, // выполнено
        Load = 11, // загрузка данных
    }

    public enum eAlert
    {
        New = 0, // новое
        View = 5, // просмотрено
        Closed = 9, // закрыто
        Edit = 10, // редактирование
        Load = 11, // загрузка данных
        Change = 99 // изменено
    }

    // Объекты обслуживания: модель
    public class ServiceObject
    {
        public int Id { get; set; } // ID объекта обслуживания
        public string ObjectTitle { get; set; } // Название
        public string ObjectCode { get; set; } // Штрих код
        public string Description { get; set; } // Описание
        public virtual ICollection<ObjectClaim> Claims { get; set; }
        public virtual ICollection<Alert> Alerts { get; set; }
        public virtual ICollection<Work> Works { get; set; }
        public virtual ICollection<Step> Steps { get; set; }
        public ServiceObject()
        {
            Claims = new List<ObjectClaim>();
            Alerts = new List<Alert>();
            Works = new List<Work>();
            Steps = new List<Step>();
        }

    }
    // Объекты обслуживания: полная информация
    public class ServiceObjectInfo
    {
        public int Id { get; set; } // ID объекта обслуживания
        public string ObjectTitle { get; set; } // Название
        public string ObjectCode { get; set; } // Штрих код
        public string Description { get; set; } // Описание
        public string Position { get; set; } // Путь
        public List<myFiles> FileLinks { get; set; } // Файлы
        public List<ObjectClaim> Claims { get; set; }
        public List<AlertInfo> Alerts { get; set; }
        public List<WorkInfo> Works { get; set; }
        public List<StepInfo> Steps { get; set; }
    }
    // Объекты обслуживания: короткая информация
    public class ServiceObjectShort
    {
        public int Id { get; set; } // ID объекта обслуживания
        public string ObjectTitle { get; set; } // Название
        public string ObjectCode { get; set; } // Штрих код
        public string Description { get; set; } // Описание
        public string Position { get; set; } // Путь
        public int CountAlerts { get; set; } // Количество сообщений
        public Work LastWork { get; set; } // Последнее обслуживание
    }
    // Объекты обслуживания: редактирование
    public class ServiceObjectEdit
    {
        public int Id { get; set; } // ID объекта обслуживания
        public string ObjectTitle { get; set; } // Название
        public string ObjectCode { get; set; } // Штрих код
        public string Description { get; set; } // Описание
        public int Position { get; set; } // Путь
        public List<myFiles> FileLinks { get; set; } // Файлы
        public List<Level> Levels { get; set; } // Позиции
    }


    // Свойства объектов
    public class ObjectClaim
    {
        public int Id { get; set; } // ID свойства
        public int ServiceObjectId { get; set; } // ID объекта обслуживания
        public string ClaimType { get; set; } // Свойство (описание, дата, кол-во)
        public string ClaimValue { get; set; } // Значение свойства
        //public virtual ServiceObject ServiceObject { get; set; }
    }


    // Уведомление (сообщение): модель
    public class Alert
    {
        public int Id { get; set; }
        public int ServiceObjectId { get; set; } // Объект обслуживания
        public string myUserId { get; set; } // Персонал
        public string groupFilesId { get; set; } // Файлы (10;11;12)
        public string DT { get; set; } // Дата и время
        public string Message { get; set; } // Сообщение
        public int Status { get; set; } // Статус
    }
    // Уведомление (сообщение): полная информация
    public class AlertInfo
    {
        public int Id { get; set; }
        public int ServiceObjectId { get; set; } // ID объекта обслуживания
        public string ServiceObjectTitle { get; set; } // Название объекта обслуживания
        public string ServiceObjectCode { get; set; } // Код объекта обслуживания
        public string UserName { get; set; } // Персонал: Имя
        public string UserEmail { get; set; } // Персонал: Почта
        public List<myFiles> FileLinks { get; set; } // Файлы
        public string DT { get; set; } // Дата и время
        public string Message { get; set; } // Сообщение
        public int Status { get; set; } // Статус

        public string StatusRus()
        {
            switch (Status)
            {
                case 0:
                    return "новое";
                case 5:
                    return "просмотрено";
                case 9:
                    return "закрыто";
                case 10:
                    return "редактирование";
                case 11:
                    return "загрузка данных";
                case 99:
                    return "изменено";
                default:
                    return $"#{Status.ToString()}";
            }
        }
    }


    // Обслуживание объектов: модель
    public class Work
    {
        public int Id { get; set; } // ID записи
        public int ServiceObjectId { get; set; } // ID объекта обслуживания

    }
    // Обслуживание объектов: полная информация
    public class WorkInfo
    {
        public int Id { get; set; } // ID записи
        public int ServiceObjectId { get; set; } // ID объекта обслуживания
        public string ServiceObjectTitle { get; set; } // Название объекта обслуживания
        public string ServiceObjectCode { get; set; } // Код объекта обслуживания
        public int Status { get; set; } // Статус
        public int FinalStep { get; set; } // Номер последнего шага (всего шагов)
        public List<WorkStepInfo> Steps { get; set; } // Шаги выполнения работ
        public string StatusRus()
        {
            switch (Status)
            {
                case 0:
                    return "-";
                case 1:
                    return "ожидание";
                case 5:
                    return "работа";
                case 9:
                    return "выполнено";
                default:
                    return $"#{Status.ToString()}";
            }
        }

        

    }


    // Шаги обслуживания: модель
    public class Step
    {
        public int Id { get; set; } // ID записи
        public int ServiceObjectId { get; set; } // ID объекта обслуживания
        public int Index { get; set; } // Номер шага
        public string Description { get; set; } // Описание
        public string groupFilesId { get; set; } // Файлы (10;11;12)
    }
    // Шаги обслуживания: полная информация
    public class StepInfo
    {
        public int Id { get; set; } // ID записи
        public int ServiceObjectId { get; set; } // ID объекта обслуживания
        public string ServiceObjectTitle { get; set; } // Название объекта обслуживания
        public string ServiceObjectCode { get; set; } // Код объекта обслуживания
        public int Index { get; set; } // Номер шага
        public string Description { get; set; } // Описание
        public List<myFiles> FileLinks { get; set; } // Файлы (10;11;12)
    }

    // Выполнение обслуживания по шагам
    public class WorkStep
    {
        public int Id { get; set; } // ID записи
        public int WorkId { get; set; } // ID записи обслуживания
        public int Index { get; set; } // Номер шага
        public string myUserId { get; set; } // Персонал
        public int Status { get; set; } // Статус
        public string DT_Start { get; set; } // Дата и время начала
        public string DT_Stop { get; set; } // Дата и время начала
        public string groupFilesId { get; set; } // Файлы (10;11;12)
        public string StatusRus()
        {
            switch (Status)
            {
                case 0:
                    return "-";
                case 1:
                    return "ожидание";
                case 2:
                    return "редактирование";
                case 3:
                    return "пропущен";
                case 11:
                    return "загрузка данных";
                case 5:
                    return "работа";
                case 8:
                    return "частично";
                case 9:
                    return "выполнено";
                default:
                    return $"#{Status.ToString()}";
            }
        }

    }

    public class WorkStepInfo
    {
        public int Id { get; set; } // ID записи
        public int ServiceObjectId { get; set; } // ID объекта обслуживания
        public string ServiceObjectTitle { get; set; } // Название объекта обслуживания
        public int WorkId { get; set; } // ID записи обслуживания
        public int Index { get; set; } // Номер шага
        public string UserName { get; set; } // Персонал: Имя
        public string UserEmail { get; set; } // Персонал: Почта
        public int Status { get; set; } // Статус
        public string DT_Start { get; set; } // Дата и время начала
        public string DT_Stop { get; set; } // Дата и время начала
        public List<myFiles> FileLinks { get; set; } // Файлы (10;11;12)
        public string StatusRus()
        {
            switch (Status)
            {
                case 0:
                    return "-";
                case 1:
                    return "ожидание";
                case 2:
                    return "редактирование";
                case 3:
                    return "пропущен";
                case 11:
                    return "загрузка данных";
                case 5:
                    return "работа";
                case 8:
                    return "частично";
                case 9:
                    return "выполнено";
                default:
                    return $"#{Status.ToString()}";
            }
        }
    }

    
}
