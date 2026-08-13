using System;

namespace Top.Logging
{
    public interface ILogWriter
    {
        void Write(LogLevel level, string message, Exception exception);
    }
}
