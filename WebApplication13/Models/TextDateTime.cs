using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public enum DTFormat
    {
        YMDHMS = 0,
        Calendar = 1
    }
    public class TextDateTime
    {
        DateTime _dt;
        int year;
        int month;
        int day;
        int hour;
        int minute;
        int second;

        // Получить название месяца
        public static string GetNameMonth(int month)
        {
            string[] Names = new string[] { "январь", "февраль", "март", "апрель", "май", "июнь", "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь" };
            if (month >= 1 && month <= 12)
                return Names[month - 1];

            return "";
        }
        
        // Получить DateTime из строки <YYYY.MM.DD HH:MM:SS>
        public static DateTime TextToDateTime(string YMDHMS, int OffsetMinutes = 0, bool EnableTime = true)
        {
            if (String.IsNullOrWhiteSpace(YMDHMS))
                return DateTime.UtcNow;

            try
            {
                int year = 0;
                int month = 0;
                int day = 0;
                int hour = 0;
                int minute = 0;
                int second = 0;

                string[] DateAndTime = YMDHMS.Split(' '); // 2020.10.11 15:16:17

                if (DateAndTime.Length >= 1)
                {
                    (year, month, day) = StringTo3(DateAndTime[0], '.'); // 2020.10.11
                    if (day > 31 || year < 1000)
                    {
                        (year, day) = (day, year);
                    }

                    if (EnableTime && DateAndTime.Length >= 2)
                    {
                        (hour, minute, second) = StringTo3(DateAndTime[1], ':'); // 15:16:17
                    }

                }

                DateTime DT = new DateTime(year, month, day, hour, minute, second);
                DT = DT.AddMinutes((double)OffsetMinutes);

                return DT;
            }
            catch
            {
                return DateTime.UtcNow;
            }
}

        // Получить DateTime из строки <2022-01-07T08:09>
        public static DateTime CalendarToDateTime (string Calendar, int OffsetMinutes = 0, bool EnableTime = true)
        {
            if (String.IsNullOrWhiteSpace(Calendar))
                return DateTime.UtcNow;

            if (Calendar.Length != 16)
                return DateTime.UtcNow;
            
            try
            {
                //var year = Convert.ToInt32(Calendar.Substring(0, 4));
                //var month = Convert.ToInt32(Calendar.Substring(5, 2));
                //var day = Convert.ToInt32(Calendar.Substring(8, 2));

                int year = 0;
                int month = 0;
                int day = 0;
                var hour = 0;
                var minute = 0;
                var second = 0;

                string[] DateAndTime = Calendar.Split('T'); // 2022-01-07T08:09

                (year, month, day) = StringTo3(DateAndTime[0], '-');

                if (EnableTime)
                {
                    //hour = Convert.ToInt32(Calendar.Substring(11, 2));
                    //minute = Convert.ToInt32(Calendar.Substring(14, 2));
                    (hour, minute, second) = StringTo3(DateAndTime[1], ':'); // 08:09
                }
                
                DateTime DT = new DateTime(year, month, day, hour, minute, second);
                DT = DT.AddMinutes((double)OffsetMinutes);

                return DT;

            } catch
            {
                return DateTime.UtcNow;
            }
            
        }

        // Конструкторы
        public TextDateTime(DateTime dt)
        {
            _dt = dt;
            SetValues(dt);
        }

        // Деконструктор
        public void Deconstruct(out int year, out int month, out int day, out int hour, out int minute, out int second)
        {
            year = this.year;
            month = this.month;
            day = this.day;
            hour = this.hour;
            minute = this.minute;
            second = this.second;
        }

        public TextDateTime(string TextDT, int OffsetMinutes = 0, DTFormat format = DTFormat.YMDHMS)
        {
            _dt = (format == DTFormat.Calendar) ? CalendarToDateTime(TextDT, OffsetMinutes) : TextToDateTime(TextDT, OffsetMinutes);
            SetValues(_dt);
        }

        public override bool Equals(object obj)
        {
            return this.ToString().Equals(obj?.ToString());
        }

        private void SetValues(DateTime dt)
        {
            (year, month, day, hour, minute, second) = (dt.Year, dt.Month, dt.Day, dt.Hour, dt.Minute, dt.Second);
        }

        private static (int, int, int) StringTo3(string Value, char Delim = ' ')
        {
            if (String.IsNullOrWhiteSpace(Value))
                return (0, 0, 0);

            int a = 0;
            int b = 0;
            int c = 0;

            string[] arr = Value.Split(Delim);

            if (arr.Length >= 1)
                int.TryParse(arr[0], out a);

            if (arr.Length >= 2)
                int.TryParse(arr[1], out b);

            if (arr.Length >= 3)
                int.TryParse(arr[2], out c);

            return (a, b, c);
        }


        // Совпадает ли в дате месяц и год
        public bool DTinValues(int year, int month) => this.year == year && this.month == month;

        // Получить строку <YYYY.MM.DD HH:MM:SS>
        public override string ToString() => $"{year}.{month.ToString("D2")}.{day.ToString("D2")} {hour.ToString("D2")}:{minute.ToString("D2")}:{second.ToString("D2")}";

        // Получить строку <май 2021>
        public string ToMonthYYYY() => $"{GetNameMonth(month)} {year}";


        // Проверить дату вида <YYYY.MM.DD> на вхождение в диапазон
        public bool DateInRange(string DateFrom = "", string DateTo = "")
        {
            if (String.IsNullOrWhiteSpace(DateFrom) && String.IsNullOrWhiteSpace(DateTo))
                return true;

            DateTime dt = new DateTime(year, month, day);

            if (String.IsNullOrWhiteSpace(DateFrom))
                return dt <= TextToDateTime(DateTo, 0, false);

            if (String.IsNullOrWhiteSpace(DateTo))
                return dt >= TextToDateTime(DateFrom, 0, false);

            return dt >= TextToDateTime(DateFrom, 0, false) && dt <= TextToDateTime(DateTo, 0, false);
        }

    }
}
