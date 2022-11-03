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
            // 02.08.2022 7:09:57
            // 0123456789
            try
            {
                var Big = DT.Split(' ');

                var BDate = Big[0].Split('.');
                var Month = Convert.ToInt32(BDate[1]);
                var Day = Convert.ToInt32(BDate[0]);
                var Year = Convert.ToInt32(BDate[2]);
                if (Day > 31 || Year < 2000)
                {
                    Year = Convert.ToInt32(BDate[0]);
                    Day = Convert.ToInt32(BDate[2]);
                }

                var DTime = Big[1].Split(':');
                var Hour = Convert.ToInt32(DTime[0]);
                var Minute = Convert.ToInt32(DTime[1]);
                var Second = Convert.ToInt32(DTime[2]);

                string NewDT = $"{Year}.{Month.ToString("D2")}.{Day.ToString("D2")} {Hour.ToString("D2")}:{Minute.ToString("D2")}:{Second.ToString("D2")}";
                return NewDT;
            } catch
            {
                return DT;
            }
        }

        // Преобразование строки в локальное время
        public static string LocalDateTime(string DT)
        {
            // 2022.08.02 5:09:57
            // 0123456789
            try
            {
                var Big = DT.Split(' ');

                var BDate = Big[0].Split('.');
                var Month = Convert.ToInt32(BDate[1]);
                var Day = Convert.ToInt32(BDate[2]);
                var Year = Convert.ToInt32(BDate[0]);
                if (Day > 31 || Year < 2000)
                {
                    Year = Convert.ToInt32(BDate[2]);
                    Day = Convert.ToInt32(BDate[0]);
                }

                var DTime = Big[1].Split(':');
                var Hour = Convert.ToInt32(DTime[0]);
                var Minute = Convert.ToInt32(DTime[1]);
                var Second = Convert.ToInt32(DTime[2]);

                var NewDT = new DateTime(Year, Month, Day, Hour, Minute, Second);
                var LocalTime = NewDT.ToLocalTime().ToString();
                var NormTime = NormDateTime(LocalTime);

                return NormTime;
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

            var OUT = GetDTfromString(Date) >= GetDTfromString(DateFrom) && GetDTfromString(Date) <= GetDTfromString(DateTo);
            return OUT;
        } 

        // Получить дату из строки
        public static DateTime GetDTfromString(string DT)
        {
            try
            {
                var Big = DT.Split(' ');

                var BDate = Big[0].Split('.');
                var Month = Convert.ToInt32(BDate[1]);
                var Day = Convert.ToInt32(BDate[2]);
                var Year = Convert.ToInt32(BDate[0]);
                if (Day > 31 || Year < 2000)
                {
                    Year = Convert.ToInt32(BDate[2]);
                    Day = Convert.ToInt32(BDate[0]);
                }

                return new DateTime(Year, Month, Day);
            } catch
            {
                return DateTime.UtcNow;
            }
        }

        // Время и дата начала работы
        public static string GetWork_DTStart(int Status, string DT = "")
        {
            return (Status == 5) ? (String.IsNullOrEmpty(DT)) ? NormDateTime(System.DateTime.Now.ToUniversalTime().ToString()): DT : "";
        }

        // Время и дата окончания работы
        public static string GetWork_DTStop(int Status, string DT = "")
        {
            return (Status == 9) ? (String.IsNullOrEmpty(DT)) ? NormDateTime(System.DateTime.Now.ToUniversalTime().ToString()) : DT : "";
        }

        // Получить минимальную дату и время
        public static string GetMinDT(List<string> DTs)
        {
            if (DTs.Count() == 0)
                return "";

            string MinDT = DTs.OrderBy(x => x).First();
            foreach(var item in DTs)
            {
                if (GetDTfromString(MinDT) > GetDTfromString(item))
                    MinDT = item;
            }
            return MinDT;
        }

        // Получить максимальную дату и время
        public static string GetMaxDT(List<string> DTs)
        {
            if (DTs.Count() == 0)
                return "";

            string MaxDT = DTs.OrderBy(x => x).Last();
            foreach (var item in DTs)
            {
                if (GetDTfromString(MaxDT) < GetDTfromString(item))
                    MaxDT = item;
            }
            return MaxDT;
        }

        // ========================

        public static bool StringContains(string Ids="", int Id=0)
        {
            return Ids.Split(";").Contains(Id.ToString());
            
            //foreach (var item in Ids.Split(';'))
            //{
            //    if (item == Id.ToString())
            //        return true;
            //}
            //return false;

            //Ids = ";" + Ids + ";";
            //return Ids.Contains(";" + Id.ToString() + ";");

            //return false;

        }      
        
        public static bool GetFalse()
        {
            return false;
        }

        // =========================================================================================

        // Добавить данные в строку списка 1;5;8 -> 1;5;8;7
        public static string AddItemToStringList(string StringList, string Delim, string Values)
        {
            List<string> myList  = StringList.Split(Delim).Distinct().ToList();
            foreach (var item in Values.Split(Delim).Distinct())
            {
                if (myList.Contains(item) == false)
                    myList.Add(item);
            }
            myList.RemoveAll(x => String.IsNullOrEmpty(x));
            return String.Join(Delim, myList.Distinct());
        }

        // Удалить данные из строки списка 2;4;8 -> 2;8
        public static string DelItemToStringList(string StringList, string Delim, string Values)
        {
            List<string> myList = StringList.Split(Delim).Distinct().ToList();
            foreach (var item in Values.Split(Delim).Distinct())
            {
                myList.Remove(item);
            }
            myList.RemoveAll(x => String.IsNullOrEmpty(x));
            return String.Join(Delim, myList.Distinct());
        }

        // Проверить наличие информации в списке
        public static int inf_ListMinus(List<int> myList, int Value)
        {
            return myList.Any(j => j == Value) ? Value : -Value; // если нет в списке, то знак минус
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

        public static int inf_II(Dictionary<int, int> II, int Id)
        {
            return II.ContainsKey(Id) ? II[Id] : 0;
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

        // Получение статуса работы
        public static int GetStatusWork(List<int> Steps, int FinalStep)
        {
            int CalcStatus = 0; // нет работ

            if (Steps.Count() > 0)
                CalcStatus = 1; // ожидание

            // работа
            foreach (var item in Steps)
            {
                if (item == 5)
                {
                    CalcStatus = 5;
                    break;
                }
            }

            // выполнено
            var Ready = 0;
            if (CalcStatus != 5 && FinalStep > 0)
            {
                foreach (var item in Steps)
                {
                    if (item == 9)
                        Ready++;
                }
                if (Ready == FinalStep)
                    CalcStatus = 9;
            }

            return CalcStatus;
        }

        // ---- Словари ----------------------------------------------------------------------------

        public static Dictionary<string, string> GetDicUsers(List<ApplicationUser> Users)
        {
            return Users.ToDictionary(x => x.Id, y => y.FullName);
        }

        public static Dictionary<string, string> GetDicFilesPath(List<myFiles> Files)
        {
            return Files.ToDictionary(x => x.Id.ToString(), y => y.Path);
        }

        public static Dictionary<string, string> GetDicFiles(List<myFiles> Files)
        {
            return Files.ToDictionary(x => x.Id.ToString(), y => y.Path + ";" + (String.IsNullOrEmpty(y.Description) ? y.Path.Split("/").Last() : y.Description));
        }

        public static Dictionary<int, int> GetDicPos(List<ObjectClaim> Claims)
        {
            return Claims.Where(x => x.ClaimType.ToLower() == "position").ToDictionary(x => x.ServiceObjectId, y => Convert.ToInt32(y.ClaimValue));
        }

        public static Dictionary<int, int> GetDicLastWorkId(List<ServiceObject> SO, List<Work> Works)
        {
            var Works2 = Works.OrderBy(y => y.Id).ToList();
            var SW1 = SO.Select(x => new { x.Id, Works = Works2.Where(k => k.ServiceObjectId == x.Id).ToList() }).ToList();
            var SW2 = SW1.Select(x => new { x.Id, WorkId = (x.Works.Count > 0) ? x.Works.Last().Id : 0 }).ToList();
            return SW2.ToDictionary(x => x.Id, y => y.WorkId);
        }

        public static Dictionary<int, int> GetDicFinalStep(List<ServiceObject> SO, List<Step> Steps)
        {
            return SO.ToDictionary(x => x.Id, y => Steps.Where(z => z.ServiceObjectId == y.Id).Select(m => m.Index).Distinct().Count());
        }

        public static Dictionary<int, int> GetDicWorkStatus(List<Work> Works, List<WorkStep> WorkSteps, Dictionary<int, int> DFinalSteps)
        {
            return Works.ToDictionary(x => x.Id, y => Bank.GetStatusWork(WorkSteps.Where(z => z.WorkId == y.Id).Select(z => z.Status).ToList(), Bank.inf_II(DFinalSteps, y.ServiceObjectId)));
        }

        public static Dictionary<int, string> GetDicSO(List<ServiceObject> SO)
        {
            return SO.ToDictionary(x => x.Id, y => y.ObjectTitle);
        }

        // ------------------------

        public static int GetLastWorkId(int ServiceObjectId, List<Work> Works)
        {
            var Works_SO = Works.Where(x => x.ServiceObjectId == ServiceObjectId).OrderBy(y => y.Id).ToList();
            var OUT = (Works_SO.Count() > 0) ? Works_SO.Last().Id : 0;
            return OUT;
        }


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
