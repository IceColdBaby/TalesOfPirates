using System;

namespace Top.Logging
{
    public static class Log
    {
        public static ILogWriter Writer { get; set; }

        public static void Info(string message)
        {
            Writer?.Write(message);
        }

        public static void Warning(string message)
        {
            Writer?.WriteWarning(message);
        }

        public static void Error(string message, Exception exception = null)
        {
            Writer?.WriteError(message, exception);
        }

        public static void Debug(string message)
        {
            Writer?.WriteDebug(message);
        }
    }
}
