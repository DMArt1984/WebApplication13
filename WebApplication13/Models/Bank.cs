using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{

    public static class Bank
    {

        public static int nextID(List<int> ids)
        {
            ids.Sort();
            var Id = 1;
            for (int i = 0; i < ids.Count; i++)
            {
                if (Id != ids[i])
                    break;
                Id++;
            }
            return Id;
        }

        public static int maxID(List<int> ids)
        {
            if (ids == null || ids.Count()==0)
                return 1;
            return ids.Max() + 1;
        }

        // Получить словарь из вложенных позиций
        public static Dictionary<int, string> GetDicPos(IEnumerable<Level> Levels, int StartId = 0, string DelimPos = ">")
        {
            if (Levels == null)
                return null;

            //Dictionary<int, string> IdName = new Dictionary<int, string>(); // список
            //foreach (var item in Levels)
            //    IdName.Add(item.Id, item.LinkId + ":" + item.Name);

            //Dictionary<int, string> Paths = new Dictionary<int, string>(); // список
            //foreach (var item in IdName.Where(x => (x.Key == StartId || StartId == 0)))
            //    Paths.Add(item.Key, $"{ExpDicPos(IdName, Convert.ToInt32(item.Value.Split(':')[0]), DelimPos)}{item.Value.Split(':')[1]}");


            Dictionary<int, string> IdName = Levels.ToDictionary(x => x.Id, y => y.LinkId + ":" + y.Name);
            Dictionary<int, string> Paths = IdName.Where(x => (x.Key == StartId || StartId == 0)).ToDictionary(
                    x => x.Key,
                    y => $"{ExpDicPos(IdName, Convert.ToInt32(y.Value.Split(':')[0]), DelimPos)}{y.Value.Split(':')[1]}"
                );

            return Paths;
        }

        // Рекурсивное получение вложенных позиций по строкам
        public static string ExpDicPos(Dictionary<int, string> IdName, int Link, string DelimPos)
        {
            var x = IdName.Where(x => x.Key == Link);
            if (x.Count() == 0)
                return "";

            return $"{ExpDicPos(IdName, Convert.ToInt32(x.First().Value.Split(':')[0]), DelimPos)}{x.First().Value.Split(':')[1]}{DelimPos}";
        }


        // Получить объекты из позиций
        public static dynamic GetObjPos(IEnumerable<Level> Levels)
        {
            if (Levels == null)
                return null;

            Dictionary<int, string> IdName = new Dictionary<int, string>(); // список
            foreach (var item in Levels)
                IdName.Add(item.Id, item.LinkId + ":" + item.Name);

            var x = ExpObjPos(IdName, 0);
            return x;
        }

        // Поиск замкнутых ссылок в позициях
        public static IEnumerable<int> ClosedLinkID(IEnumerable<Level> Levels)
        {
            List<int> ClosedLinkID = new List<int>();
            List<int> Path = new List<int>();
            foreach (var item in Levels.OrderBy(x => x.Id))
            {
                Path.Clear();
                if (IsClosed(Levels, Path, item.LinkId) == -1)
                    ClosedLinkID.Add(item.Id);
            }
            return ClosedLinkID;
        }

        private static int IsClosed(IEnumerable<Level> Levels, List<int> Path, int nextId)
        {
            if (Path.Any(x => x == nextId))
                return -1;

            Path.Add(nextId);
            var Pos = Levels.FirstOrDefault(x => x.Id == nextId);
            if (Pos == null)
                return 0;

            return IsClosed(Levels, Path, Pos.LinkId);
        }

        // Рекурсивное получение объектов вложенных позиций
        public static List<dynamic> ExpObjPos(Dictionary<int, string> IdName, int Link)
        {
            var x = new List<dynamic>();
            foreach (var item in IdName.Where(x => Convert.ToInt32(x.Value.Split(':')[0]) == Link && x.Key != Link))
            {
                dynamic inp = new { Id = item.Key, Name = item.Value.Split(':')[1], Next = ExpObjPos(IdName, item.Key) };
                x.Add(inp);
            }
            return x;
        }


        // Получить дочерние объекты только заданного ID
        public static Dictionary<int, string> GetChildPos(IEnumerable<Level> Levels, int Id)
        {
            Dictionary<int, string> IdName = new Dictionary<int, string>();
            foreach (var item in Levels)
            {
                if (item.LinkId == Id)
                    IdName.Add(item.Id, item.Name);
            }
            return IdName;
        }

        // Получить ВСЕ дочерние объекты
        public static void GetChildPosRec(ref Dictionary<int, string> Dic, IEnumerable<Level> Levels, int Id)
        {
            foreach (var item in Levels.OrderBy(x => x.Id))
            {
                if (item.LinkId == Id)
                {
                    Dic.Add(item.Id, item.Name);
                    GetChildPosRec(ref Dic, Levels, item.Id);
                }
            }
            //return Dic;
        }

        // Добавление позиций по фильтру
        public static void TreeExpPos(ref List<int> MyPos, Dictionary<int, int> Levels, int ID)
        {
            foreach (var item in Levels)
            {
                // Key = Id, Value = LinkId
                if (item.Value == ID)
                {
                    MyPos.Add(item.Key);
                    TreeExpPos(ref MyPos, Levels, item.Key);
                }
            }
        }


        // Удаление из названия позиции спец. символов
        public static string NormPosName(string Name)
        {
            return Name.Replace(">", String.Empty).Replace(":", String.Empty);
        }

        // Преобразование даты и времени в строку
        public static string NormDateTime(string DT)
        {
            // 02.08.2022 11:09:57
            // 0123456789
            try
            {
                var DateAndTime = DT.Split(' ');
                var D = DateAndTime[0];
                var T = (DateAndTime.Count() >= 2) ? DateAndTime[1] : "";
                string NewDT = $"{D.Substring(6, 4)}.{D.Substring(3, 2)}.{D.Substring(0, 2)} {T}";
                return NewDT;
            } catch
            {
                return DT;
            }
        }

        // Преобразование строки в локальное время
        public static string LocalDateTime(string DT)
        {
            // 02.08.2022 11:09:57
            // 0123456789
            try
            {
                var NewDT = new DateTime(Convert.ToInt16(DT.Substring(6, 4)), Convert.ToInt16(DT.Substring(3, 2)), Convert.ToInt16(DT.Substring(0, 2)),
                    Convert.ToInt16(DT.Substring(11, 2)), Convert.ToInt16(DT.Substring(14, 2)), Convert.ToInt16(DT.Substring(17, 2)));
               
                return NormDateTime(NewDT.ToLocalTime().ToString());
            }
            catch
            {
                return DT;
            }
        }

        // Проверить дату на вхождение в диапазон  2020.10.25
        public static bool DateInRange(string Date="", string DateFrom="", string DateTo="")
        {
            if (String.IsNullOrEmpty(Date))
                return false;

            if (String.IsNullOrEmpty(DateFrom) && String.IsNullOrEmpty(DateTo))
                return true;

            if (String.IsNullOrEmpty(DateFrom))
                return GetDTfromString(Date) <= GetDTfromString(DateTo);

            if (String.IsNullOrEmpty(DateTo))
                return GetDTfromString(Date) >= GetDTfromString(DateFrom);

            return GetDTfromString(Date) >= GetDTfromString(DateFrom) && GetDTfromString(Date) <= GetDTfromString(DateTo);
        } 

        public static bool TR(string Date = "")
        {
            return true;
        }

        // Получить дату из строки
        public static DateTime GetDTfromString(string Date)
        {
            try
            {
                int Y = Convert.ToInt32(Date.Substring(0, 4));
                int M = Convert.ToInt32(Date.Substring(5, 2));
                int D = Convert.ToInt32(Date.Substring(8, 2));
                return new DateTime(Y, M, D);
            } catch
            {
                return DateTime.UtcNow;
            }
        }

        // Добавить данные в строку списка 1;5;8 -> 1;5;8;7
        public static string AddItemToStringList(string StringList, string Delim, string Values)
        {
            List<string> myList  = StringList.Split(Delim).ToList();
            foreach(var item in Values.Split(Delim))
                myList.Add(item);
            myList.RemoveAll(x => String.IsNullOrEmpty(x));
            return String.Join(Delim, myList.Distinct());
        }

        // Удалить данные из строки списка 2;4;8 -> 2;8
        public static string DelItemToStringList(string StringList, string Delim, string Values)
        {
            List<string> myList = StringList.Split(Delim).ToList();
            foreach (var item in Values.Split(Delim))
                myList.Remove(item);
            myList.RemoveAll(x => String.IsNullOrEmpty(x));
            return String.Join(Delim, myList.Distinct());
        }

        // Получить значения из словарей  ----------------------------------------------
        public static string inf_SS(Dictionary<string, string> SS, string Id)
        {
            return SS.ContainsKey(Id) ? SS[Id] : "";
        }

        public static string inf_IS(Dictionary<int, string> IS, int Id)
        {
            return IS.ContainsKey(Id) ? IS[Id] : "";
        }

        public static string inf_SSOne(Dictionary<string, string> SS, string Ids)
        {
            return (String.IsNullOrEmpty(Ids)) ? "" : String.Join(';', Ids.Split(';').Select(z => z = (SS.ContainsKey(z)) ? SS[z] : ""));
        }

        public static List<string> inf_SSList(Dictionary<string, string> SS, string Ids)
        {
            if (String.IsNullOrEmpty(Ids))
                return new List<string>();

            if (Ids.Split(';').Count() == 0)
                return new List<string>();

            var OUT = Ids.Split(';').Select(z => z = (SS.ContainsKey(z)) ? SS[z] : "").ToList();
            return OUT;

            //return (Ids == null) ? new List<string>() : (Ids.Split(';').Count()==0) ? new List<string>() : Ids.Split(';').Select(z => z = (SS.ContainsKey(z)) ? SS[z] : "").ToList(); // Where(x => !String.IsNullOrEmpty(x)).Distinct()
        }

        public static List<myFiles> inf_SSFiles(List<myFiles> Files, string Ids)
        {
            if (String.IsNullOrEmpty(Ids))
                return new List<myFiles>();

            if (Ids.Split(';').Count() == 0)
                return new List<myFiles>();

            var OUT = Ids.Split(';').Select(x => (Files.Any(y => y.Id.ToString() == x)) ? Files.FirstOrDefault(y => y.Id.ToString() == x) : null).ToList();
            OUT.RemoveAll(x => x == null);
            return OUT;
        }

        public static List<string> OptimizeSList(List<string> SL)
        {
            if (SL == null)
                return null;

            return SL.Where(x => !String.IsNullOrEmpty(x)).Distinct().ToList();
        }

        public static List<string> inf_ISList(Dictionary<int, string> SS, string Ids)
        {
            return (Ids == null) ? new List<string>() : (Ids.Split(';').Count() == 0) ? new List<string>() : Ids.Split(';').Select(z => z = (SS.ContainsKey(Convert.ToInt32(z))) ? SS[Convert.ToInt32(z)] : "").ToList(); //Where(x => !String.IsNullOrEmpty(x)).Distinct()
        }

        // --------------------------------------------------------------------------------

        #region OLD
        // Название позиции (Id, Name)
        public static string GetPosString(int Id, string Name, bool EnId = true, bool EnName = true, string Delim = ":") // Id:Name
        {
            return ((EnId) ? Id.ToString() : String.Empty) + ((EnId && EnName) ? Delim : String.Empty) + ((EnName) ? Name : String.Empty);
        }

        // Получить словарь: позиция+путь
        public static Dictionary<int, string> GetPathLevels(IEnumerable<Level> Levels, int StartId = 0, bool EnId = true, bool EnName = true, string DelimPos = ">", string DelimId = ":")
        {
            List<int> Postions = new List<int>(); // список ID позиций
            Dictionary<int, string> Paths = new Dictionary<int, string>(); // список позиция+путь (родительские объекты)
            if (Levels == null)
                return Paths;
            foreach (var item in Levels) // получаем список всех позиций (участок, шкаф, этаж)
                Postions.Add(item.Id);

            foreach (var itemID in Postions) // получаем путь для всех позиций (цех->участок->кабинет)
            {
                var itemPos = Levels.Where(x => x.Id == itemID).First(); // позиция
                var Line = GetPosString(itemPos.Id, itemPos.Name, EnId, EnName, DelimId); // последняя часть пути
                var Id_for_Link = itemPos.LinkId; // первая ссылка на родительский объект
                var Filter = (itemPos.Id == StartId || StartId == 0);
                while (Id_for_Link > 0)
                {
                    var Parent = Levels.Where(x => (x.Id == Id_for_Link && Id_for_Link != itemID)); // родительский объект
                    if (Parent.Count() > 0) // родительский объект найден
                    {
                        var One = Parent.First(); // родительский объект
                        Line = GetPosString(One.Id, One.Name, EnId, EnName, DelimId) + DelimPos + Line; // добавление части пути...
                        Id_for_Link = One.LinkId; // ссылка на следующий родительский объект
                        Filter = Filter || (One.Id == StartId);
                    }
                    else
                    {
                        Id_for_Link = 0;
                    }
                }
                if (Filter)
                    Paths.Add(itemID, Line); // заполение словаря: позиция+путь
            }

            return Paths;
        }

        #endregion
    }
}
