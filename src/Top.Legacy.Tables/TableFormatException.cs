using System;

namespace Top.Legacy.Tables
{
    /// <summary>
    /// Structural violation of the table format. Thrown in the situations
    /// where the original client aborted the load.
    /// </summary>
    public class TableFormatException : Exception
    {
        public string File { get; }
        public int Line { get; }

        public TableFormatException(string file, int line, string message)
            : base(file == null ? $"line {line}: {message}" : $"{file}:{line}: {message}")
        {
            File = file;
            Line = line;
        }
    }
}
