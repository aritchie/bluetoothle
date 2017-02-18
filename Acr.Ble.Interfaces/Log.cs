using System;
using System.Diagnostics;


namespace Acr.Ble
{
    public static class Log
    {
        static Log()
        {
            Out = msg => Debug.WriteLine(msg);
        }


        public static Action<string> Out { get; set; }
        public static void Write(string msg)
        {
            Out?.Invoke(msg);
        }
    }
}
