using System;

namespace Top.Logging
{
    public static class Log
    {
        public static ILogWriter Writer { get; set; }

        public static void Info(string message)
        {
            Writer?.Write(LogLevel.Info, message, null);
        }

        public static void Warning(string message, Exception exception = null)
        {
            Writer?.Write(LogLevel.Warning, message, exception);
        }

        public static void Error(string message, Exception exception = null)
        {
            Writer?.Write(LogLevel.Error, message, exception);
        }

        public static void Debug(string message)
        {
            Writer?.Write(LogLevel.Debug, message, null);
        }
    }
}
