using System;


namespace Plugin.BluetoothLE.Infrastructure
{
    public static class Log
    {
        static Log() => ToDebug();

#if !NETSTANDARD1_0
        public static void ToConsole() => Out = (cat, msg, level) => Console.WriteLine($"[{level}][{cat}] {msg}");
#endif
        public static void ToDebug() => Out = (cat, msg, level) => System.Diagnostics.Debug.WriteLine($"[{level}][{cat}] {msg}");

        public static LogLevel MinLogLevel { get; set; } = LogLevel.Info;
        public static Action<string, string, LogLevel> Out { get; set; }


        public static void Debug(string category, string msg) => Write(category, msg, LogLevel.Debug);
        public static void Info(string category, string msg) => Write(category, msg, LogLevel.Info);
        public static void Warn(string category, string msg) => Write(category, msg, LogLevel.Warn);
        public static void Error(string category, string msg) => Write(category, msg, LogLevel.Error);


        public static void Write(string category, string msg, LogLevel level = LogLevel.Debug)
        {
            if (level >= MinLogLevel)
                Out?.Invoke(category, msg, level);
        }
    }
}
