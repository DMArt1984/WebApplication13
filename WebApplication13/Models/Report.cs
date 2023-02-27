using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public enum QueryLeftRight // тип переменной
    {
        Condition = 0, // условие
        Formula = 1 // формула
    }
    public enum OperatorLeftRight // оператор
    {
        OR = 0, // или
        AND = 1 // и
    }

    public enum RepCondition // условие
    {
        empty, 
        notempty, 
        equals, 
        notequals, 
        gt, 
        gte, 
        lt, 
        lte, 
        contains, 
        notcontains, 
        between, 
        notbetween
    }

    public enum RepGroup // группа
    {
        SO, // Объект
        Work, // Обслуживание
        Step, // Шаг
        User, // Сотрудник
        File // Файл
    }

    public enum RepSO // группа: Объект
    {
        ID,
        Title,
        Code,
        Description,
        LastWorkDT
    }

    public enum RepWork // группа: Обслуживание
    {
        ID,
        StartDT,
        EndDT,
        Steps,
        ReadySteps,
        Status
    }

    public enum RepStep // группа: Шаг
    {
        ID,
        Title,
        Description,
        StartDT,
        EndDT,
        Status,
        HasFiles
    }

    public enum RepUser // группа: Сотрудник
    {
        ID,
        Name,
        Email,
        Phone,
        Role,
        Job,
        CountWorks
    }

    public enum RepFile // группа: Файл
    {
        ID,
        Name,
        Description,
        Type
    }


    // ========================================================================================

    public class QueryColumn // Колонка
    {
        public int Id { get; set; } // ID
        public string group { get; set; } // объект, таблица (объекты, пользователи и т.п.), ...
        public string element { get; set; } // свойство объекта, столбец (название, дата и т.п.), ...
    }
    public class QueryCondition // Условие [DB]
    {
        public int Id { get; set; } // ID
        public int IdColumn { get; set; } // ID колонки
        public string condition { get; set; } // условие: 'равно', 'не равно', 'больше', 'больше или равно', ...
        public string value1 { get; set; } // константа 1 для условия
        public string value2 { get; set; } // константа 2 для условия
    }
    public class QueryConditionInfo // Условие
    {
        public int Id { get; set; } // ID
        public QueryColumn column { get; set; } // Колонка
        public string condition { get; set; } // условие: 'равно', 'не равно', 'больше', 'больше или равно', ...
        public string value1 { get; set; } // константа 1 для условия
        public string value2 { get; set; } // константа 2 для условия
    }

    public class QueryFormula // Формула [DB]
    {
        public int Id { get; set; } // ID
        public bool typeLeft { get; set; } // тип переменной слева: условие или формула
        public int IdLeft { get; set; } // ID переменной слева (0 - если пусто)
        public bool AndOr { get; set; } // оператор: AND, OR
        public bool typeRight { get; set; } // тип переменной справа: условие или формула
        public int IdRight { get; set; } // ID переменной справа (0 - если пусто)
    }
    public class QueryFormulaInfo // Формула
    {
        public int Id { get; set; } // ID
        public QueryLeftRight typeLeft { get; set; } // тип переменной слева: условие или формула
        public int IdLeft { get; set; } // ID переменной слева (0 - если пусто)
        public OperatorLeftRight AndOr { get; set; } // оператор: AND, OR
        public QueryLeftRight typeRight { get; set; } // тип переменной справа: условие или формула
        public int IdRight { get; set; } // ID переменной справа (0 - если пусто)
    }

    public class QueryView // Вид [DB]
    {
        public int Id { get; set; } // ID
        public int IdFormula { get; set; } // ID формулы
        public string IdColumns { get; set; } // Колонки по группам (например: 1;2;3 > 4;5 > 9) в строке
    }
    public class QueryViewInfo // Вид
    {
        public int Id { get; set; } // ID
        public QueryFormulaInfo formula { get; set; } // Формула
        public List<List<QueryColumn>> columns { get; set; } // Колонки по группам
    }


    // ==================================================================================

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

    // ==================================================================================

    public struct RepAll // Универсальный отчет
    {
        public List<dynamic> blocks;
    }

    // ----------------------------------------------------------------------------------

    public struct RepTable // Блок: Таблица
    {
        public string title { set; get; } // Заголовок
        public readonly List<string> colsName; // Названия столбцов
        public readonly List<RepTableRow> rows; // Строки

        public RepTable(string title)
        {
            this.title = title;
            this.colsName = new List<string>();
            this.rows = new List<RepTableRow>();
        }

        public RepTable(string title, IEnumerable<string> colsName)
        {
            this.title = title;
            this.colsName = colsName.ToList();
            this.rows = new List<RepTableRow>();
        }

        public RepTable(string title, IEnumerable<string> colsName, IEnumerable<RepTableRow> rows)
        {
            this.title = title;
            this.colsName = colsName.ToList();
            this.rows = rows.ToList();
        }

    }

    public struct RepAccordion // Блок: Раскрывающийся список 1
    {
        public string title; // Заголовок
        public List<RepAccordionRow> rows; // Строки

        public RepAccordion(string title)
        {
            this.title = title;
            this.rows = new List<RepAccordionRow>();
        }

        public RepAccordion(string title, IEnumerable<RepAccordionRow> rows)
        {
            this.title = title;
            this.rows = rows.ToList();
        }

    }

    public struct RepCollaps // Блок: Раскрывающийся список 2
    {
        public string title; // Заголовок
        public List<RepCollapsRow> rows; // Строки

        public RepCollaps(string title)
        {
            this.title = title;
            this.rows = new List<RepCollapsRow>();
        }

        public RepCollaps(string title, IEnumerable<RepCollapsRow> rows)
        {
            this.title = title;
            this.rows = rows.ToList();
        }
    }

    // ----------------------------------------------------------------------------------

    public class RepTableRow // Блок: Таблица - строка
    {
        public List<dynamic> Values { get; set; }

        public RepTableRow(IEnumerable<dynamic> Values)
        {
            this.Values = Values.ToList();
        }
    }

    public class RepAccordionRow // Блок: Раскрывающийся список 1 - строка
    {
        public string Title { get; set; }
        public dynamic Content { get; set; }
    }

    public class RepCollapsRow // Блок: Раскрывающийся список 2 - строка
    {
        public dynamic TitleContent { get; set; }
        public dynamic OpenContent { get; set; }
    }

    // ---------------------------------------------------------------------------------

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



}
