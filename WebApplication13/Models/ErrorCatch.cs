using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FactPortal.Models
{
    public class ErrorCatch
    {
        public int Result { get; set; }
        public string Message { get; set; }
        public string Info1 { get; set; }
        public string Info2 { get; set; }
        public string Info3 { get; set; }

        public void Set(Exception ex, string title="")
        {
            Result = ex.HResult;
            Message = ex.Message;
            Info1 = ex.StackTrace;
            Info2 = ex.Source;
            Info3 = title;
        }

    }
}
