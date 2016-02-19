using System;

namespace HappyOracle.WellManagedDataAccess.Helpers
{
    public interface ITraceContext
    {
        void Write(string msg);
        //void Warn(string fmt, params object[] args);
    }

    public class ConsoleTraceContext : ITraceContext
    {
        public void Write(string fmt)
        {
            Console.WriteLine($"{DateTime.Now : T} {fmt}");
        }

        //public void Write(string fmt, params object[] args)
        //{
        //    if (string.IsNullOrWhiteSpace(fmt))
        //    {
        //        Console.WriteLine();
        //    }
        //    else {
        //        Console.Write("{0:T} ", DateTime.Now);
        //        Console.WriteLine(fmt, args);
        //    }
        //}
    }
}