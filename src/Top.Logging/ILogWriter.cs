using System;

namespace Top.Logging
{
    public interface ILogWriter
    {
        void Write(string message);

        void WriteWarning(string message);

        void WriteError(string message, Exception exception);

        void WriteDebug(string message);
    }
}
