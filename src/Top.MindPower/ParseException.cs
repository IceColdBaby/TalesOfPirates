using System;

namespace Top.MindPower
{
    /// <summary>
    /// Thrown by the binary readers for expected-bad input.
    /// </summary>
    public sealed class ParseException : Exception
    {
        public string Format { get; }
        public uint Version { get; }
        public long Offset { get; }

        public ParseException(string format, uint version, long offset, string reason)
            : base($"[{format}] v{version} @ {offset}: {reason}")
        {
            Format = format;
            Version = version;
            Offset = offset;
        }
    }
}
